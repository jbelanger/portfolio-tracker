// src/mui.d.ts
import { ButtonPropsVariantOverrides } from '@mui/material/Button';

declare module '@mui/material/Button' {
  interface ButtonPropsVariantOverrides {
    ghost: true;
    flat: true;
  }
}
