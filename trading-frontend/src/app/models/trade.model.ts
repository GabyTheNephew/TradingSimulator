import { PortfolioItem } from "./portfolio.model";


export interface TradeRequest {
  symbol: string;
  quantity: number;
}

export interface TradeResponse {
  success: boolean;
  message: string;
  newBalance?: number;
  remainingQuantity?: number;
  portfolioItem?: PortfolioItem;
}


export interface OrderResponse {
  id: number;
  symbol: string;
  side: string;
  quantity: number;
  filledQuantity: number;
  averageFillPrice: number;
  status: string;
  createdAt: string; 
}

export interface StockQuote {
  symbol: string;
  price: number;
  bid: number;
  ask: number;
  changePercent: number;
  open: number;
  high: number;
  low: number;
  previousClose: number;
  volume: number;
}