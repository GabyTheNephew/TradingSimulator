import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TradingService } from '../../services/trading.service';
import { PortfolioItem, PortfolioResponse } from '../../models/portfolio.model';
import { Subscription, interval } from 'rxjs';
@Component({
  selector: 'app-portfolio',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './portfolio.component.html',
  styleUrl: './portfolio.component.scss',
})
export class PortfolioComponent implements OnInit, OnDestroy {
  public cashBalance: number = 0;
  public portfolioItems: PortfolioItem[] = [];
  private pollingSubscription?: Subscription;

  constructor(private tradingService: TradingService, private cdr: ChangeDetectorRef) { }

  ngOnDestroy(): void {
    if (this.pollingSubscription) {
      this.pollingSubscription.unsubscribe();
    }
  }
  ngOnInit(): void {
    this.loadPortfolio();
    this.startPolling();
  }
  private loadPortfolio(): void {
    this.tradingService.getPortfolio().subscribe({
      next: (res: PortfolioResponse) => {
        this.cashBalance = res.balance;
        this.portfolioItems = res.items;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error fetching portfolio', err)
    });
  }
  private startPolling(): void {
    // Reîncărcăm portofoliul la fiecare 5 secunde pentru a avea prețurile live
    this.pollingSubscription = interval(5000).subscribe(() => {
      this.loadPortfolio();
    });
  }
  public get totalInvestedValue(): number {
    return this.portfolioItems.reduce((total, item) => total + (item.quantity * item.averagePrice), 0);
  }

  public get totalCurrentValue(): number {
    return this.portfolioItems.reduce((total, item) => total + (item.quantity * item.currentPrice), 0);
  }

  public get accountTotalValue(): number {
    return this.cashBalance + this.totalCurrentValue;
  }

  public get totalPnL(): number {
    return this.totalCurrentValue - this.totalInvestedValue;
  }

  public get totalPnLPercent(): number {
    if (this.totalInvestedValue === 0) return 0;
    return (this.totalPnL / this.totalInvestedValue) * 100;
  }
}
