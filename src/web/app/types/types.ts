export interface CryptoCurrencyTransactionDto {
  id: number;
  dateTime: Date;
  type: string;
  receivedAmount?: number;
  receivedCurrency: string;
  sentAmount?: number;
  sentCurrency: string;
  feeAmount?: number;
  feeCurrency: string;
  account: string;
  note: string;
}

export interface Wallet {
  id: number;
  name: string;
  transactions: CryptoCurrencyTransactionDto[];
  balance: number;
  transactionCount: number;
}

export enum CsvFileImportType {
  Kraken = 'Kraken',
  Coinbase = 'Coinbase',
  Binance = 'Binance',
}

export interface Holding {
  id: number;
  asset: string;
  balance: number;
  averageBoughtPrice: number;
  currentPrice: number;
  errorMessage?: string;
}
