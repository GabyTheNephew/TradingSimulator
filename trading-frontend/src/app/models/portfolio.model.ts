export interface PortfolioItem {
  symbol: string;
  quantity: number;
  averagePrice: number;
  currentPrice: number;
}

export interface PortfolioResponse {
  balance: number;
  items: PortfolioItem[];
}