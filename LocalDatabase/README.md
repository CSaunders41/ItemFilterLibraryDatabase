# ItemFilterLibrary Local Database System

This is a self-hosted replacement for the external ItemFilterLibrary database system. It provides a local API server with SQLite database storage, allowing you to manage templates independently without relying on external services.

## 🏗️ Architecture

- **Database**: SQLite database for local storage
- **API Server**: ASP.NET Core Web API (port 5000)
- **Authentication**: Simplified local authentication with JWT tokens
- **Plugin Integration**: Compatible with existing ItemFilterLibraryDatabase plugin

## 📋 Prerequisites

- .NET 8.0 SDK
- Path of Exile with ExileCore installed
- ItemFilterLibraryDatabase plugin
- ItemFilterLibInspector plugin (for item identification)

## 🚀 Setup Instructions

### 1. Build and Run the API Server

**⚠️ Important:** The LocalDatabase folder contains a separate ASP.NET Core API server project. This is NOT compiled with the plugin - it runs as a standalone application.

```bash
cd LocalDatabase/ItemFilterLibraryAPI
dotnet restore
dotnet build
dotnet run
```

The API server will start on `http://localhost:5000`

### 2. Verify Server is Running

Open your browser and go to `http://localhost:5000` - you should see:
```json
{
  "message": "ItemFilterLibrary Local API Server",
  "version": "1.0.0",
  "endpoints": {
    "login": "/auth/discord/login",
    "test_auth": "/auth/test",
    "template_types": "/templates/types/list",
    "swagger": "/swagger"
  }
}
```

### 3. Configure the Plugin

The plugin has already been updated to use `http://localhost:5000` as the default host URL. If you need to change this:

1. Open ExileCore
2. Go to Plugin Settings → ItemFilterLibraryDatabase
3. Update the `HostUrl` setting to point to your local server

### 4. Authenticate

1. In ExileCore, go to Plugin Settings → ItemFilterLibraryDatabase
2. Click "Open Login Page" - this will open `http://localhost:5000/auth/discord/login`
3. Enter a username (any name you want)
4. Click "Login"
5. Copy the generated auth token
6. Paste it into the "Auth Token" field in the plugin
7. Click "Use Auth Token"

## 📊 Default Data

The system comes with:
- **Default Admin User**: `admin` (created automatically)
- **Template Types**:
  - `itemfilterlibrary` - Item Filter Library
  - `wheresmycraftat` - Where's My Craft At
  - `reagent` - ReAgent

## 🔧 Configuration

### Database Configuration

The database file (`itemfilterlibrary.db`) is created automatically in the API server directory. You can modify the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=itemfilterlibrary.db"
  }
}
```

### JWT Configuration

For production use, update the JWT secret key in `appsettings.json`:

```json
{
  "JWT": {
    "SecretKey": "your-super-secret-key-change-in-production-make-it-at-least-32-characters-long"
  }
}
```

### Port Configuration

To change the server port, update `appsettings.json`:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  }
}
```

## 📡 API Endpoints

### Authentication
- `GET /auth/discord/login` - Login page
- `POST /auth/login` - Login with username/password
- `POST /auth/refresh` - Refresh access token
- `GET /auth/test` - Test authentication

### Template Types
- `GET /templates/types/list` - Get all template types

### Templates
- `GET /templates/{typeId}/templates` - Get public templates (paginated)
- `GET /templates/{typeId}/my` - Get user's templates
- `GET /templates/{typeId}/Template/{templateId}` - Get specific template
- `POST /templates/{typeId}/create` - Create new template
- `PUT /templates/{typeId}/Template/{templateId}` - Update template
- `PATCH /templates/{typeId}/Template/{templateId}/visibility` - Toggle public/private
- `DELETE /templates/{typeId}/Template/{templateId}` - Soft delete template
- `DELETE /templates/{typeId}/Template/{templateId}/hard` - Hard delete template

## 🔌 Plugin Integration

The plugin automatically connects to the local API server. Features include:

- **Template Management**: Create, edit, delete templates
- **Version Control**: Automatic template versioning
- **Visibility Control**: Toggle templates between public/private
- **Search & Filter**: Find templates by name
- **Template Types**: Support for multiple template categories

## 🛠️ Integration with ItemFilterLibInspector

The ItemFilterLibInspector plugin provides item identification capabilities for IFL format. You can use it to:

- **Monitor Items**: Track items across inventory, stash, cursor, etc.
- **Analyze Item Data**: Get detailed item information for filter creation
- **Generate Templates**: Create templates based on actual item data

### Using ItemFilterLibInspector Data

1. Run ItemFilterLibInspector to collect item data
2. Use the snapshot feature to capture specific items
3. Analyze item properties to create effective filters
4. Save templates to your local database

## 🔍 Troubleshooting

### Server Won't Start
- Check that .NET 8.0 SDK is installed
- Verify port 5000 is available
- Check the console output for specific error messages

### Plugin Connection Issues
- Ensure the API server is running
- Verify the HostUrl in plugin settings matches your server
- Check that authentication was successful

### Database Issues
- The SQLite database is created automatically
- Check file permissions in the API server directory
- Database file: `itemfilterlibrary.db`

### Authentication Problems
- Try generating a new auth token
- Check that the JWT secret key is consistent
- Verify the token hasn't expired

## 📚 Advanced Usage

### Backup and Restore

**Backup**: Copy the `itemfilterlibrary.db` file to a safe location

**Restore**: Replace the current database file with your backup

### Multiple Users

The system supports multiple users. Each user:
- Has their own templates
- Can make templates public/private
- Has independent authentication

### API Development

You can extend the API by:
- Adding new endpoints in the Controllers
- Modifying database schema in `Database.sql`
- Adding new services in the Services folder

## 🤝 Contributing

This local database system is designed to be:
- **Self-contained**: No external dependencies
- **Extensible**: Easy to add new features
- **Compatible**: Works with existing plugins

## 📄 License

This project maintains compatibility with the original ItemFilterLibraryDatabase plugin structure and API endpoints.

---

## 🎯 Next Steps

1. **Start the API server**: `dotnet run` in the API directory
2. **Configure the plugin**: Update HostUrl to `http://localhost:5000`
3. **Authenticate**: Use the login page to get an auth token
4. **Create templates**: Start managing your item filter templates locally
5. **Integrate with ItemFilterLibInspector**: Use item data to create better filters

Your local ItemFilterLibrary database is now ready for use! 