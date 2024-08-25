import React from 'react';
import { Box, Typography } from '@mui/material';
import HoldingsList from './HoldingsList';

// const MainPage: React.FC = () => (
//     <Box sx={{ padding: '20px' }}>
//         <Typography variant="h4" gutterBottom>
//             Welcome to Your Portfolio
//         </Typography>
//         <Typography variant="body1">
//             Please select a wallet from the left menu to view its transactions, or create a new wallet to start tracking your cryptocurrency assets.
//         </Typography>
//     </Box>
// );

const MainPage: React.FC = () => (
    <HoldingsList />
  );

export default MainPage;
