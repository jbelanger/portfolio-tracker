import { CryptoCurrencyTransactionDto } from "./CryptoCurrencyTransactionDto";


export interface Wallet {
  id: number;
  name: string;
  transactions: CryptoCurrencyTransactionDto[];
}
