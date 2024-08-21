import React from 'react';
import { Box, CssBaseline, Toolbar, Drawer, List, ListItem, ListItemText, AppBar, Typography, IconButton } from '@mui/material';
import { AccountCircle } from '@mui/icons-material';
import { Routes, Route, Link, BrowserRouter as Router } from 'react-router-dom';
import WalletList from './WalletList'; // Import your components/pages

const drawerWidth = 240;

interface LayoutProps {
    darkMode: boolean;
    onToggleDarkMode: () => void;
}

const Layout: React.FC<LayoutProps> = ({ darkMode, onToggleDarkMode }) => {
    return (
        <Router>
            <Box sx={{ display: 'flex' }}>
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
                <Drawer
                    variant="permanent"
                    sx={{
                        width: drawerWidth,
                        flexShrink: 0,
                        [`& .MuiDrawer-paper`]: { width: drawerWidth, boxSizing: 'border-box' },
                    }}
                >
                    <Toolbar />
                    <Box sx={{ overflow: 'auto' }}>
                        <List>
                            <ListItem button component={Link} to="/">
                                <ListItemText primary="Wallets" />
                            </ListItem>
                            <ListItem button component={Link} to="/another-page">
                                <ListItemText primary="Another Page" />
                            </ListItem>
                        </List>
                    </Box>
                </Drawer>
                <Box
                    component="main"
                    sx={{ flexGrow: 1, bgcolor: 'background.default', p: 3, ml: `${drawerWidth}px`, mt: 8 }}
                >
                    <Toolbar />
                    <Routes>
                        <Route path="/" element={<WalletList />} />
                        <Route path="/another-page" element={<Typography variant="h4">Another Page</Typography>} />                                                
                    </Routes>
                </Box>
            </Box>
        </Router>
    );
};

export default Layout;
