import React from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, Button } from '@mui/material';

interface BulkEditDialogProps {
  open: boolean;
  onClose: () => void;
  selectedTransactions: number[];
}

const BulkEditDialog: React.FC<BulkEditDialogProps> = ({ open, onClose, selectedTransactions }) => (
  <Dialog open={open} onClose={onClose}>
    <DialogTitle>Bulk Edit Transactions</DialogTitle>
    <DialogContent>
      <p>{`You have selected ${selectedTransactions.length} transactions for bulk editing.`}</p>
    </DialogContent>
    <DialogActions>
      <Button onClick={onClose} color="primary">Cancel</Button>
      <Button onClick={onClose} color="primary">Apply Changes</Button>
    </DialogActions>
  </Dialog>
);

export default BulkEditDialog;
