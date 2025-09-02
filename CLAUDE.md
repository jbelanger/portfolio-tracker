# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Architecture

Portfolio Tracker is a full-stack application with separate backend and frontend components:

### Backend (.NET 8 Web API)
- **Clean Architecture**: Uses Domain, Application, Infrastructure, and API layers
- **Projects**:
  - `Portfolio.Api` - Web API controllers and configuration
  - `Portfolio.Application` - Business logic and MediatR handlers
  - `Portfolio.Domain` - Domain models and business rules
  - `Portfolio.Infrastructure` - Data access with Entity Framework Core
  - `Portfolio.Transactions.Importers` - CSV import functionality for various brokers
  - `Portfolio.Transactions.Exporters` - Export functionality for portfolio data

### Frontend (Two implementations)
- **portfolio-web**: React 18 with TypeScript, Material-UI, Create React App
- **web_next**: Next.js 14 with TypeScript, Material-UI, Tailwind CSS (newer implementation)

## Development Commands

### Backend (.NET)
```bash
# From /backend directory
dotnet build                    # Build all projects
dotnet test                     # Run all tests
dotnet run --project src/Portfolio.Api    # Run API server

# Entity Framework migrations
dotnet ef migrations add "MigrationName" --project src/Portfolio.Infrastructure --startup-project src/Portfolio.Api --output-dir Data/Migrations
dotnet ef database update --project src/Portfolio.Infrastructure --startup-project src/Portfolio.Api
```

### Frontend - portfolio-web (React)
```bash
# From /frontend/portfolio-web directory
npm start                       # Development server (port 3000)
npm test                        # Run tests
npm run build                   # Production build
```

### Frontend - web_next (Next.js)
```bash
# From /frontend/web_next directory
npm run dev                     # Development server with HTTPS
npm run build                   # Production build
npm start                       # Start production server
npm run lint                    # Run ESLint
```

## Key Technologies

### Backend
- .NET 8 Web API
- Entity Framework Core with SQLite
- MediatR for CQRS pattern
- FluentValidation for input validation
- Google Authentication
- JWT Bearer authentication
- CsvHelper for transaction imports
- Swagger/OpenAPI documentation

### Frontend
- **portfolio-web**: React 18, TypeScript, Material-UI v5, React Router, Axios
- **web_next**: Next.js 14, TypeScript, Material-UI v6, Tailwind CSS, NextAuth

## Database

- Uses SQLite database via Entity Framework Core
- Migrations are stored in `backend/src/Portfolio.Infrastructure/Data/Migrations`
- Database context is in Infrastructure layer following clean architecture

## Transaction Import/Export System

The application includes specialized modules for:
- **Importers**: Handle CSV imports from various brokers (Coinbase, etc.)
- **Exporters**: Generate reports and export portfolio data in different formats

Each importer/exporter is a separate project with comprehensive test coverage.

## Testing

- Backend tests use standard .NET testing framework
- Frontend tests use React Testing Library and Jest
- All projects have corresponding test projects in `/backend/tests/` directory