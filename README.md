# Portfolio Tracker

A full-stack application for tracking cryptocurrency portfolios with transaction import/export capabilities and historical price analysis.

## Architecture

Portfolio Tracker uses clean architecture with separate backend and frontend components:

### Backend (.NET 8 Web API)
- **Clean Architecture**: Domain, Application, Infrastructure, and API layers
- **Entity Framework Core** with SQLite database
- **JWT Authentication** with Google OAuth integration
- **MediatR** for CQRS pattern implementation
- **Transaction Import/Export** system for various brokers

### Frontend (Two implementations)
- **portfolio-web**: React 18 with TypeScript and Material-UI
- **web_next**: Next.js 14 with TypeScript, Material-UI v6, and Tailwind CSS

## Getting Started

### Prerequisites
- .NET 8 SDK
- Node.js 18+ 
- Google OAuth credentials (for authentication)

### Environment Setup

1. **Copy the environment template:**
   ```bash
   cp .env.example .env
   ```

2. **Configure Google OAuth:**
   - Go to the [Google Cloud Console](https://console.cloud.google.com/)
   - Create a new project or select an existing one
   - Enable the Google+ API
   - Create OAuth 2.0 credentials
   - Add your domain to authorized origins
   - Update `.env` with your credentials:
     ```
     GOOGLE_CLIENT_ID=your_actual_google_client_id
     GOOGLE_CLIENT_SECRET=your_actual_google_client_secret
     ```

3. **Generate JWT Secret:**
   ```bash
   # Generate a secure random key for JWT signing
   openssl rand -base64 32
   ```
   Update `.env` with the generated key:
   ```
   JWT_KEY=your_generated_jwt_key
   JWT_ISSUER=https://yourdomain.com
   JWT_AUDIENCE=https://yourdomain.com
   ```

4. **Generate NextAuth Secret:**
   ```bash
   # Generate a secure random secret for NextAuth
   openssl rand -base64 32
   ```
   Update `.env`:
   ```
   NEXTAUTH_SECRET=your_generated_nextauth_secret
   ```

### Backend Setup

```bash
# Navigate to backend directory
cd backend

# Restore dependencies
dotnet restore

# Run database migrations
dotnet ef database update --project src/Portfolio.Infrastructure --startup-project src/Portfolio.Api

# Start the API server
dotnet run --project src/Portfolio.Api
```

The API will be available at `https://localhost:5262`

### Frontend Setup

#### Option 1: Next.js Frontend (Recommended)
```bash
# Navigate to Next.js frontend
cd frontend/web_next

# Install dependencies
npm install

# Start development server
npm run dev
```

#### Option 2: React Frontend
```bash
# Navigate to React frontend
cd frontend/portfolio-web

# Install dependencies
npm install

# Start development server
npm start
```

## Development Commands

### Backend
```bash
# Build all projects
dotnet build

# Run tests
dotnet test

# Add new migration
dotnet ef migrations add "MigrationName" --project src/Portfolio.Infrastructure --startup-project src/Portfolio.Api --output-dir Data/Migrations
```

### Frontend
```bash
# Next.js commands
npm run dev          # Development server with HTTPS
npm run build        # Production build
npm run lint         # Run ESLint

# React commands  
npm start            # Development server
npm test             # Run tests
npm run build        # Production build
```

## Features

- **Portfolio Management**: Track multiple cryptocurrency portfolios and wallets
- **Transaction Import**: Support for CSV imports from various brokers (Coinbase, Kraken, KuCoin, etc.)
- **Historical Price Data**: Integration with CoinGecko API for price history
- **Cost Basis Calculation**: FIFO and LIFO cost basis strategies
- **Export Capabilities**: Generate reports and export data in various formats
- **Google Authentication**: Secure user authentication via Google OAuth
- **Clean Architecture**: Maintainable codebase following SOLID principles

## Database

The application uses SQLite by default for local development. The database file will be created automatically when you run the migrations.

## Security

- Environment variables are used for all sensitive configuration
- JWT tokens for API authentication
- Google OAuth for user authentication
- No secrets are committed to version control

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests to ensure everything works
5. Submit a pull request

## License

This project is open source and available under the [MIT License](LICENSE).