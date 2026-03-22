import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HistoricalBar } from '../models/historical-bar.model';
import { ChartRange } from '../models/enums/chart-range.enum';
import { ChartTimeframe } from '../models/enums/chart-timeframe.enum';

@Injectable({
  providedIn: 'root',
})
export class StockService {
  private apiUrl = 'https://localhost:7051/api/Stock';

  constructor (private http: HttpClient){}

  // public searchStock(symbol: string):Observable<any>{
  //   const token = localStorage.getItem('jwtToken');
  //   const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

  //   return this.http.get(`${this.apiUrl}/${symbol}`, {headers});
  // }
  public searchStock(symbol: string):Observable<any>{
    const token = localStorage.getItem('jwtToken');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    return this.http.get(`${this.apiUrl}/search/${symbol}`, {headers});
  }

  public getStockHistory(symbol: string, timeframe: ChartTimeframe): Observable<HistoricalBar[]>{
    const token = localStorage.getItem('jwtToken');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    return this.http.get<HistoricalBar[]>
    (`${this.apiUrl}/${symbol}/history?timeframe=${timeframe}`, {headers});
  }
}
