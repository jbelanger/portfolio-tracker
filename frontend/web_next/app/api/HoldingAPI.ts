 'use client';

import apiConfig from './configs/apiConfig';
import { Result, ok, err } from 'neverthrow';
import { getSession } from 'next-auth/react';

export class HoldingAPI {
    static async fetchHoldings(portfolioId: number): Promise<Result<any, string>> {
        const session = await getSession();
        console.log(session);
        const token = session?.apiToken;

        // if (!token) {
        //     return err('Authentication token not found');
        // }

        try {
            const response = await fetch(`${apiConfig.baseURL}/portfolios/${portfolioId}/holdings`, {
                method: 'GET',
                headers: {
                    ...apiConfig.getHeaders(),
                    'Authorization': `Bearer ${token}`,
                },
                credentials: 'include',
                mode: "cors"
            });

            if (!response.ok) {
                return err('Failed to fetch holdings');
            }

            const data = await response.json();
            return ok(data);
        } catch (error) {
            return err('An error occurred while fetching holdings');
        }
    }
}
