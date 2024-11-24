import React, { useState } from 'react';
import {
  Dialog, DialogTitle, DialogContent, DialogActions, Button, Box, Typography, RadioGroup, FormControl, FormControlLabel, Radio, InputLabel, Select, MenuItem
} from '@mui/material';
import { CsvFileImportType, CryptoCurrencyTransactionDto } from '../types/Wallet';
import apiClient from '../api/axios';
import TransactionForm from './TransactionForm';

interface TransactionDialogProps {
  open: boolean;
  onClose: () => void;
  onTransactionAdded: () => void;
  selectedWalletId: number;
  showSnackbar: (message: string, severity: 'success' | 'error') => void;
}

const AddTransactionDialog: React.FC<TransactionDialogProps> = ({ open, onClose, onTransactionAdded, selectedWalletId, showSnackbar }) => {
  const [importType, setImportType] = useState<CsvFileImportType>(CsvFileImportType.Kraken);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [transactionMode, setTransactionMode] = useState<'manual' | 'csv'>('csv');
  const [currentTransaction, setCurrentTransaction] = useState<CryptoCurrencyTransactionDto | null>(null);

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>): void => {
    if (event.target.files && event.target.files[0]) {
      setSelectedFile(event.target.files[0]);
    }
  };

  const handleImportTransactions = async (): Promise<void> => {
    if (!selectedFile) return;

    const formData = new FormData();
    formData.append('file', selectedFile);

    try {
      await apiClient.post(`/portfolios/1/wallets/${selectedWalletId}/transactions/upload-csv?csvImportType=${importType}`, formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });
      showSnackbar('Transactions imported successfully.', 'success');
      onTransactionAdded();
      onClose();
    } catch (error) {
      console.error('Error importing transactions:', error);
      showSnackbar('Error importing transactions. Please try again.', 'error');
    }
  };

  const handleSaveManualTransaction = async (): Promise<void> => {
    if (!currentTransaction) return;

    try {
      await apiClient.post(`/portfolios/1/wallets/${selectedWalletId}/transactions`, currentTransaction);
      showSnackbar('Transaction added successfully.', 'success');
      onTransactionAdded();
      onClose();
    } catch (error: any) {
      console.error('Error saving transaction:', error);
      const errorMessage = error.response?.data || 'An unexpected error occurred. Please try again.';
      showSnackbar(errorMessage, 'error');
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
            isEditing={false}
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
