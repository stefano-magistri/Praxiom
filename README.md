# Dottoressa Iorio Medical Management System

**Developer portfolio:** [Stefano Magistri — .NET Modernisation](https://stefano-magistri-dotnet.topjaguar.chatgpt.site/)

A comprehensive medical management system built with Blazor WebAssembly, designed for individual medical practitioners to efficiently manage patient records, therapies, and documentation.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Technical Stack](#technical-stack)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Database Management](#database-management)
- [Security](#security)
- [Testing](#testing)
- [Deployment](#deployment)
- [Contributing](#contributing)
- [License](#license)

## Overview

The Dottoressa Iorio Medical Management System is a secure, efficient, and user-friendly application designed to help medical practitioners manage their practice. The system provides comprehensive tools for patient management, therapy tracking, and document generation, all while maintaining strict data privacy and security standards.

## Features

### Patient Management
- Complete patient profiles with personal and contact information
- Patient search and filtering capabilities
- Patient history tracking
- Secure patient data storage

### Therapy Management
- Therapy template creation and management
- Patient-specific therapy tracking
- Therapy history and progress monitoring
- PDF report generation for therapies

### Security Features
- Windows Authentication integration
- Role-based access control
- Encrypted SQLite database
- Secure file handling

### Document Management
- PDF generation for patient records and therapies
- Document storage and retrieval
- Template-based document creation

## Technical Stack

- **Frontend**: Blazor WebAssembly
- **Backend**: ASP.NET Core
- **Database**: SQLite with encryption
- **Authentication**: Windows Authentication (Negotiate)
- **PDF Generation**: DinkToPdf
- **Testing**: xUnit, Moq
- **Logging**: Serilog

## Getting Started

### Prerequisites
- Windows 10 or later
- .NET 8.0 SDK
- Visual Studio 2022 (recommended)
- SQLite

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/DottoressaIorio.git
   ```

2. Open the solution in Visual Studio

3. Restore NuGet packages

4. Configure the application (see Configuration section)

5. Run the application:
   ```bash
   dotnet run --project DottoressaIorio.App
   ```

## Configuration

### Authentication
The application supports two authentication modes:
1. Windows Authentication (Negotiate)
2. Development mode (no authentication)

Configure in `appsettings.json`:
```json
{
  "Authentication": {
    "UseNegotiate": true,
    "AllowedUsername": "DOMAIN\\username"
  }
}
```

### Database
The SQLite database is automatically encrypted. Configure the encryption key in `appsettings.json`:
```json
{
  "EncryptionKey": "your-encryption-key"
}
```

## Database Management

### Initial Setup
The application automatically:
- Creates the encrypted database if it doesn't exist
- Applies migrations
- Sets up initial schema

### Backup
Regular database backups are recommended. Use the built-in backup functionality or implement your own backup strategy.

## Security

### Authentication
- Windows Authentication integration
- Configurable allowed users
- Secure session management

### Data Protection
- Encrypted SQLite database
- Secure file storage
- Input validation and sanitization

## Testing

The application includes comprehensive test coverage:
- Unit tests for repositories
- Integration tests for services
- Authentication and authorization tests

Run tests:
```bash
dotnet test
```

## Deployment

### Windows Service Deployment
1. Build the application:
   ```bash
   dotnet publish -c Release
   ```

2. Use the provided installer scripts in the `setup/installer` directory:
   - `archive.bat` - Creates deployment package
   - `install.bat` - Installs Windows service

### Configuration
- Update `appsettings.Production.json` with production settings
- Configure Windows Authentication
- Set up database encryption key

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the Dottoressa Alessandra Iorio License - see the LICENSE.md file for details.
