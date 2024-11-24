import { createTheme } from '@mui/material/styles';

const vsCodeTheme = createTheme({
  palette: {
    mode: 'dark',
    primary: {
      main: '#007ACC', // VS Code's blue accent color
    },
    secondary: {
      main: '#23D18B', // Green accent color
    },
    background: {
      default: '#1E1E1E', // Main background
      paper: '#252526', // Panel background
    },
    text: {
      primary: '#D4D4D4', // Main text color
      secondary: '#AAAAAA', // Secondary text color
    },
    divider: '#3C3C3C', // Divider color similar to VS Code
  },
  typography: {
    fontFamily: '"Fira Code", "Consolas", "Monaco", "Courier New", monospace',
    fontSize: 14,
    button: {
      textTransform: 'none', // Maintain VS Code's case for buttons
    },
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: '4px',
          backgroundColor: '#333333',
          color: '#D4D4D4',
          '&:hover': {
            backgroundColor: '#3C3C3C',
          },
          '&:active': {
            backgroundColor: '#007ACC',
            color: '#FFFFFF',
          },
        },
      },
    },
    MuiTextField: {
      styleOverrides: {
        root: {
          backgroundColor: '#252526',
          borderRadius: '4px',
          '& .MuiInputBase-root': {
            color: '#D4D4D4',
          },
          '& .MuiOutlinedInput-notchedOutline': {
            borderColor: '#3C3C3C',
          },
          '&:hover .MuiOutlinedInput-notchedOutline': {
            borderColor: '#007ACC',
          },
          '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
            borderColor: '#007ACC',
          },
        },
      },
    },
    MuiTooltip: {
      styleOverrides: {
        tooltip: {
          backgroundColor: '#333333',
          color: '#D4D4D4',
          fontSize: '12px',
        },
      },
    },
    MuiTabs: {
      styleOverrides: {
        root: {
          backgroundColor: '#1E1E1E',
          color: '#D4D4D4',
        },
        indicator: {
          backgroundColor: '#007ACC',
        },
      },
    },
    MuiTab: {
      styleOverrides: {
        root: {
          color: '#D4D4D4',
          '&.Mui-selected': {
            color: '#FFFFFF',
          },
        },
      },
    },
  },
});

export default vsCodeTheme;
