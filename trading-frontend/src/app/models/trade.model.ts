export interface TradeRequest {
  symbol: string;
  quantity: number;
}

export interface TradeResponse {
  success: boolean;
  message: string;
  newBalance?: number;
  remainingQuantity?: number;
  portfolioItem?: any;
}