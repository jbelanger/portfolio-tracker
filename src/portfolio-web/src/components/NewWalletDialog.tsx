import React, { useState } from 'react';
import {
    Dialog, DialogTitle, DialogContent, DialogActions, Button, TextField
} from '@mui/material';
import apiClient from '../api/axios';

interface NewWalletDialogProps {
    open: boolean;
    onClose: () => void;
    onWalletCreated: () => void;
    showSnackbar: (message: string, severity: 'success' | 'error') => void;
}

const NewWalletDialog: React.FC<NewWalletDialogProps> = ({ open, onClose, onWalletCreated, showSnackbar }) => {
    const [walletName, setWalletName] = useState('');

    const handleCreateWallet = async () => {
        if (!walletName.trim()) {
            showSnackbar('Wallet name cannot be empty.', 'error');
            return;
        }

        try {
            await apiClient.post('/portfolios/1/wallets', { name: walletName });  // Replace 1 with your portfolio ID
            showSnackbar('Wallet created successfully.', 'success');
            onWalletCreated();
            onClose();
        } catch (error) {
            console.error('Error creating wallet:', error);
            showSnackbar('Error creating wallet. Please try again.', 'error');
        }
    };

    return (
        <Dialog open={open} onClose={onClose}>
            <DialogTitle>Create New Wallet</DialogTitle>
            <DialogContent>
                <TextField
                    autoFocus
                    fullWidth
                    margin="dense"
                    label="Wallet Name"
                    value={walletName}
                    onChange={(e) => setWalletName(e.target.value)}
                />
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose} color="primary">Cancel</Button>
                <Button onClick={handleCreateWallet} color="primary">Create</Button>
            </DialogActions>
        </Dialog>
    );
};

export default NewWalletDialog;
