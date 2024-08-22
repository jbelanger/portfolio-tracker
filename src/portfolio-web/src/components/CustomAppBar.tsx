import React from 'react';
import AppBar from '@mui/material/AppBar';
import Toolbar from '@mui/material/Toolbar';
import IconButton from '@mui/material/IconButton';
import Typography from '@mui/material/Typography';
import { Box } from '@mui/material';
import AccountCircle from '@mui/icons-material/AccountCircle';
import Settings from '@mui/icons-material/Settings';
import CodeIcon from '@mui/icons-material/Code'; // Placeholder for logo

const CustomAppBar: React.FC = () => {
  return (
    <AppBar position="static" sx={{ background: 'linear-gradient(90deg, #007ACC, #1E1E1E)', padding: '0 16px', boxShadow: 'none' }}>
      <Toolbar disableGutters>
        {/* Logo and App Name */}
        <Box display="flex" alignItems="center" sx={{ flexGrow: 1 }}>
          <CodeIcon sx={{ fontSize: 36, mr: 2, color: '#23D18B' }} /> {/* Use your custom logo here */}
          <Typography variant="h6" sx={{ fontFamily: '"Fira Code", monospace', color: '#D4D4D4', fontWeight: 'bold' }}>
            CodeFlow
          </Typography>
        </Box>

        {/* Right-aligned Icons */}
        <Box display="flex">
          <IconButton size="large" edge="end" color="inherit" aria-label="settings" sx={{ color: '#D4D4D4' }}>
            <Settings />
          </IconButton>
          <IconButton size="large" edge="end" color="inherit" aria-label="account" sx={{ color: '#D4D4D4' }}>
            <AccountCircle />
          </IconButton>
        </Box>
      </Toolbar>
    </AppBar>
  );
};

export default CustomAppBar;
