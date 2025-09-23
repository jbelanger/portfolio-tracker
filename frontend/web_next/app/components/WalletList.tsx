'use client';

import React, { useState, useEffect } from 'react';
import {
    IconButton, Typography, Snackbar, Alert,
    Dialog, DialogTitle, DialogContent, DialogActions, Button, Box, MenuItem, Select, InputLabel, FormControl
} from '@mui/material';
import { Delete, Edit, Add, Download } from '@mui/icons-material';
import { DataGrid, GridColDef, GridRenderCellParams } from '@mui/x-data-grid';
import { Wallet, CryptoCurrencyTransactionDto } from '../types/types';
import dayjs from 'dayjs';
import TransactionForm from './TransactionForm';
import AddTransactionDialog from './AddTransactionDialog';
import BulkEditDialog from './BulkEditDialog';
import { TransactionAPI } from '@/app/api/TransactionAPI';
import { useSnackbar } from '../context/SnackbarContext';

interface WalletListProps {
    portfolioId: number;
    allWallets?: boolean;
    wallet?: Wallet | null;
}

const WalletList: React.FC<WalletListProps> = ({ portfolioId, allWallets = false, wallet = null }) => {    

    const [transactions, setTransactions] = useState<CryptoCurrencyTransactionDto[]>([]);
    const [isEditing, setIsEditing] = useState(false);
    const [currentTransaction, setCurrentTransaction] = useState<CryptoCurrencyTransactionDto | null>(null);
    const [dialogOpen, setDialogOpen] = useState(false);
    const [exportFormat, setExportFormat] = useState('xlsx');
    const [transactionDialogOpen, setTransactionDialogOpen] = useState(false);
    const [deleting, setDeleting] = useState<number | null>(null);
    const [selectedTransactions, setSelectedTransactions] = useState<number[]>([]);
    const [bulkEditDialogOpen, setBulkEditDialogOpen] = useState(false);
    const [bulkAction, setBulkAction] = useState<string | null>(null);
    const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false);
    const { showSnackbar } = useSnackbar();

    const fetchTransactions = async (walletId: number | null) => {
        const result = await TransactionAPI.fetchTransactions(Number(portfolioId), walletId);

        if (result.isOk()) {
            setTransactions(result.value);
        } else {
            console.error('Error fetching transactions:', result.error);
            showSnackbar('Error fetching transactions from wallet(s).', 'error');
        }
    };

    useEffect(() => {
        fetchTransactions(wallet?.id ?? null);
    }, [allWallets, wallet, portfolioId]);

    const handleDeleteTransaction = async (transactionId: number) => {
        if (!wallet || !portfolioId) return;

        setDeleting(transactionId);

        const result = await TransactionAPI.deleteTransaction(Number(portfolioId), wallet.id, transactionId);

        if (result.isOk()) {
            setTransactions(transactions.filter(transaction => transaction.id !== transactionId));
            showSnackbar('Transaction deleted successfully.', 'success');
        } else {
            console.error('Error deleting transaction:', result.error);
            showSnackbar('Error deleting transaction. Please try again.', 'error');
        }

        setDeleting(null);
    };

    const handleBulkDelete = async () => {
        if (!wallet || !portfolioId) return;

        const result = await TransactionAPI.bulkDeleteTransactions(Number(portfolioId), wallet.id, selectedTransactions);

        if (result.isOk()) {
            setTransactions(transactions.filter(transaction => !selectedTransactions.includes(transaction.id)));
            setSelectedTransactions([]);
            showSnackbar('Selected transactions deleted successfully.', 'success');
        } else {
            console.error('Error deleting transactions:', result.error);
            showSnackbar('Error deleting transactions. Please try again.', 'error');
        }
    };

    const handleEditTransaction = (transaction: CryptoCurrencyTransactionDto) => {
        setCurrentTransaction(transaction);
        setIsEditing(true);
        setDialogOpen(true);
    };

    const handleBulkEdit = async (updatedFields: Partial<CryptoCurrencyTransactionDto>) => {
        if (!wallet || !portfolioId) return;

        const transactionsToUpdate = selectedTransactions.map(id => ({
            ...transactions.find(t => t.id === id),
            ...updatedFields,
        }) as CryptoCurrencyTransactionDto);

        const result = await TransactionAPI.bulkEditTransactions(Number(portfolioId), wallet.id, transactionsToUpdate);

        if (result.isOk()) {
            const updatedTransactions = transactions.map(tx =>
                selectedTransactions.includes(tx.id)
                    ? { ...tx, ...updatedFields }
                    : tx
            );
            setTransactions(updatedTransactions);
            setBulkEditDialogOpen(false);
            setSelectedTransactions([]);
            showSnackbar('Selected transactions updated successfully.', 'success');
        } else {
            console.error('Error updating transactions:', result.error);
            showSnackbar('Error updating transactions. Please try again.', 'error');
        }
    };

    const handleDialogClose = () => {
        setDialogOpen(false);
        setCurrentTransaction(null);
    };

    const handleSaveTransaction = async () => {
        if (!currentTransaction || !wallet || !portfolioId) return;

        const result = isEditing
            ? await TransactionAPI.editTransaction(portfolioId, wallet.id, currentTransaction.id, currentTransaction)
            : await TransactionAPI.createTransaction(portfolioId, wallet.id, currentTransaction);

        if (result.isOk()) {
            await fetchTransactions(wallet.id);
            handleDialogClose();
        } else {
            console.error('Error saving transaction:', result.error);
            showSnackbar(`Error saving transaction: ${result.error}`, 'error');
        }
    };

    const handleAddTransaction = () => {
        setTransactionDialogOpen(true);
    };

    const handleTransactionDialogClose = () => {
        setTransactionDialogOpen(false);
        fetchTransactions(wallet?.id || 0);
    };

    const handleBulkAction = (action: string | null) => {
        if (action === 'bulkEdit') {
            setBulkEditDialogOpen(true);
        } else if (action === 'bulkDelete') {
            setConfirmDeleteOpen(true);
        }
        setBulkAction("");
    };

    const handleExport = async () => {
        if (!wallet || !portfolioId) return;

        try {
            const result = await TransactionAPI.exportTransactions(Number(portfolioId), wallet.id, exportFormat);

            if (result.isOk()) {
                const url = window.URL.createObjectURL(result.value);
                const link = document.createElement('a');
                link.href = url;
                link.setAttribute('download', `transactions.${exportFormat}`);
                document.body.appendChild(link);
                link.click();
            } else {
                throw new Error('Failed to export transactions');
            }
        } catch (error) {
            console.error('Error exporting transactions:', error);
        }
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
                        disabled={deleting !== null}
                    >
                        <Edit />
                    </IconButton>
                    <IconButton
                        onClick={() => handleDeleteTransaction((params.row as CryptoCurrencyTransactionDto).id)}
                        disabled={deleting === (params.row as CryptoCurrencyTransactionDto).id}
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
                                onRowSelectionModelChange={(newSelection) => setSelectedTransactions(Array.from(newSelection.ids) as number[])}
                            />
                        </div>
                    </div>
                </div>
            )}

            <Dialog open={dialogOpen} onClose={handleDialogClose}>
                <DialogTitle>{isEditing ? 'Edit Transaction' : 'Add Transaction'}</DialogTitle>
                <DialogContent>
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
                portfolioId={portfolioId}
                open={transactionDialogOpen}
                onClose={handleTransactionDialogClose}
                onTransactionAdded={handleTransactionDialogClose}
                selectedWalletId={wallet?.id || 0}
            />
            <BulkEditDialog
                open={bulkEditDialogOpen}
                onClose={() => setBulkEditDialogOpen(false)}
                selectedTransactions={selectedTransactions}
            />
            <Dialog open={confirmDeleteOpen} onClose={() => setConfirmDeleteOpen(false)}>
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
                            handleBulkDelete();
                        }}
                        color="secondary"
                    >
                        Delete
                    </Button>
                </DialogActions>
            </Dialog>
        </div>
    );
};

export default WalletList;
