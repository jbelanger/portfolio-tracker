// app/components/Navbar.tsx

'use client';

import React, { useState } from 'react';
import { AppBar, Toolbar, IconButton, Menu, MenuItem, Typography, Switch } from '@mui/material';
import { AccountCircle } from '@mui/icons-material';

interface NavbarProps {
  darkMode: boolean;
  onToggleDarkMode: () => void;
}

const Navbar: React.FC<NavbarProps> = ({ darkMode, onToggleDarkMode }) => {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  const handleMenu = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  return (
    <AppBar position="static">
      <Toolbar variant="regular" disableGutters>
        <Typography variant="h6" style={{ flexGrow: 1 }}>
          Portfolio
        </Typography>
        <div>
          <IconButton edge="end" color="inherit" onClick={handleMenu}>
            <AccountCircle />
          </IconButton>
          <Menu
            anchorEl={anchorEl}
            keepMounted
            open={Boolean(anchorEl)}
            onClose={handleClose}
          >
            <MenuItem>
              <Typography variant="body1">Dark Mode</Typography>
              <Switch checked={darkMode} onChange={onToggleDarkMode} />
            </MenuItem>
            <MenuItem onClick={handleClose}>Logout</MenuItem>
          </Menu>
        </div>
      </Toolbar>
    </AppBar>
  );
};

export default Navbar;
