import React, { useState, useEffect } from 'react';
import {
    IconButton, Typography, Snackbar, Alert,
    Dialog, DialogTitle, DialogContent, DialogActions, Button, Box, MenuItem, Select, InputLabel, FormControl
} from '@mui/material';
import { Delete, Edit, Add, Download } from '@mui/icons-material';
import { DataGrid, GridColDef, GridRenderCellParams } from '@mui/x-data-grid';
import apiClient from '../api/axios';
import { Wallet, CryptoCurrencyTransactionDto } from '../types/Wallet';
import dayjs from 'dayjs';
import TransactionForm from './TransactionForm'
import AddTransactionDialog from './AddTransactionDialog'
import BulkEditDialog from './BulkEditDialog';

interface WalletListProps {
    allWallets?: boolean;
    wallet?: Wallet | null;
}

const WalletList: React.FC<WalletListProps> = ({ allWallets = false, wallet = null }) => {
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
    const [selectedTransactions, setSelectedTransactions] = useState<number[]>([]);
    const [bulkEditDialogOpen, setBulkEditDialogOpen] = useState(false);
    const [bulkEditOption, setBulkEditOption] = useState(''); // Track the selected bulk edit option
    const [bulkAction, setBulkAction] = useState<string | null>(null); // Track the selected bulk action
    const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false);

    useEffect(() => {
        if (allWallets) {
            fetchTransactions(null);
        } else if (wallet) {
            fetchTransactions(wallet?.id);
        }
    }, [allWallets, wallet]);

    const fetchTransactions = async (walletId: number | null) => {
        try {
            if (walletId === null) {
                const response = await apiClient.get<CryptoCurrencyTransactionDto[]>('/portfolios/1/transactions'); // Replace with the appropriate API endpoint
                setTransactions(response.data);
            }
            else {
                const response = await apiClient.get<CryptoCurrencyTransactionDto[]>(`/portfolios/1/wallets/${walletId}/transactions`);
                setTransactions(response.data);
            }
        } catch (error) {
            console.error('Error fetching transactions:', error);
            showSnackbar('Error fetching transactions from wallet(s).', 'error');
        }
    };

    const handleDeleteTransaction = async (transactionId: number) => {

        try {
            // Optimistically update the state
            const updatedTransactions = transactions.filter(transaction => transaction.id !== transactionId);
            setTransactions(updatedTransactions);
            setDeleting(transactionId);  // Set the deleting state to the ID of the transaction being deleted

            if (wallet) {
                await apiClient.delete(`/portfolios/1/wallets/${wallet.id}/transactions/${transactionId}`);
                setTransactions(transactions.filter(transaction => transaction.id !== transactionId));
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

    const handleBulkDelete = async () => {
        try {
            await apiClient.delete(`/portfolios/1/wallets/${wallet?.id}/transactions/bulk-delete`, {
                data: selectedTransactions, // Send selected transaction IDs
            });
            setTransactions(transactions.filter(transaction => !selectedTransactions.includes(transaction.id)));
            setSelectedTransactions([]);  // Clear selection after deletion
            showSnackbar('Selected transactions deleted successfully.', 'success');
        } catch (error) {
            console.error('Error deleting transactions:', error);
            showSnackbar('Error deleting transactions. Please try again.', 'error');
        }
    };



    const handleEditTransaction = (transaction: CryptoCurrencyTransactionDto) => {
        setCurrentTransaction(transaction);
        setIsEditing(true);
        setDialogOpen(true);
    };


    const handleBulkEdit = async (updatedFields: Partial<CryptoCurrencyTransactionDto>) => {
        try {
            const transactionsToUpdate = selectedTransactions.map(id => ({
                ...transactions.find(t => t.id === id),
                ...updatedFields, // Apply the updated fields
            }));

            await apiClient.put(`/portfolios/1/wallets/${wallet?.id}/transactions/bulk-edit`, transactionsToUpdate);

            // Optionally, refetch transactions from the server
            await fetchTransactions(wallet?.id!);

            setBulkEditDialogOpen(false);
            setSelectedTransactions([]);  // Clear selection after edit
            showSnackbar('Selected transactions updated successfully.', 'success');
        } catch (error) {
            console.error('Error updating transactions:', error);
            showSnackbar('Error updating transactions. Please try again.', 'error');
        }
    };



    const handleDialogClose = () => {
        setDialogOpen(false);
        setCurrentTransaction(null);
    };

    const handleSaveTransaction = async () => {
        if (!currentTransaction || !wallet) return;
        try {
            setErrorMessage(null); // Clear previous error message

            if (isEditing) {
                // If editing, use PUT to update the transaction
                await apiClient.put(`/portfolios/1/wallets/${wallet.id}/transactions/${currentTransaction.id}`, currentTransaction);
            } else {
                // If not editing, use POST to create a new transaction
                await apiClient.post(`/portfolios/1/wallets/${wallet.id}/transactions`, currentTransaction);
            }

            // Refresh the transactions from the server only after a successful save
            await fetchTransactions(wallet.id);
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
        fetchTransactions(wallet?.id || 0); // Refresh transactions after dialog closes
    };

    const handleBulkAction = (action: string | null) => {
        if (action === 'bulkEdit') {
            setBulkEditDialogOpen(true);  // Open bulk edit dialog
        } else if (action === 'bulkDelete') {
            setConfirmDeleteOpen(true);  // Trigger bulk delete action
        }
        // Clear the selection after the action
        setBulkAction("");
    };

    const handleExport = async () => {
        if (!wallet) return;
        try {
            const response = await apiClient.get(`/portfolios/1/wallets/${wallet.id}/export?format=${exportFormat}`, {
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
        <div style={{ padding: '20px' }}>
            <Typography variant="h1" gutterBottom>{wallet?.name}</Typography>

            {wallet && (
                <div style={{ marginTop: '20px' }}>
                    <Box display="flex" justifyContent="space-between" mb={2}>
                        <Box display="flex" alignItems="left">
                            <Button
                                variant="contained"
                                color="primary"
                                startIcon={<Add />}
                                onClick={handleAddTransaction}
                                sx={{ mr: 2 }}
                            >
                                Add Transactions
                            </Button>
                            <FormControl variant="outlined" size="small" sx={{ minWidth: 150, mr: 2 }}>
                                <Select
                                    value={bulkAction}
                                    color='secondary'
                                    onChange={(e) => {
                                        setBulkAction(e.target.value);
                                        handleBulkAction(e.target.value);
                                    }}
                                    displayEmpty
                                    sx={{
                                        //backgroundColor: '#1976d2',
                                        color: 'white',
                                        //textTransform: 'uppercase',
                                        '& .MuiSelect-icon': {
                                            color: 'white',
                                        },
                                        '& .MuiOutlinedInput-notchedOutline': {
                                            //borderColor: 'transparent',
                                        },
                                        '&:hover .MuiOutlinedInput-notchedOutline': {
                                            //borderColor: 'transparent',
                                        },
                                        '&:focus .MuiOutlinedInput-notchedOutline': {
                                            //borderColor: 'transparent',
                                        },
                                    }}
                                //renderValue={(selected) => (selected ? selected : 'Bulk Actions')}
                                >
                                    <MenuItem value="" disabled>Bulk Action</MenuItem>
                                    <MenuItem value="bulkEdit">Bulk Edit</MenuItem>
                                    <MenuItem value="bulkDelete">Bulk Delete</MenuItem>
                                </Select>
                            </FormControl>
                        </Box>
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
                    <Typography variant="h6">Transactions for {wallet.name}</Typography>
                    <div style={{ display: 'flex', height: '100%' }}>
                        <div style={{ flexGrow: 1 }}>
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
                                disableRowSelectionOnClick
                                checkboxSelection
                                onRowSelectionModelChange={(newSelection) => setSelectedTransactions(newSelection as number[])}

                            />
                        </div>
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
                selectedWalletId={wallet?.id || 0}
                showSnackbar={showSnackbar}  // Pass showSnackbar to the dialog
            />
            <BulkEditDialog
                open={bulkEditDialogOpen}
                onClose={() => setBulkEditDialogOpen(false)}
                selectedTransactions={selectedTransactions}
            />
            <Dialog
                open={confirmDeleteOpen}
                onClose={() => setConfirmDeleteOpen(false)}
            >
                <DialogTitle>Confirm Bulk Delete</DialogTitle>
                <DialogContent>
                    <Typography>Are you sure you want to delete the selected transactions?</Typography>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setConfirmDeleteOpen(false)} color="primary">
                        Cancel
                    </Button>
                    <Button
                        onClick={() => {
                            setConfirmDeleteOpen(false);
                            handleBulkDelete();  // Perform bulk delete after confirmation
                        }}
                        color="secondary"
                    >
                        Delete
                    </Button>
                </DialogActions>
            </Dialog>


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
