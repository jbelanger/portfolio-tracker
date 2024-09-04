// app/components/LeftMenu.tsx

'use client';

import React, { useState, useEffect } from 'react';
import { Box, Toolbar, List, ListItem, ListItemText, Button, Divider, IconButton, ListItemIcon, ListItemButton, Tooltip } from '@mui/material';
import { Add, ChevronLeft, ChevronRight, Wallet, AccountBalanceWallet } from '@mui/icons-material';
import Link from 'next/link';
import NewWalletDialog from './NewWalletDialog';
import DeleteWalletButton from './DeleteWalletButton';
import { Wallet as WalletType } from '../types/Wallet';
import { WalletAPI } from '../api/WalletAPI';
import { useSnackbar } from '../context/SnackbarContext';

interface LeftMenuProps {
  portfolioId: number;
  onSelectWallet: (wallet: WalletType | null) => void;
  onWalletDeleted: () => void;
}

const LeftMenu: React.FC<LeftMenuProps> = ({ portfolioId, onSelectWallet, onWalletDeleted }) => {
  const [wallets, setWallets] = useState<WalletType[]>([]);
  const [newWalletDialogOpen, setNewWalletDialogOpen] = useState(false);
  const [menuCollapsed, setMenuCollapsed] = useState(false);
  const { showSnackbar } = useSnackbar();

  useEffect(() => {
    fetchWallets();
  }, []);

  const fetchWallets = async () => {
    const result = await WalletAPI.fetchWallets(portfolioId);
    if (result.isOk()) {
        const data = result.value;
        setWallets(data);
    } else {
      console.error('Error fetching wallets:', result.error);
      showSnackbar('Error fetching wallets. Please try again.', 'error');
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
        width: menuCollapsed ? 60 : 240,
        flexShrink: 0,
        whiteSpace: 'nowrap',
        transition: 'width 0.3s',
        [`& .MuiDrawer-paper`]: {
          width: menuCollapsed ? 60 : 240,
          boxSizing: 'border-box',
          transition: 'width 0.3s',
        },
      }}
    >
      <Toolbar />
      <Box sx={{ overflow: 'auto' }}>
        <List component="nav">
          <ListItemButton onClick={toggleMenuCollapse}>
            <ListItemIcon>
              {menuCollapsed ? <ChevronRight /> : <ChevronLeft />}
            </ListItemIcon>
            {!menuCollapsed && <ListItemText primary="Collapse Menu" />}
          </ListItemButton>
          <Divider />
          <ListItemButton component={Link} href="/" onClick={() => onSelectWallet(null)}>
            <Tooltip title="All Wallets" placement="right">
              <ListItemIcon>
                <AccountBalanceWallet />
              </ListItemIcon>
            </Tooltip>
            {!menuCollapsed && <ListItemText primary="All Wallets" />}
          </ListItemButton>
          <Divider />
          {wallets.map((wallet) => (
            <ListItemButton
              key={wallet.id}
              component={Link}
              href={`/wallets/${wallet.id}`}
              onClick={() => onSelectWallet(wallet)}
              sx={{ display: 'flex', justifyContent: 'space-between' }}
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
                  portfolioId={portfolioId}
                  wallet={wallet}
                  onDelete={() => {
                    fetchWallets();
                    onWalletDeleted();
                  }}
                />
              )}
            </ListItemButton>
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
      />
    </Box>
  );
};

export default LeftMenu;
