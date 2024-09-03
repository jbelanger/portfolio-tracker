// api/WalletAPI.ts

import { Result, ok, err } from 'neverthrow';
import apiConfig from './configs/apiConfig';

export class WalletAPI {

  static async fetchWallets(portfolioId: number): Promise<Result<any, string>> {
    try {
      const response = await fetch(`${apiConfig.baseURL}/portfolios/${portfolioId}/wallets`, {
        method: 'GET',
        headers: apiConfig.getHeaders(),
      });

      if (!response.ok) {
        return err('Failed to fetch wallets');
      }

      const data = await response.json();
      return ok(data);
    } catch (error) {
      return err('An error occurred while fetching wallets');
    }
  }

  static async createWallet(portfolioId: number, walletName: string): Promise<Result<any, string>> {
    try {
      const response = await fetch(`${apiConfig.baseURL}/portfolios/${portfolioId}/wallets`, {
        method: 'POST',
        headers: apiConfig.getHeaders(),
        body: JSON.stringify({ name: walletName }),
      });

      if (!response.ok) {
        return err('Failed to create wallet');
      }

      const data = await response.json();
      return ok(data);
    } catch (error) {
      return err('An error occurred while creating the wallet');
    }
  }

  static async deleteWallet(portfolioId: number, walletId: number): Promise<Result<any, string>> {
    try {
      const response = await fetch(`${apiConfig.baseURL}/portfolios/${portfolioId}/wallets/${walletId}`, {
        method: 'DELETE',
        headers: apiConfig.getHeaders(),
      });

      if (!response.ok) {
        return err('Failed to delete wallet');
      }

      return ok(response);
    } catch (error) {
      return err('An error occurred while deleting the wallet');
    }
  }
}
