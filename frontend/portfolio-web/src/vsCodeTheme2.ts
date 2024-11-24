import { createTheme } from '@mui/material/styles';

const vsCodeTheme = createTheme({
  palette: {
    mode: 'dark',
    primary: {
      main: '#007ACC',
    },
    secondary: {
      main: '#23D18B',
    },
    background: {
      default: '#1E1E1E',
      paper: 'linear-gradient(135deg, #252526, #2C2C2C)',
    },
    text: {
      primary: '#D4D4D4',
      secondary: '#AAAAAA',
    },
    divider: '#444444',
  },
  typography: {
    fontFamily: '"Fira Code", "Consolas", "Monaco", "Courier New", monospace',
    fontSize: 14,
    h1: {
      color: '#23D18B',
      fontSize: '2rem',
    },
    h2: {
      color: '#D4D4D4',
      fontSize: '1.5rem',
    },
    body1: {
      color: '#D4D4D4',
    },
    body2: {
      color: '#AAAAAA',
    },
    button: {
      textTransform: 'none',
      fontWeight: 'bold',
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
            backgroundColor: 'linear-gradient(135deg, #333333, #3C3C3C)',
            boxShadow: '0 4px 8px rgba(0, 0, 0, 0.2)',
          },
          '&:active': {
            backgroundColor: '#007ACC',
            color: '#FFFFFF',
          },
        },
        contained: {
          backgroundColor: '#007ACC',
          color: '#FFFFFF',
          '&:hover': {
            backgroundColor: '#005FAF',
          },
        },
        outlined: {
          borderColor: '#007ACC',
          '&:hover': {
            borderColor: '#005FAF',
            backgroundColor: 'rgba(0, 122, 204, 0.1)',
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
            boxShadow: '0 2px 4px rgba(0, 0, 0, 0.1)',
          },
          '& .MuiOutlinedInput-notchedOutline': {
            borderColor: '#3C3C3C',
          },
          '&:hover .MuiOutlinedInput-notchedOutline': {
            borderColor: '#007ACC',
          },
          '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
            borderColor: '#23D18B',
            boxShadow: '0 0 5px #23D18B',
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
          borderRadius: '4px',
          boxShadow: '0 2px 4px rgba(0, 0, 0, 0.2)',
        },
      },
    },
    MuiTabs: {
      styleOverrides: {
        root: {
          backgroundColor: '#1E1E1E',
          color: '#D4D4D4',
          '& .MuiTab-root:hover': {
            color: '#FFFFFF',
            backgroundColor: '#252526',
          },
        },
        indicator: {
          backgroundColor: '#23D18B',
        },
      },
    },
    MuiTab: {
      styleOverrides: {
        root: {
          color: '#D4D4D4',
          '&.Mui-selected': {
            color: '#23D18B',
          },
        },
      },
    },
    MuiIconButton: {
      styleOverrides: {
        root: {
          color: '#D4D4D4',
          '&:hover': {
            color: '#007ACC',
            backgroundColor: 'rgba(0, 122, 204, 0.1)',
          },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          backgroundColor: '#252526',
          borderRadius: '8px',
          boxShadow: '0 4px 8px rgba(0, 0, 0, 0.3)',
          padding: '16px',
          '& .MuiCardHeader-root': {
            backgroundColor: '#333333',
            color: '#D4D4D4',
          },
        },
      },
    },
    MuiDialog: {
      styleOverrides: {
        paper: {
          backgroundColor: '#1E1E1E',
          borderRadius: '8px',
          padding: '20px',
          boxShadow: '0 8px 16px rgba(0, 0, 0, 0.4)',
        },
      },
    },
  },
});

export default vsCodeTheme;
