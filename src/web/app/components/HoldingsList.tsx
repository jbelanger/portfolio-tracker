// app/components/HoldingsList.tsx

'use client';

import React, { useState, useEffect } from 'react';
import {
    Box, Typography, FormControlLabel, Checkbox,
} from '@mui/material';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import { Holding } from '../types/types';
import { HoldingAPI } from '@/app/api/HoldingAPI';
import { useSnackbar } from '../context/SnackbarContext';

interface HoldingsListProps {
    portfolioId: number;    
  }

const HoldingsList: React.FC<HoldingsListProps> = ({portfolioId}) => {
    const [holdings, setHoldings] = useState<Holding[]>([]);
    const [displayedHoldings, setDisplayedHoldings] = useState<Holding[]>([]);
    const [hideSmallBalances, setHideSmallBalances] = useState(true);
    const { showSnackbar } = useSnackbar();

    useEffect(() => {
        const fetchHoldings = async () => {
            const result = await HoldingAPI.fetchHoldings(portfolioId);

            if (result.isOk()) {
                const data = result.value;
                setHoldings(data);
                filterHoldings(data, hideSmallBalances);
            } else {
                console.error('Error fetching holdings:', result.error);
                showSnackbar('Failed to load holdings. Please try again later.', 'error');
            }
        };

        fetchHoldings();
    }, [portfolioId, hideSmallBalances]);

    const filterHoldings = (holdings: Holding[], hideSmall: boolean) => {
        const filtered = hideSmall
            ? holdings.filter(holding => holding.balance > 0.01)
            : holdings;

        // Sort by balance, highest to lowest
        const sorted = filtered.sort((a, b) => b.balance - a.balance);
        setDisplayedHoldings(sorted);
    };

    const handleHideSmallBalancesChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setHideSmallBalances(event.target.checked);
        filterHoldings(holdings, event.target.checked);
    };

    const columns: GridColDef[] = [
        { field: 'asset', headerName: 'Asset', flex: 1 },
        { field: 'balance', headerName: 'Balance', flex: 1 },
        { field: 'averageBoughtPrice', headerName: 'Average Bought Price', flex: 1 },
        { field: 'currentPrice', headerName: 'Current Price', flex: 1 },
    ];

    return (
        <Box sx={{ padding: '20px' }}>
            <Typography variant="h4" gutterBottom>
                Holdings Overview
            </Typography>
            <FormControlLabel
                control={
                    <Checkbox
                        checked={hideSmallBalances}
                        onChange={handleHideSmallBalancesChange}
                    />
                }
                label="Hide Small Balances"
            />
            <Box sx={{ height: 400, width: '100%', marginTop: 2 }}>
                <DataGrid
                    rows={displayedHoldings}
                    columns={columns}
                    initialState={{
                        pagination: {
                            paginationModel: {
                                pageSize: 25,
                            },
                        },
                    }}
                    pageSizeOptions={[10, 25, 100]}
                    disableRowSelectionOnClick
                    autoHeight
                    getRowId={(row) => row.id}
                />
            </Box>
        </Box>
    );
};

export default HoldingsList;
