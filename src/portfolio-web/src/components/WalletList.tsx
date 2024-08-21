import React, { useState, useEffect } from 'react';
import {
    List, ListItem, ListItemText, ListItemSecondaryAction, IconButton, Typography, Paper, Grid, Snackbar, Alert,
    Dialog, DialogTitle, DialogContent, DialogActions, Button, TextField, Box, MenuItem, Select, InputLabel, FormControl
} from '@mui/material';
import { Delete, Edit, Add, Download } from '@mui/icons-material';
import { DataGrid, GridColDef, GridRenderCellParams } from '@mui/x-data-grid';
import apiClient from '../api/axios';
import { Wallet, CryptoCurrencyTransactionDto } from '../types/Wallet';
import dayjs from 'dayjs';
import TransactionForm from './TransactionForm'
import AddTransactionDialog from './AddTransactionDialog'

const WalletList: React.FC = () => {
    const [wallets, setWallets] = useState<Wallet[]>([]);
    const [selectedWallet, setSelectedWallet] = useState<Wallet | null>(null);
    const [transactions, setTransactions] = useState<CryptoCurrencyTransactionDto[]>([]);
    const [isEditing, setIsEditing] = useState(false);
    const [currentTransaction, setCurrentTransaction] = useState<CryptoCurrencyTransactionDto | null>(null);
    const [dialogOpen, setDialogOpen] = useState(false);
    const [exportFormat, setExportFormat] = useState('xlsx');
    const [errorMessage, setErrorMessage] = useState<string | null>(null);
    const [transactionDialogOpen, setTransactionDialogOpen] = useState(false);
    const [snackbarOpen, setSnackbarOpen] = useState(false);
    const [snackbarMessage, setSnackbarMessage] = useState('');
    const [snackbarSeverity, setSnackbarSeverity] = useState<'success' | 'error'>('success');
    const [deleting, setDeleting] = useState<number | null>(null); // Track the ID of the deleting transaction


    useEffect(() => {
        fetchWallets();
    }, []);

    const fetchWallets = async () => {
        try {
            const response = await apiClient.get<Wallet[]>('/portfolios/1/wallets'); // Replace 1 with your portfolio ID
            setWallets(response.data);
        } catch (error) {
            console.error('Error fetching wallets:', error);
        }
    };

    const fetchTransactions = async (walletId: number) => {
        try {
            const response = await apiClient.get<CryptoCurrencyTransactionDto[]>(`/portfolios/1/wallets/${walletId}/transactions`);
            setTransactions(response.data);
        } catch (error) {
            console.error('Error fetching transactions:', error);
        }
    };

    const handleWalletClick = async (wallet: Wallet) => {
        setSelectedWallet(wallet);
        fetchTransactions(wallet.id);
    };

    const handleDeleteWallet = async (walletId: number) => {
        try {
            await apiClient.delete(`/portfolios/1/wallets/${walletId}`); // Replace 1 with your portfolio ID
            setWallets(wallets.filter(wallet => wallet.id !== walletId));
            setSelectedWallet(null);
            setTransactions([]);
        } catch (error) {
            console.error('Error deleting wallet:', error);
        }
    };
    const handleDeleteTransaction = async (transactionId: number) => {

        try {
            // Optimistically update the state
            const updatedTransactions = transactions.filter(transaction => transaction.id !== transactionId);
            setTransactions(updatedTransactions);
            setDeleting(transactionId);  // Set the deleting state to the ID of the transaction being deleted

            if (selectedWallet) {
                await apiClient.delete(`/portfolios/1/wallets/${selectedWallet.id}/transactions/${transactionId}`);
                showSnackbar('Transaction deleted successfully.', 'success');
            }
        } catch (error) {
            // Rollback the state update if the API call fails
            setTransactions(transactions);
            console.error('Error deleting transaction:', error);
            showSnackbar('Error deleting transaction. Please try again.', 'error');
        } finally {
            // Temporarily disable the button to prevent double click
            setDeleting(null);  // Clear the deleting state once deletion is complete
        }
    };

    const handleEditTransaction = (transaction: CryptoCurrencyTransactionDto) => {
        setCurrentTransaction(transaction);
        setIsEditing(true);
        setDialogOpen(true);
    };

    const handleDialogClose = () => {
        setDialogOpen(false);
        setCurrentTransaction(null);
    };

    const handleSaveTransaction = async () => {
        if (!currentTransaction || !selectedWallet) return;
        try {
            setErrorMessage(null); // Clear previous error message

            if (isEditing) {
                // If editing, use PUT to update the transaction
                await apiClient.put(`/portfolios/1/wallets/${selectedWallet.id}/transactions/${currentTransaction.id}`, currentTransaction);
            } else {
                // If not editing, use POST to create a new transaction
                await apiClient.post(`/portfolios/1/wallets/${selectedWallet.id}/transactions`, currentTransaction);
            }

            // Refresh the transactions from the server only after a successful save
            await fetchTransactions(selectedWallet.id);
            handleDialogClose();
        } catch (error: any) {
            console.error('Error saving transaction:', error);
            if (error.response && error.response.data && typeof error.response.data === 'string') {
                setErrorMessage(error.response.data); // Display server error message
            } else {
                setErrorMessage('An unexpected error occurred. Please try again.'); // Generic error message
            }
        }
    };

    const handleAddTransaction = () => {
        setTransactionDialogOpen(true);
    };

    const handleTransactionDialogClose = () => {
        setTransactionDialogOpen(false);
        fetchTransactions(selectedWallet?.id || 0); // Refresh transactions after dialog closes
    };


    const handleExport = async () => {
        if (!selectedWallet) return;
        try {
            const response = await apiClient.get(`/portfolios/1/wallets/${selectedWallet.id}/export?format=${exportFormat}`, {
                responseType: 'blob', // To handle file downloads
            });
            const url = window.URL.createObjectURL(new Blob([response.data]));
            const link = document.createElement('a');
            link.href = url;
            link.setAttribute('download', `transactions.${exportFormat}`);
            document.body.appendChild(link);
            link.click();
        } catch (error) {
            console.error('Error exporting transactions:', error);
        }
    };

    const showSnackbar = (message: string, severity: 'success' | 'error' = 'success') => {
        setSnackbarMessage(message);
        setSnackbarSeverity(severity);
        setSnackbarOpen(true);
    };

    const columns: GridColDef[] = [
        {
            field: 'dateTime',
            headerName: 'Date',
            flex: 2,
            valueGetter: (params: any, tx: CryptoCurrencyTransactionDto) => {
                return dayjs(tx.dateTime).format('YYYY-MM-DD HH:mm:ss UTC');
            },
        },
        { field: 'type', headerName: 'Type', flex: 1 },
        {
            field: 'receivedAmount',
            headerName: 'Received Amount',
            flex: 1,
            valueGetter: (params: any, tx: CryptoCurrencyTransactionDto) =>
                `${tx.receivedAmount ?? ''} ${tx.receivedCurrency ?? ''}`,
        },
        {
            field: 'sentAmount',
            headerName: 'Sent Amount',
            flex: 1,
            valueGetter: (params: any, tx: CryptoCurrencyTransactionDto) =>
                `${tx.sentAmount ?? ''} ${tx.sentCurrency ?? ''}`,
        },
        {
            field: 'feeAmount',
            headerName: 'Fee',
            //width: 20,
            flex: 1,
            valueGetter: (params: any, tx: CryptoCurrencyTransactionDto) =>
                `${tx.feeAmount ?? ''} ${tx.feeCurrency ?? ''}`,
        },
        { field: 'account', headerName: 'Account', flex: 1 },
        { field: 'note', headerName: 'Note', flex: 2 },
        {
            field: 'actions',
            headerName: 'Actions',
            flex: 0,
            sortable: false,
            renderCell: (params: GridRenderCellParams) => (
                <>
                    <IconButton
                        onClick={() => handleEditTransaction(params.row as CryptoCurrencyTransactionDto)}
                        disabled={deleting !== null}  // Disable the edit button when deleting is in progress
                    >
                        <Edit />
                    </IconButton>
                    <IconButton
                        onClick={() => handleDeleteTransaction((params.row as CryptoCurrencyTransactionDto).id)}
                        disabled={deleting === (params.row as CryptoCurrencyTransactionDto).id}  // Disable only the delete button for the transaction being deleted
                    >
                        <Delete />
                    </IconButton>
                </>
            ),
        },
    ];

    return (
        <div style={{ padding: '20px', fontSize: '0.875rem' }}>
            <Typography variant="h5" gutterBottom>Wallets</Typography>
            <List component={Paper} style={{ marginBottom: '20px' }}>
                {wallets.map(wallet => (
                    <ListItem button key={wallet.id} onClick={() => handleWalletClick(wallet)}>
                        <ListItemText primary={wallet.name} />
                        <ListItemSecondaryAction>
                            <IconButton edge="end" onClick={() => handleDeleteWallet(wallet.id)}>
                                <Delete />
                            </IconButton>
                        </ListItemSecondaryAction>
                    </ListItem>
                ))}
            </List>

            {selectedWallet && (
                <div style={{ marginTop: '20px' }}>
                    <Box display="flex" justifyContent="space-between" mb={2}>
                        <Button
                            variant="contained"
                            color="primary"
                            startIcon={<Add />}
                            onClick={handleAddTransaction}
                        >
                            Add Manual Transaction
                        </Button>
                        <Box display="flex" alignItems="center">
                            <FormControl variant="outlined" size="small" sx={{ minWidth: 120, mr: 2 }}>
                                <InputLabel>Export Format</InputLabel>
                                <Select
                                    value={exportFormat}
                                    onChange={(e) => setExportFormat(e.target.value)}
                                    label="Export Format"
                                >
                                    <MenuItem value="xlsx">Excel (.xlsx)</MenuItem>
                                    <MenuItem value="csv">CSV (.csv)</MenuItem>
                                </Select>
                            </FormControl>
                            <Button
                                variant="contained"
                                color="secondary"
                                startIcon={<Download />}
                                onClick={handleExport}
                            >
                                Export
                            </Button>
                        </Box>
                    </Box>
                    <Typography variant="h6">Transactions for {selectedWallet.name}</Typography>
                    <div style={{ width: '100%' }}>
                        <DataGrid
                            rows={transactions}
                            columns={columns}
                            initialState={{
                                pagination: {
                                    paginationModel: {
                                        pageSize: 25,
                                    },
                                },
                            }}
                            pageSizeOptions={[10, 25, 100]}
                            autoHeight
                            checkboxSelection
                            disableRowSelectionOnClick
                        />
                    </div>
                </div>
            )}

            <Dialog open={dialogOpen} onClose={handleDialogClose}>
                <DialogTitle>{isEditing ? 'Edit Transaction' : 'Add Transaction'}</DialogTitle>
                <DialogContent>
                    {errorMessage && (
                        <Typography color="error" variant="body2" gutterBottom>
                            {errorMessage}
                        </Typography>
                    )}
                    <TransactionForm
                        transaction={currentTransaction}
                        onChange={setCurrentTransaction}
                        onSave={handleSaveTransaction}
                        isEditing={isEditing}
                    />
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleDialogClose} color="primary">Cancel</Button>
                </DialogActions>
            </Dialog>

            <AddTransactionDialog
                open={transactionDialogOpen}
                onClose={handleTransactionDialogClose}
                onTransactionAdded={handleTransactionDialogClose}
                selectedWalletId={selectedWallet?.id || 0}
                showSnackbar={showSnackbar}  // Pass showSnackbar to the dialog
            />

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
        </div>
    );
};

export default WalletList;
