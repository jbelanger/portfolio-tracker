import React, { useState, useEffect } from 'react';
import {
    List, ListItem, ListItemText, Button, Divider, Box, Toolbar, IconButton, ListItemIcon, Tooltip
} from '@mui/material';
import { Add, ChevronLeft, ChevronRight, Wallet, AccountBalanceWallet } from '@mui/icons-material';
import { Link } from 'react-router-dom';
import apiClient from '../api/axios';
import { Wallet as WalletType } from '../types/Wallet';
import NewWalletDialog from './NewWalletDialog';
import DeleteWalletButton from './DeleteWalletButton';

interface LeftMenuProps {
    onSelectWallet: (wallet: WalletType | null) => void;
    showSnackbar: (message: string, severity: 'success' | 'error') => void;
    onWalletDeleted: () => void;  // Add this prop to handle wallet deletion
}

const drawerWidthExpanded = 240;
const drawerWidthCollapsed = 60;

const LeftMenu: React.FC<LeftMenuProps> = ({ onSelectWallet, showSnackbar, onWalletDeleted }) => {
    const [wallets, setWallets] = useState<WalletType[]>([]);
    const [newWalletDialogOpen, setNewWalletDialogOpen] = useState(false);
    const [menuCollapsed, setMenuCollapsed] = useState(false); // State for collapse/expand

    useEffect(() => {
        fetchWallets();
    }, []);

    const fetchWallets = async () => {
        try {
            const response = await apiClient.get<WalletType[]>('/portfolios/1/wallets');  // Replace 1 with your portfolio ID
            setWallets(response.data);
        } catch (error) {
            console.error('Error fetching wallets:', error);
        }
    };

    const handleWalletCreated = () => {
        setNewWalletDialogOpen(false);
        fetchWallets();
    };

    const handleAddWallet = () => {
        setNewWalletDialogOpen(true);
    };

    const toggleMenuCollapse = () => {
        setMenuCollapsed(!menuCollapsed);
    };

    return (
        <Box
            sx={{
                width: menuCollapsed ? drawerWidthCollapsed : drawerWidthExpanded,
                flexShrink: 0,
                whiteSpace: 'nowrap',
                transition: 'width 0.3s',
                [`& .MuiDrawer-paper`]: {
                    width: menuCollapsed ? drawerWidthCollapsed : drawerWidthExpanded,
                    boxSizing: 'border-box',
                    transition: 'width 0.3s'
                },
            }}
        >
            <Toolbar />
            <Box sx={{ overflow: 'auto' }}>
                <List component="nav">
                    <ListItem button onClick={toggleMenuCollapse}>
                        <ListItemIcon>
                            {menuCollapsed ? <ChevronRight /> : <ChevronLeft />}
                        </ListItemIcon>
                        {!menuCollapsed && <ListItemText primary="Collapse Menu" />}
                    </ListItem>
                    <Divider />
                    <ListItem button component={Link} to="/" onClick={() => onSelectWallet(null)}>
                        <Tooltip title="All Wallets" placement="right">
                            <ListItemIcon>
                                <AccountBalanceWallet />
                            </ListItemIcon>
                        </Tooltip>
                        {!menuCollapsed && <ListItemText primary="All Wallets" />}
                    </ListItem>
                    <Divider />
                    {wallets.map((wallet) => (
                        <ListItem
                            button
                            key={wallet.id}
                            component={Link}
                            to={`/wallets/${wallet.id}`}
                            onClick={() => onSelectWallet(wallet)}
                            sx={{ display: 'flex', justifyContent: 'space-between' }} // Aligns delete button to the right
                        >
                            <Box sx={{ display: 'flex', alignItems: 'center' }}>
                                <Tooltip title={wallet.name} placement="right">
                                    <ListItemIcon>
                                        <Wallet />
                                    </ListItemIcon>
                                </Tooltip>
                                {!menuCollapsed && <ListItemText primary={wallet.name} />}
                            </Box>
                            {!menuCollapsed && (
                                <DeleteWalletButton 
                                    wallet={wallet} 
                                    onDelete={() => {
                                        fetchWallets();
                                        onWalletDeleted(); // Trigger redirect to the main page
                                    }} 
                                    showSnackbar={showSnackbar} 
                                />
                            )}
                        </ListItem>
                    ))}
                    <Divider />
                    <ListItem>
                        <Button
                            variant="contained"
                            color="primary"
                            startIcon={<Add />}
                            onClick={handleAddWallet}
                            fullWidth={!menuCollapsed}
                            sx={{ justifyContent: menuCollapsed ? 'center' : 'flex-start' }}
                        >
                            {!menuCollapsed && "Add Wallet"}
                        </Button>
                    </ListItem>
                </List>
            </Box>
            <NewWalletDialog
                open={newWalletDialogOpen}
                onClose={() => setNewWalletDialogOpen(false)}
                onWalletCreated={handleWalletCreated}
                showSnackbar={showSnackbar}
            />
        </Box>
    );
};

export default LeftMenu;
