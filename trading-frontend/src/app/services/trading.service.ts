import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { OrderResponse, TradeRequest, TradeResponse } from '../models/trade.model';
import { PortfolioResponse } from '../models/portfolio.model';

@Injectable({
  providedIn: 'root'
})
export class TradingService {
  private apiUrl = 'https://localhost:7051/api/trading';

  constructor(private http: HttpClient) { }

  buyStock(request: TradeRequest): Observable<TradeResponse> {
    return this.http.post<TradeResponse>(`${this.apiUrl}/buy`, request);
  }

  sellStock(request: TradeRequest): Observable<TradeResponse> {
    return this.http.post<TradeResponse>(`${this.apiUrl}/sell`, request);
  }
  
  getPortfolio(): Observable<PortfolioResponse> {
    return this.http.get<PortfolioResponse>(`${this.apiUrl}/portfolio`);
  }
  
  getOrdersBySymbol(symbol: string): Observable<OrderResponse[]> {
    return this.http.get<OrderResponse[]>(`${this.apiUrl}/orders?symbol=${symbol}`);
  }

  cancelOrder(orderId: number): Observable<TradeResponse> {
    return this.http.post<TradeResponse>(`${this.apiUrl}/cancel/${orderId}`, {});
  }
}