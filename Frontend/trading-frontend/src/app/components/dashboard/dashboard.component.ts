import { Component, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StockService } from '../../services/stock.service';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ViewChild, ElementRef } from '@angular/core';
import { createChart, IChartApi, ISeriesApi, CandlestickSeries, LineSeries, AreaSeries, BaselineSeries, CrosshairMode, HistogramSeries } from 'lightweight-charts';
import { ChartRange, getRangePriority } from '../../models/enums/chart-range.enum';
import { ChartTimeframe, getCandlePriority } from '../../models/enums/chart-timeframe.enum';
import { ChartType } from '../../models/enums/chart-type.enum';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {
  public searchQuery: string = '';
  public stockData: any = null;
  public errorMessage: string = '';
  public searchedSymbol: string = '';

  public ChartRangeEnum = ChartRange;
  public ChartTimeframeEnum = ChartTimeframe;

  public ChartTypeEnum = ChartType;
  public selectedChartType: ChartType = ChartType.Candlestick;

  public selectedRange: ChartRange = ChartRange.OneMonth;
  public selectedCandle: ChartTimeframe = ChartTimeframe.OneDay;

  public showSMA: boolean = true;
  private smaSeries: any = null;

  public showEMA: boolean = false;
  private emaSeries: any = null;

  public showBollinger: boolean = false;
  private bbUpperSeries: any = null;
  private bbLowerSeries: any = null;
  private bbBasisSeries: any = null;

  public getRangePrio = getRangePriority;
  public getCandlePrio = getCandlePriority;

  @ViewChild('chartContainer') chartContainer!: ElementRef;
  private chart!: IChartApi;
  // private candlestickSeries!: ISeriesApi<"Candlestick">;

  private mainSeries: any;

  private rawHistoricalData: any[] = [];
  private lastFetchedSymbol: string = '';
  private lastFetchedCandle: ChartTimeframe | null = null;

  constructor(private stockService: StockService, private router: Router, private cdr: ChangeDetectorRef) { }

  public onSearch(): void {
    if (!this.searchQuery) return;

    this.errorMessage = '';
    this.stockData = null;
    this.cdr.detectChanges();

    this.stockService.searchStock(this.searchQuery).subscribe({
      next: (data) => {
        this.stockData = data;

        this.searchedSymbol = this.searchQuery;

        this.loadChartHistory(this.searchedSymbol, this.selectedCandle);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMessage = "Error when searching stock or inexistent stock";
        this.stockData = null;
        this.cdr.detectChanges();
        console.error(err);
      }
    });
  }

  public onChartSettingsChange(): void {
    const rangePriority = getRangePriority(this.selectedRange);
    const candlePriority = getCandlePriority(this.selectedCandle);

    if (candlePriority > rangePriority) {
      if (rangePriority === 1) this.selectedCandle = ChartTimeframe.FifteenMinutes;
      else if (rangePriority === 2) this.selectedCandle = ChartTimeframe.ThirtyMinutes;
      else if (rangePriority === 3) this.selectedCandle = ChartTimeframe.OneHour;
      else if (rangePriority === 4) this.selectedCandle = ChartTimeframe.OneDay;
      else this.selectedCandle = ChartTimeframe.OneMonth;
    }

    if (this.searchedSymbol) {
      // Dacă am schimbat Simbolul sau Lumânarea (Timeframe-ul), facem request la Backend
      if (this.searchedSymbol !== this.lastFetchedSymbol || this.selectedCandle !== this.lastFetchedCandle) {
        this.loadChartHistory(this.searchedSymbol, this.selectedCandle);
      } else {
        // Dacă am schimbat DOAR Range-ul, Tipul Graficului sau Bifa SMA -> Redesenăm instant din memorie!
        this.renderChart(this.rawHistoricalData, this.selectedCandle);
      }
    }
  }

  // private loadChartHistory(symbol: string, range: ChartRange, candle: ChartTimeframe): void {
  //   this.stockService.getStockHistory(symbol, range, candle).subscribe({
  //     next: (historicalData) => {
  //       if (this.chart) {
  //         this.chart.remove();
  //       }

  //       this.chart = createChart(this.chartContainer.nativeElement, {
  //         layout: { background: { color: '#ffffff' }, textColor: '#333' },
  //         grid: { vertLines: { color: '#f0f3fa' }, horzLines: { color: '#f0f3fa' } },
  //         width: this.chartContainer.nativeElement.clientWidth,
  //         height: 400,
  //         crosshair: {
  //           mode: CrosshairMode.Normal,
  //         }
  //       });

  //       this.candlestickSeries = this.chart.addSeries(CandlestickSeries, {
  //         upColor: '#26a69a', downColor: '#ef5350', borderVisible: false,
  //         wickUpColor: '#26a69a', wickDownColor: '#ef5350'
  //       });

  //       // this.chart.priceScale('').applyOptions({
  //       //   scaleMargins: {
  //       //     top: 0.85,
  //       //     bottom: 0
  //       //   }
  //       // });

  //       this.chart.priceScale('right').applyOptions({
  //         scaleMargins: {
  //           top: 0.1,
  //           bottom: 0.25
  //         }
  //       });

  //       const volumeSeries = this.chart.addSeries(HistogramSeries, {
  //         priceFormat: { type: 'volume' },
  //         priceScaleId: '',
  //         lastValueVisible: false,
  //         priceLineVisible: false
  //       });
  //       volumeSeries.priceScale().applyOptions({
  //         scaleMargins: {
  //           top: 0.85,
  //           bottom: 0
  //         }
  //       });
  //       const formattedData = historicalData.map(d => {
  //         const isDailyOrMonthly = candle === ChartTimeframe.OneDay || candle === ChartTimeframe.OneMonth;
  //         const timeValue = isDailyOrMonthly ? d.time.split('T')[0] : new Date(d.time).getTime() / 1000;

  //         return {
  //           time: timeValue as any,
  //           open: d.open,
  //           high: d.high,
  //           low: d.low,
  //           close: d.close,
  //           volume: d.volume
  //         };
  //       });

  //       this.candlestickSeries.setData(formattedData);

  //       const volumeData = formattedData.map(d => ({
  //         time: d.time,
  //         value: d.volume,
  //         color: d.close >= d.open ? 'rgba(38, 166, 154, 0.5)' : 'rgba(239, 83, 80, 0.5)'
  //       }));

  //       volumeSeries.setData(volumeData);
  //       this.chart.timeScale().fitContent();
  //     },
  //     error: (err) => console.error(err)
  //   });
  // }

  private loadChartHistory(symbol: string, candle: ChartTimeframe): void {
    this.stockService.getStockHistory(symbol, candle).subscribe({
      next: (historicalData) => {
        // Salvăm datele descărcate și setările curente
        this.rawHistoricalData = historicalData;
        this.lastFetchedSymbol = symbol;
        this.lastFetchedCandle = candle;

        this.updateHeaderStats(historicalData);
        this.renderChart(historicalData, candle);
        this.cdr.detectChanges();
      },
      error: (err) => console.error(err)
    });
  }

  private applyZoom(): void {
    if (!this.rawHistoricalData || this.rawHistoricalData.length === 0) return;

    // Luăm data ultimei lumânări
    const toDate = new Date(this.rawHistoricalData[this.rawHistoricalData.length - 1].time);
    const fromDate = new Date(toDate.getTime());

    // Calculăm matematic perioada de început pentru Zoom
    switch (this.selectedRange) {
      case ChartRange.OneYear: fromDate.setFullYear(fromDate.getFullYear() - 1); break;
      case ChartRange.OneMonth: fromDate.setMonth(fromDate.getMonth() - 1); break;
      case ChartRange.OneDay: fromDate.setDate(fromDate.getDate() - 1); break;
      case ChartRange.OneHour: fromDate.setHours(fromDate.getHours() - 1); break;
      case ChartRange.ThirtyMinutes: fromDate.setMinutes(fromDate.getMinutes() - 30); break;
      case ChartRange.FifteenMinutes: fromDate.setMinutes(fromDate.getMinutes() - 15); break;
    }

    const isDailyOrMonthly = this.selectedCandle === ChartTimeframe.OneDay || this.selectedCandle === ChartTimeframe.OneMonth;

    // Formatăm timestamp-urile exact cum le cere Lightweight Charts
    const fromValue = isDailyOrMonthly ? fromDate.toISOString().split('T')[0] : fromDate.getTime() / 1000;
    const toValue = isDailyOrMonthly ? toDate.toISOString().split('T')[0] : toDate.getTime() / 1000;

    // setTimeout lasă graficul să se randeze înainte de a executa comanda de Zoom
    if (this.chart) {
      this.chart.timeScale().setVisibleRange({
        from: fromValue as any,
        to: toValue as any
      });
    }
  }

  private calculateSMA(historicalData: any[], period: number, candle: ChartTimeframe): any[] {
    const smaData = [];
    const isDailyOrMonthly = candle === ChartTimeframe.OneDay || candle === ChartTimeframe.OneMonth;

    // Începem de la indexul 'period - 1' pentru că nu putem face o medie de 20 de zile pe primele 19 zile
    for (let i = period - 1; i < historicalData.length; i++) {
      let sum = 0;
      for (let j = 0; j < period; j++) {
        sum += historicalData[i - j].close;
      }

      const timeValue = isDailyOrMonthly
        ? historicalData[i].time.split('T')[0]
        : new Date(historicalData[i].time).getTime() / 1000;

      smaData.push({
        time: timeValue as any,
        value: sum / period
      });
    }

    return smaData;
  }
  private calculateEMA(historicalData: any[], period: number, candle: ChartTimeframe): any[] {
    if (historicalData.length < period) return [];
    const emaData = [];
    const k = 2 / (period + 1);
    const isDailyOrMonthly = candle === ChartTimeframe.OneDay || candle === ChartTimeframe.OneMonth;

    // Pasul 1: Punctul de start (Seed) este SMA-ul primelor 'period' zile
    let sum = 0;
    for (let i = 0; i < period; i++) {
      sum += historicalData[i].close;
    }
    let ema = sum / period;

    // Pasul 2: Calculăm exponențial restul datelor
    for (let i = period - 1; i < historicalData.length; i++) {
      if (i > period - 1) {
        ema = (historicalData[i].close - ema) * k + ema;
      }
      const timeValue = isDailyOrMonthly ? historicalData[i].time.split('T')[0] : new Date(historicalData[i].time).getTime() / 1000;
      emaData.push({ time: timeValue as any, value: ema });
    }
    return emaData;
  }

  private calculateBollingerBands(historicalData: any[], period: number, stdDevMultiplier: number, candle: ChartTimeframe): { upper: any[], lower: any[], basis: any[] } {
    const upperData = [];
    const lowerData = [];
    const basisData = [];
    const isDailyOrMonthly = candle === ChartTimeframe.OneDay || candle === ChartTimeframe.OneMonth;

    for (let i = period - 1; i < historicalData.length; i++) {
      let sum = 0;
      for (let j = 0; j < period; j++) sum += historicalData[i - j].close;
      const sma = sum / period; // Media pe mijloc

      // Calculăm deviația standard (volatilitatea)
      let varianceSum = 0;
      for (let j = 0; j < period; j++) {
        varianceSum += Math.pow(historicalData[i - j].close - sma, 2);
      }
      const variance = varianceSum / period;
      const stdDev = Math.sqrt(variance);

      const timeValue = isDailyOrMonthly ? historicalData[i].time.split('T')[0] : new Date(historicalData[i].time).getTime() / 1000;

      basisData.push({ time: timeValue as any, value: sma });
      upperData.push({ time: timeValue as any, value: sma + stdDevMultiplier * stdDev });
      lowerData.push({ time: timeValue as any, value: sma - stdDevMultiplier * stdDev });
    }

    return { upper: upperData, lower: lowerData, basis: basisData };
  }
  private updateHeaderStats(historicalData: any[]): void {
    if (!historicalData || historicalData.length === 0 || !this.stockData) return;

    const lastBar = historicalData[historicalData.length - 1];

    // Totul cu litere mici!
    this.stockData.open = lastBar.open;
    this.stockData.high = lastBar.high;
    this.stockData.low = lastBar.low;
    this.stockData.volume = lastBar.volume;

    if (historicalData.length > 1) {
      this.stockData.previousClose = historicalData[historicalData.length - 2].close;
    }
  }

  // private renderChart(historicalData: any[], candle: ChartTimeframe): void {
  //   // Curățăm graficul vechi
  //   if (this.chart) {
  //     this.chart.remove();
  //   }

  //   this.chart = createChart(this.chartContainer.nativeElement, {
  //     layout: { background: { color: '#ffffff' }, textColor: '#333' },
  //     grid: { vertLines: { color: '#f0f3fa' }, horzLines: { color: '#f0f3fa' } },
  //     width: this.chartContainer.nativeElement.clientWidth,
  //     height: 400,
  //     crosshair: {
  //       mode: CrosshairMode.Normal,
  //     }
  //   });

  //   this.candlestickSeries = this.chart.addSeries(CandlestickSeries, {
  //     upColor: '#26a69a', downColor: '#ef5350', borderVisible: false,
  //     wickUpColor: '#26a69a', wickDownColor: '#ef5350'
  //   });

  //   // this.chart.priceScale('').applyOptions({
  //   //   scaleMargins: {
  //   //     top: 0.85,
  //   //     bottom: 0
  //   //   }
  //   // });

  //   this.chart.priceScale('right').applyOptions({
  //     scaleMargins: {
  //       top: 0.1,
  //       bottom: 0.25
  //     }
  //   });

  //   const volumeSeries = this.chart.addSeries(HistogramSeries, {
  //     priceFormat: { type: 'volume' },
  //     priceScaleId: '',
  //     lastValueVisible: false,
  //     priceLineVisible: false
  //   });
  //   volumeSeries.priceScale().applyOptions({
  //     scaleMargins: {
  //       top: 0.85,
  //       bottom: 0
  //     }
  //   });
  //   const formattedData = historicalData.map(d => {
  //     const isDailyOrMonthly = candle === ChartTimeframe.OneDay || candle === ChartTimeframe.OneMonth;
  //     const timeValue = isDailyOrMonthly ? d.time.split('T')[0] : new Date(d.time).getTime() / 1000;

  //     return {
  //       time: timeValue as any,
  //       open: d.open,
  //       high: d.high,
  //       low: d.low,
  //       close: d.close,
  //       volume: d.volume
  //     };
  //   });

  //   this.candlestickSeries.setData(formattedData);

  //   const volumeData = formattedData.map(d => ({
  //     time: d.time,
  //     value: d.volume,
  //     color: d.close >= d.open ? 'rgba(38, 166, 154, 0.5)' : 'rgba(239, 83, 80, 0.5)'
  //   }));

  //   volumeSeries.setData(volumeData);
  //   this.chart.timeScale().fitContent();
  // }

  private renderChart(historicalData: any[], candle: ChartTimeframe): void {
    if (this.chart) {
      this.chart.remove();
    }

    this.chart = createChart(this.chartContainer.nativeElement, {
      layout: { background: { color: '#ffffff' }, textColor: '#333' },
      grid: { vertLines: { color: '#f0f3fa' }, horzLines: { color: '#f0f3fa' } },
      width: this.chartContainer.nativeElement.clientWidth,
      height: 400,
      crosshair: { mode: CrosshairMode.Normal }
    });

    // 1. ALEGEM TIPUL DE GRAFIC
    switch (this.selectedChartType) {
      case ChartType.Line:
        this.mainSeries = this.chart.addSeries(LineSeries, { color: '#2962FF', lineWidth: 2 });
        break;
      case ChartType.Area:
        this.mainSeries = this.chart.addSeries(AreaSeries, {
          lineColor: '#2962FF', topColor: 'rgba(41, 98, 255, 0.3)', bottomColor: 'rgba(41, 98, 255, 0)', lineWidth: 2
        });
        break;
      case ChartType.Baseline:
        // Folosim închiderea de ieri ca linie de bază. Dacă nu există, folosim prima lumânare.
        const basePrice = this.stockData?.previousClose || historicalData[0].close;
        this.mainSeries = this.chart.addSeries(BaselineSeries, {
          baseValue: { type: 'price', price: basePrice },
          topLineColor: '#26a69a', topFillColor1: 'rgba(38, 166, 154, 0.3)', topFillColor2: 'rgba(38, 166, 154, 0)',
          bottomLineColor: '#ef5350', bottomFillColor1: 'rgba(239, 83, 80, 0)', bottomFillColor2: 'rgba(239, 83, 80, 0.3)'
        });
        break;
      case ChartType.Candlestick:
      default:
        this.mainSeries = this.chart.addSeries(CandlestickSeries, {
          upColor: '#26a69a', downColor: '#ef5350', borderVisible: false,
          wickUpColor: '#26a69a', wickDownColor: '#ef5350'
        });
        break;
    }

    this.chart.priceScale('right').applyOptions({ scaleMargins: { top: 0.1, bottom: 0.25 } });

    // Volumul rămâne neschimbat
    const volumeSeries = this.chart.addSeries(HistogramSeries, {
      priceFormat: { type: 'volume' }, priceScaleId: '', lastValueVisible: false, priceLineVisible: false
    });
    volumeSeries.priceScale().applyOptions({ scaleMargins: { top: 0.85, bottom: 0 } });

    // 2. FORMATĂM DATELE ÎN FUNCȚIE DE GRAFIC (Lumânările vor OHLC, restul vor doar Close)
    const formattedData = historicalData.map(d => {
      const isDailyOrMonthly = candle === ChartTimeframe.OneDay || candle === ChartTimeframe.OneMonth;
      const timeValue = isDailyOrMonthly ? d.time.split('T')[0] : new Date(d.time).getTime() / 1000;

      if (this.selectedChartType === ChartType.Candlestick) {
        return { time: timeValue as any, open: d.open, high: d.high, low: d.low, close: d.close };
      } else {
        // Graficele Line, Area și Baseline au nevoie doar de o singură "valoare" (închiderea)
        return { time: timeValue as any, value: d.close };
      }
    });

    this.mainSeries.setData(formattedData);

    const volumeData = historicalData.map(d => {
      const isDailyOrMonthly = candle === ChartTimeframe.OneDay || candle === ChartTimeframe.OneMonth;
      return {
        time: (isDailyOrMonthly ? d.time.split('T')[0] : new Date(d.time).getTime() / 1000) as any,
        value: d.volume,
        color: d.close >= d.open ? 'rgba(38, 166, 154, 0.5)' : 'rgba(239, 83, 80, 0.5)'
      };
    });

    volumeSeries.setData(volumeData);

    // --- NOU: LOGICA PENTRU INDICATORUL SMA ---
    if (this.showSMA) {
      // 1. Creăm linia portocalie (Overlay pe graficul principal)
      this.smaSeries = this.chart.addSeries(LineSeries, {
        color: '#ff9800', // Portocaliu
        lineWidth: 2,
        title: 'SMA 20', // Titlul care va apărea ca legendă
        crosshairMarkerVisible: false // Opțional: ascunde bulina de pe linie
      });

      // 2. Calculăm matematica pe 20 de lumânări
      const smaFormattedData = this.calculateSMA(historicalData, 20, candle);

      // 3. Desenăm datele
      this.smaSeries.setData(smaFormattedData);
    }

    if (this.showEMA) {
      this.emaSeries = this.chart.addSeries(LineSeries, {
        color: '#2196F3', // Albastru
        lineWidth: 2,
        title: 'EMA 20',
        crosshairMarkerVisible: false
      });
      const emaFormattedData = this.calculateEMA(historicalData, 20, candle);
      this.emaSeries.setData(emaFormattedData);
    }

    // --- INDICATORUL BOLLINGER BANDS ---
    if (this.showBollinger) {
      const bbColor = 'rgba(156, 39, 176, 0.6)'; // Mov
      
      this.bbUpperSeries = this.chart.addSeries(LineSeries, { color: bbColor, lineWidth: 1, title: 'Upper' });
      this.bbBasisSeries = this.chart.addSeries(LineSeries, { color: bbColor, lineWidth: 1, title: 'BB Basis', lineStyle: 2 }); // Linie punctată
      this.bbLowerSeries = this.chart.addSeries(LineSeries, { color: bbColor, lineWidth: 1, title: 'Lower' });

      const bbData = this.calculateBollingerBands(historicalData, 20, 2, candle);
      
      this.bbUpperSeries.setData(bbData.upper);
      this.bbBasisSeries.setData(bbData.basis);
      this.bbLowerSeries.setData(bbData.lower);
    }

    // this.chart.timeScale().fitContent();
    this.applyZoom();
  }

  public getPriceDiff(): number {
    if (!this.stockData || !this.stockData.previousClose) return 0;
    // Modificat din askPrice în price!
    return this.stockData.price - this.stockData.previousClose;
  }

  public getDailyChangePercent(): number {
    if (!this.stockData || !this.stockData.previousClose) return 0;
    return (this.getPriceDiff() / this.stockData.previousClose) * 100;
  }

  public getChangeClass(): string {
    const diff = this.getPriceDiff();
    return diff >= 0 ? 'price-up' : 'price-down';
  }

  public recenterChart(): void {
    this.applyZoom();
  }

  public onLogout(): void {
    localStorage.removeItem('jwtToken');
    this.router.navigate(['/login']);
  }
}
