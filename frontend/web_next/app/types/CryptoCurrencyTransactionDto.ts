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
