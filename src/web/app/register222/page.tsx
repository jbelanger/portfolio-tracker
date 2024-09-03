'use client';

import React from 'react';
import { useForm } from 'react-hook-form';
import { Box, Button, TextField, Typography } from '@mui/material';

const RegisterPage: React.FC = () => {
  const { register, handleSubmit } = useForm();

  const onSubmit = async (data: any) => {
    // Send data to the backend API for registration
    const response = await fetch('/api/register', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
    });

    if (response.ok) {
      // Handle successful registration, redirect, or show a success message
    } else {
      // Handle registration failure
    }
  };

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', mt: 8 }}>
      <Typography variant="h4" gutterBottom>
        Register
      </Typography>
      <form onSubmit={handleSubmit(onSubmit)} style={{ width: '100%', maxWidth: 400 }}>
        <TextField
          label="Email"
          type="email"
          fullWidth
          margin="normal"
          {...register('email', { required: true })}
        />
        <TextField
          label="Password"
          type="password"
          fullWidth
          margin="normal"
          {...register('password', { required: true })}
        />
        <Button type="submit" variant="contained" fullWidth sx={{ mt: 2 }}>
          Register
        </Button>
      </form>
    </Box>
  );
};

export default RegisterPage;
