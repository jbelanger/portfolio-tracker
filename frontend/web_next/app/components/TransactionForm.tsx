// app/components/TransactionForm.tsx

'use client';

import React from 'react';
import { TextField, MenuItem, Select, InputLabel, FormControl, Button, Box } from '@mui/material';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { DateTimePicker } from '@mui/x-date-pickers';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import dayjs from 'dayjs';
import { CryptoCurrencyTransactionDto } from '../types/types';

interface TransactionFormProps {
  transaction: CryptoCurrencyTransactionDto | null;
  onChange: (transaction: CryptoCurrencyTransactionDto) => void;
  onSave: () => void;
  isEditing: boolean;
}

const TransactionForm: React.FC<TransactionFormProps> = ({ transaction, onChange, onSave, isEditing }) => (
  <>
    <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 2 }}>
      <Box>
        <FormControl fullWidth margin="dense">
          <InputLabel>Transaction Type</InputLabel>
          <Select
            fullWidth
            value={transaction?.type || ''}
            disabled={isEditing}
            onChange={(e) =>
              onChange({
                ...transaction!,
                type: e.target.value,
              })
            }
            label="Transaction Type"
          >
            <MenuItem value="Deposit">Deposit</MenuItem>
            <MenuItem value="Withdrawal">Withdrawal</MenuItem>
            <MenuItem value="Trade">Trade</MenuItem>
          </Select>
        </FormControl>
      </Grid>
      <Grid xs={12} sm={6}>
        <LocalizationProvider dateAdapter={AdapterDayjs}>
          <DateTimePicker
            label="Transaction Date"
            value={dayjs(transaction?.dateTime || new Date())}
            onChange={(date) => {
              onChange({
                ...transaction!,
                dateTime: date?.toDate() || new Date(),
              });
            }}
            slotProps={{ textField: { margin: "dense", fullWidth: true } }}
          />
        </LocalizationProvider>
      </Grid>
      <Grid xs={12} sm={6}>
        <TextField
          fullWidth
          margin="dense"
          label="Received Amount"
          type="number"
          value={transaction?.receivedAmount || ''}
          onChange={(e) => onChange({ ...transaction!, receivedAmount: parseFloat(e.target.value) })}
          InputProps={{ style: { fontSize: '0.875rem' } }}
          disabled={transaction?.type === "Withdrawal"}
        />
      </Grid>
      <Grid xs={12} sm={6}>
        <TextField
          fullWidth
          margin="dense"
          label="Received Currency"
          value={transaction?.receivedCurrency || ''}
          onChange={(e) => onChange({ ...transaction!, receivedCurrency: e.target.value })}
          InputProps={{ style: { fontSize: '0.875rem' } }}
          disabled={transaction?.type === "Withdrawal"}
        />
      </Grid>
      <Grid xs={12} sm={6}>
        <TextField
          fullWidth
          margin="dense"
          label="Sent Amount"
          type="number"
          value={transaction?.sentAmount || ''}
          onChange={(e) => onChange({ ...transaction!, sentAmount: parseFloat(e.target.value) })}
          InputProps={{ style: { fontSize: '0.875rem' } }}
          disabled={transaction?.type === "Deposit"}
        />
      </Grid>
      <Grid xs={12} sm={6}>
        <TextField
          fullWidth
          margin="dense"
          label="Sent Currency"
          value={transaction?.sentCurrency || ''}
          onChange={(e) => onChange({ ...transaction!, sentCurrency: e.target.value })}
          InputProps={{ style: { fontSize: '0.875rem' } }}
          disabled={transaction?.type === "Deposit"}
        />
      </Grid>
      <Grid xs={12} sm={6}>
        <TextField
          fullWidth
          margin="dense"
          label="Fee Amount"
          type="number"
          value={transaction?.feeAmount || ''}
          onChange={(e) => onChange({ ...transaction!, feeAmount: parseFloat(e.target.value) })}
          InputProps={{ style: { fontSize: '0.875rem' } }}
        />
      </Grid>
      <Grid xs={12} sm={6}>
        <TextField
          fullWidth
          margin="dense"
          label="Fee Currency"
          value={transaction?.feeCurrency || ''}
          onChange={(e) => onChange({ ...transaction!, feeCurrency: e.target.value })}
          InputProps={{ style: { fontSize: '0.875rem' } }}
        />
      </Grid>
      <Grid xs={12} sm={6}>
        <TextField
          fullWidth
          margin="dense"
          label="Account"
          value={transaction?.account || ''}
          onChange={(e) => onChange({ ...transaction!, account: e.target.value })}
          InputProps={{ style: { fontSize: '0.875rem' } }}
        />
      </Grid>
      <Grid xs={12}>
        <TextField
          multiline
          rows={5}
          fullWidth
          margin="dense"
          label="Note"
          value={transaction?.note || ''}
          onChange={(e) => onChange({ ...transaction!, note: e.target.value })}
          InputProps={{ style: { fontSize: '0.875rem' } }}
        />
      </Grid>
    </Grid>
    <Button
      variant="contained"
      color="primary"
      onClick={onSave}
      style={{ marginTop: '15px' }}
    >
      {isEditing ? 'Save Changes' : 'Add Transaction'}
    </Button>
  </>
);

export default TransactionForm;
