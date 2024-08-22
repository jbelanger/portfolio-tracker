import React, { useState, useEffect } from 'react';
import { Box, CssBaseline, Toolbar, AppBar, Typography, IconButton } from '@mui/material';
import { AccountCircle } from '@mui/icons-material';
import { Routes, Route, BrowserRouter as Router } from 'react-router-dom';
import WalletList from './WalletList'; // Import your components/pages
import LeftMenu from './LeftMenu'; // Import the LeftMenu component
import { Wallet } from '../types/Wallet';
import MainPage from './MainPage';

const drawerWidthExpanded = 240;
const drawerWidthCollapsed = 60;

interface LayoutProps {
    darkMode: boolean;
    onToggleDarkMode: () => void;
}

const Layout: React.FC<LayoutProps> = ({ darkMode, onToggleDarkMode }) => {
    const [selectedWallet, setSelectedWallet] = useState<Wallet | null>(null);
    const [menuCollapsed, setMenuCollapsed] = useState(false);

    const handleSelectWallet = (walletId: Wallet | null) => {
        setSelectedWallet(walletId);
    };

    const showSnackbar = (message: string, severity: 'success' | 'error') => {
        console.log(message, severity); // Replace with actual snackbar implementation
    };

        // Function to handle wallet deletion and navigate back to the main page
        const handleWalletDeleted = () => {
            showSnackbar('Wallet deleted successfully.', 'success');
            //navigate('/'); // Redirect to the main page
        };

        // Trigger a window resize event whenever the menu collapses or expands
        useEffect(() => {
            const resizeGrid = () => {
                window.dispatchEvent(new Event('resize'));
            };
            resizeGrid(); // Trigger resize on initial load
            window.addEventListener('resize', resizeGrid);
    
            return () => {
                window.removeEventListener('resize', resizeGrid);
            };
        }, [menuCollapsed]);

    return (
        <Router>
            <Box sx={{ display: 'flex', height: '100vh', overflow: 'hidden' }}>
                <CssBaseline />
                <AppBar position="fixed" sx={{ zIndex: (theme) => theme.zIndex.drawer + 1 }}>
                    <Toolbar>
                        <Typography variant="h6" noWrap sx={{ flexGrow: 1 }}>
                            Portfolio Manager
                        </Typography>
                        <IconButton edge="end" color="inherit">
                            <AccountCircle />
                        </IconButton>
                    </Toolbar>
                </AppBar>
                <LeftMenu onWalletDeleted={handleWalletDeleted}  onSelectWallet={handleSelectWallet} showSnackbar={showSnackbar} />
                <Box
                    component="main"
                    sx={{
                        flexGrow: 1,
                        bgcolor: 'background.default',
                        p: 3,
                        transition: 'margin-left 0.3s'
                    }}
                >
                    <Toolbar />
                    <Routes>
                    <Route path="/" element={<MainPage />} />
                        <Route path="/" element={<WalletList allWallets />} />
                        {selectedWallet !== null && (
                            <Route
                                path={`/wallets/${selectedWallet.id}`}
                                element={<WalletList wallet={selectedWallet} />}
                            />
                        )}
                        <Route path="/another-page" element={<Typography variant="h4">Another Page</Typography>} />
                    </Routes>
                </Box>
            </Box>
        </Router>
    );
};

export default Layout;
