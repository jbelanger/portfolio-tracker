
export interface Holding {
  id: number;
  asset: string;
  balance: number;
  averageBoughtPrice: number;
  currentPrice: number;
  errorMessage?: string;
}
