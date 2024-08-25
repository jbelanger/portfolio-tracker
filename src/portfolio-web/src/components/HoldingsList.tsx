import React, { useState, useEffect } from 'react';
import {
    Box, Typography, Snackbar, Alert, FormControlLabel, Checkbox,
} from '@mui/material';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import apiClient from '../api/axios';
import { Holding } from '../types/Wallet';

const HoldingsList: React.FC = () => {
    const [holdings, setHoldings] = useState<Holding[]>([]);
    const [displayedHoldings, setDisplayedHoldings] = useState<Holding[]>([]);
    const [hideSmallBalances, setHideSmallBalances] = useState(true);
    const [snackbarOpen, setSnackbarOpen] = useState(false);
    const [snackbarMessage, setSnackbarMessage] = useState('');
    const [snackbarSeverity, setSnackbarSeverity] = useState<'success' | 'error'>('success');

    useEffect(() => {
        const fetchHoldings = async () => {
            try {
                const response = await apiClient.get<Holding[]>('/portfolios/1/holdings');
                setHoldings(response.data);
                filterHoldings(response.data, hideSmallBalances);
            } catch (err) {
                console.error('Error fetching holdings:', err);
                showSnackbar('Failed to load holdings. Please try again later.', 'error');
            }
        };

        fetchHoldings();
    }, []);

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

    const showSnackbar = (message: string, severity: 'success' | 'error' = 'success') => {
        setSnackbarMessage(message);
        setSnackbarSeverity(severity);
        setSnackbarOpen(true);
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

            <Snackbar
                open={snackbarOpen}
                autoHideDuration={6000}
                onClose={() => setSnackbarOpen(false)}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
            >
                <Alert onClose={() => setSnackbarOpen(false)} severity={snackbarSeverity} sx={{ width: '100%' }}>
                    {snackbarMessage}
                </Alert>
            </Snackbar>
        </Box>
    );
};

export default HoldingsList;
