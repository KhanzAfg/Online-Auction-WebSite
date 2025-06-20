# Online Auction System

An ASP.NET MVC-based online auction system that allows users to buy and sell items through an auction process.

## Features

- User registration and authentication
- Item listing with images and documents
- Bidding system with minimum bid and increment settings
- Category management
- Search functionality
- Admin panel for user and category management
- Rating system for buyers and sellers

## Prerequisites

- .NET 7.0 SDK
- SQL Server (LocalDB or full version)
- Visual Studio 2022 or later

## Getting Started

1. Clone the repository
2. Open the solution in Visual Studio
3. Update the connection string in `appsettings.json` if needed
4. Run the following commands in Package Manager Console:
   ```
   Add-Migration InitialCreate
   Update-Database
   ```
5. Run the application

## Project Structure

- `Models/` - Contains the data models
- `Views/` - Contains the Razor views
- `Controllers/` - Contains the controller classes
- `Data/` - Contains the database context and configurations
- `wwwroot/` - Contains static files (CSS, JavaScript, images)

## User Roles

1. **Administrator**
   - Manage users (block/unblock)
   - Create and merge categories
   - Monitor system activity

2. **Normal User**
   - Register and login
   - List items for sale
   - Place bids on items
   - View item details and bid history
   - Rate other users

## Database Schema

The system uses the following main tables:
- Users
- Items
- Categories
- Bids

## Security Features

- Password hashing
- User authentication
- Role-based authorization
- Input validation
- XSS protection
- CSRF protection

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License. 