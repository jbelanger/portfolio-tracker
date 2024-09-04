// api/TransactionAPI.ts

import apiConfig from './configs/apiConfig';
import { CryptoCurrencyTransactionDto } from '../types/types';
import { Result, ok, err } from 'neverthrow';

export class TransactionAPI {

    static async fetchTransactions(portfolioId: number, walletId: number | null): Promise<Result<CryptoCurrencyTransactionDto[], string>> {
        const url = walletId === null
            ? `${apiConfig.baseURL}/portfolios/${portfolioId}/transactions`
            : `${apiConfig.baseURL}/portfolios/${portfolioId}/wallets/${walletId}/transactions`;

        try {
            const response = await fetch(url, {
                method: 'GET',
                headers: apiConfig.getHeaders(),
            });

            if (!response.ok) {
                return err('Failed to fetch transactions');
            }

            const data = await response.json();
            return ok(data);
        } catch (error) {
            return err('An error occurred while fetching transactions');
        }
    };

    static async createTransaction(portfolioId: number, walletId: number, transaction: CryptoCurrencyTransactionDto): Promise<Result<CryptoCurrencyTransactionDto, string>> {
        try {
            const response = await fetch(`${apiConfig.baseURL}/portfolios/${portfolioId}/wallets/${walletId}/transactions`, {
                method: 'POST',
                headers: apiConfig.getHeaders(),
                body: JSON.stringify(transaction),
            });

            if (!response.ok) {
                return err('Failed to create transaction');
            }

            const data = await response.json();
            return ok(data);
        } catch (error) {
            return err('An error occurred while creating the transaction');
        }
    };

    static async deleteTransaction(portfolioId: number, walletId: number, transactionId: number): Promise<Result<void, string>> {
        try {
            const response = await fetch(`${apiConfig.baseURL}/portfolios/${portfolioId}/wallets/${walletId}/transactions/${transactionId}`, {
                method: 'DELETE',
                headers: apiConfig.getHeaders(),
            });

            if (!response.ok) {
                return err('Failed to delete transaction');
            }

            return ok(undefined);
        } catch (error) {
            return err('An error occurred while deleting the transaction');
        }
    };

    static async bulkDeleteTransactions(portfolioId: number, walletId: number, transactionIds: number[]): Promise<Result<void, string>> {
        try {
            const response = await fetch(`${apiConfig.baseURL}/portfolios/${portfolioId}/wallets/${walletId}/transactions/bulk-delete`, {
                method: 'DELETE',
                headers: apiConfig.getHeaders(),
                body: JSON.stringify(transactionIds),
            });

            if (!response.ok) {
                return err('Failed to delete transactions');
            }

            return ok(undefined);
        } catch (error) {
            return err('An error occurred while deleting the transactions');
        }
    };

    static async bulkEditTransactions(portfolioId: number, walletId: number, transactions: Partial<CryptoCurrencyTransactionDto>[]): Promise<Result<void, string>> {
        try {
            const response = await fetch(`${apiConfig.baseURL}/portfolios/${portfolioId}/wallets/${walletId}/transactions/bulk-edit`, {
                method: 'PUT',
                headers: apiConfig.getHeaders(),
                body: JSON.stringify(transactions),
            });

            if (!response.ok) {
                return err('Failed to edit transactions');
            }

            return ok(undefined);
        } catch (error) {
            return err('An error occurred while editing the transactions');
        }
    };

    static async uploadCsvTransactions(portfolioId: number, walletId: number, formData: FormData, importType: string): Promise<Result<void, string>> {
        try {
            const response = await fetch(`${apiConfig.baseURL}/portfolios/${portfolioId}/wallets/${walletId}/transactions/upload-csv?csvImportType=${importType}`, {
                method: 'POST',
                body: formData,
            });

            if (!response.ok) {
                return err('Failed to upload CSV transactions');
            }

            return ok(undefined);
        } catch (error) {
            return err('An error occurred while uploading CSV transactions');
        }
    };

    static async exportTransactions(portfolioId: number, walletId: number, format: string): Promise<Result<Blob, string>> {
        try {
          const response = await fetch(`${apiConfig.baseURL}/portfolios/${portfolioId}/wallets/${walletId}/export?format=${format}`, {
            method: 'GET',
            headers: {
              ...apiConfig.getHeaders(),
              'Accept': 'application/octet-stream',
            },
          });
    
          if (response.ok) {
            const blob = await response.blob();
            return ok(blob);
          } else {
            return err(`Failed to export transactions: ${response.statusText}`);
          }
        } catch (error) {
          console.error('Error exporting transactions:', error);
          return err('An unexpected error occurred while exporting transactions.');
        }
      }
    
      static async editTransaction(portfolioId: number, walletId: number, transactionId: number, transaction: Partial<CryptoCurrencyTransactionDto>): Promise<Result<CryptoCurrencyTransactionDto, string>> {
        try {
          const response = await fetch(`${apiConfig.baseURL}/portfolios/${portfolioId}/wallets/${walletId}/transactions/${transactionId}`, {
            method: 'PUT',
            headers: apiConfig.getHeaders(),
            body: JSON.stringify(transaction),
          });
    
          if (response.ok) {
            const data = await response.json();
            return ok(data);
          } else {
            return err(`Failed to edit transaction: ${response.statusText}`);
          }
        } catch (error) {
          console.error('Error editing transaction:', error);
          return err('An unexpected error occurred while editing the transaction.');
        }
      }   
}
