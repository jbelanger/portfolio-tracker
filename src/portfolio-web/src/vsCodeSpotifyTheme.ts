import { createTheme } from '@mui/material/styles';

const vsCodeSpotifyTheme = createTheme({
  palette: {
    mode: 'dark',
    primary: {
      main: '#007ACC', // VS Code blue
    },
    secondary: {
      main: '#23D18B', // Green accent
    },
    background: {
      default: '#1E1E1E', // Dark grey background
      paper: 'linear-gradient(135deg, #1E1E1E, #2C2C2C)', // Slight gradient for panels
    },
    text: {
      primary: '#D4D4D4', // Primary text color
      secondary: '#B0B0B0', // Darker grey for secondary text
    },
    divider: '#2C2C2C', // Dark divider
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
      color: '#B0B0B0', // Use darker grey instead of light grey
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
          backgroundColor: '#2C2C2C', // Dark grey for buttons
          color: '#D4D4D4',
          '&:hover': {
            backgroundColor: 'linear-gradient(135deg, #2C2C2C, #3C3C3C)',
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
          backgroundColor: '#1E1E1E', // Dark grey background for inputs
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
          backgroundColor: '#000000', // Black background for tooltips
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
          backgroundColor: '#121212', // Black background for tabs
          color: '#D4D4D4',
          '& .MuiTab-root:hover': {
            color: '#FFFFFF',
            backgroundColor: '#2C2C2C',
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
            backgroundColor: '#121212', // Black background for selected tabs
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
          backgroundColor: '#2C2C2C', // Darker grey for cards
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
          backgroundColor: '#1E1E1E', // Dark grey for dialogs
          borderRadius: '8px',
          padding: '20px',
          boxShadow: '0 8px 16px rgba(0, 0, 0, 0.4)',
        },
      },
    },
  },
});

export default vsCodeSpotifyTheme;
