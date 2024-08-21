import React, { useState } from 'react';
import {
    Dialog, DialogTitle, DialogContent, DialogActions, Button, Box, Typography, RadioGroup, FormControl, FormControlLabel, Radio, InputLabel, Select, MenuItem
} from '@mui/material';
import { CsvFileImportType, CryptoCurrencyTransactionDto } from '../types/Wallet';
import apiClient from '../api/axios';
import TransactionForm from './TransactionForm';
import { Snackbar, Alert } from '@mui/material';

interface TransactionDialogProps {
    open: boolean;
    onClose: () => void;
    onTransactionAdded: () => void;
    selectedWalletId: number;
    showSnackbar: (message: string, severity: 'success' | 'error') => void; // Add this prop
}

const AddTransactionDialog: React.FC<TransactionDialogProps> = ({ open, onClose, onTransactionAdded, selectedWalletId, showSnackbar }) => {
    const [importType, setImportType] = useState<CsvFileImportType>(CsvFileImportType.Kraken); // Default import type
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [transactionMode, setTransactionMode] = useState<'manual' | 'csv'>('csv'); // Default to CSV import
    const [currentTransaction, setCurrentTransaction] = useState<CryptoCurrencyTransactionDto | null>(null);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);
    const [snackbarOpen, setSnackbarOpen] = useState(false);
    const [snackbarMessage, setSnackbarMessage] = useState('');
    const [snackbarSeverity, setSnackbarSeverity] = useState<'success' | 'error'>('success');

    // const showSnackbar = (message: string, severity: 'success' | 'error' = 'success') => {
    //     setSnackbarMessage(message);
    //     setSnackbarSeverity(severity);
    //     setSnackbarOpen(true);
    // };

    const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        if (event.target.files && event.target.files[0]) {
            setSelectedFile(event.target.files[0]);
        }
    };

    const handleImportTransactions = async () => {
        if (!selectedFile) return;

        const formData = new FormData();
        formData.append('file', selectedFile);

        try {
            await apiClient.post(`/portfolios/1/wallets/${selectedWalletId}/transactions/upload-csv?csvImportType=${importType}`, formData, {
                headers: {
                    'Content-Type': 'multipart/form-data',
                },
            });
            showSnackbar('Transactions imported successfully.', 'success'); // Show success message
            onTransactionAdded(); // Trigger a refresh in the parent component
            onClose(); // Close the dialog
        } catch (error) {
            console.error('Error importing transactions:', error);
            showSnackbar('Error importing transactions. Please try again.', 'error'); // Show error message
        }
    };

    const handleSaveManualTransaction = async () => {
        if (!currentTransaction) return;

        try {
            await apiClient.post(`/portfolios/1/wallets/${selectedWalletId}/transactions`, currentTransaction);
            showSnackbar('Transaction added successfully.', 'success'); // Show success message
            onTransactionAdded(); // Trigger a refresh in the parent component
            onClose(); // Close the dialog
        } catch (error: any) {
            console.error('Error saving transaction:', error);
            if (error.response && error.response.data && typeof error.response.data === 'string') {
                showSnackbar(error.response.data, 'error'); // Show server error message
            } else {
                showSnackbar('An unexpected error occurred. Please try again.', 'error'); // Show generic error message
            }
        }
    };

    const renderCsvImportSection = () => (
        <>
            <Typography variant="h6">CSV Import</Typography>
            <FormControl fullWidth margin="dense">
                <InputLabel>Import Type</InputLabel>
                <Select
                    value={importType}
                    onChange={(e) => setImportType(e.target.value as CsvFileImportType)}
                    label="Import Type"
                >
                    <MenuItem value={CsvFileImportType.Kraken}>Kraken</MenuItem>
                    <MenuItem value={CsvFileImportType.Coinbase}>Coinbase</MenuItem>
                    <MenuItem value={CsvFileImportType.Binance}>Binance</MenuItem>
                    {/* Add more import types as needed */}
                </Select>
            </FormControl>
            <input
                accept=".csv"
                style={{ display: 'none' }}
                id="csv-file-input"
                type="file"
                onChange={handleFileChange}
            />
            <label htmlFor="csv-file-input">
                <Button
                    variant="contained"
                    color="primary"
                    component="span"
                    //disabled={!selectedFile}
                    style={{ marginTop: '15px' }}
                >
                    Choose CSV File
                </Button>
            </label>
            {selectedFile && (
                <Typography variant="body2" style={{ marginTop: '10px' }}>
                    Selected File: {selectedFile.name}
                </Typography>
            )}
            <Button
                variant="contained"
                color="secondary"
                onClick={handleImportTransactions}
                disabled={!selectedFile}
                style={{ marginTop: '15px' }}
            >
                Import Transactions
            </Button>
        </>
    );

    return (
        <Dialog open={open} onClose={onClose}>
            <DialogTitle>Add or Import Transactions</DialogTitle>
            <DialogContent>
                {errorMessage && (
                    <Typography color="error" variant="body2" gutterBottom>
                        {errorMessage}
                    </Typography>
                )}
                <Box marginBottom={2}>
                    <Typography variant="subtitle1">Choose Transaction Mode</Typography>
                    <RadioGroup
                        row
                        value={transactionMode}
                        onChange={(e) => setTransactionMode(e.target.value as 'manual' | 'csv')}
                    >
                        <FormControlLabel value="csv" control={<Radio />} label="CSV Import" />
                        <FormControlLabel value="manual" control={<Radio />} label="Manual Entry" />
                    </RadioGroup>
                </Box>
                {transactionMode === 'csv' ? (
                    renderCsvImportSection()
                ) : (
                    <TransactionForm
                        transaction={currentTransaction}
                        onChange={setCurrentTransaction}
                        onSave={handleSaveManualTransaction}
                        isEditing={false} // Assuming we're adding a new transaction here
                    />
                )}
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose} color="primary">Cancel</Button>
            </DialogActions>
        </Dialog>
    );
};

export default AddTransactionDialog;