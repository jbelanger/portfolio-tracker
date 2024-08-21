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
  }
  

  export enum CsvFileImportType {
    Kraken = 'Kraken',
    Coinbase = 'Coinbase',
    Binance = 'Binance',
    // Add more import types as needed
}