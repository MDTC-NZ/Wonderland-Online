# Domain Configuration for wlo.kiaoragaming.xyz

## Changes Made

### 1. Registration Server (RegistrationServer.cs)
- **Port**: 8090 (HTTP)
- **Binding**: All interfaces (IPAddress.Any) - allows external connections
- **URL**: http://wlo.kiaoragaming.xyz:8090
- Registration page is now accessible from any IP address, not just localhost

### 2. Database Configuration (MainForm1.cs)
- **Domain**: wlo.kiaoragaming.xyz
- **Type**: PostgreSQL
- **Port**: 5423
- **Database**: kiaorag1_wlo
- **User**: kiaorag1_wlo
- **Connection String**: `Server=wlo.kiaoragaming.xyz;Port=5423;Database=kiaorag1_wlo;User Id=kiaorag1_wlo;Password=20Manukau26;`

### 3. Network Access

#### DNS Configuration Required:
```
wlo.kiaoragaming.xyz  A  <your-server-ip>
```

#### Firewall Configuration Required:
- **Port 8090/TCP**: Registration Server (HTTP)
- **Port 5432/TCP**: PostgreSQL Database (if exposed)
- **Port 7777/TCP**: Game Server (default)

#### For External Users:

1. **Registration Page**: 
   - Navigate to: `http://wlo.kiaoragaming.xyz:8090`
   - Users can create new accounts

2. **Database Connection**:
   - Connected to `wlo.kiaoragaming.xyz:5423`
   - Supports remote player connections

### 4. Features Now Enabled:

✅ **Remote Registration**: Other users can register accounts via the web interface
✅ **Domain-Based Connection**: Uses `wlo.kiaoragaming.xyz` instead of hardcoded IPs
✅ **Database Linked to Domain**: PostgreSQL connection configured for the domain
✅ **Multi-User Support**: Server accepts connections from all IPs

## Setup Steps:

1. **Update DNS Records**:
   - Point `wlo.kiaoragaming.xyz` to your server's public IP address

2. **Configure Network/Firewall**:
   - Open port 8090 for HTTP registration server
   - Open port 5432 for database (or use SSH tunnel for security)
   - Open port 7777 for game server

3. **Build and Deploy**:
   - Build the project: `.\build.ps1 -Configuration Release`
   - Run the server application
   - Verify registration page is accessible at `http://wlo.kiaoragaming.xyz:8090`

4. **Test Registration**:
   - Have other users visit the registration page
   - Create test accounts
   - Verify database records appear in PostgreSQL

## Security Recommendations:

1. **Database Access**:
   - Consider using SSH tunnel for PostgreSQL access
   - Implement firewall rules to restrict database access
   - Use strong passwords (already configured)

2. **Registration Server**:
   - Consider adding HTTPS support for production
   - Implement rate limiting to prevent abuse
   - Add CAPTCHA for bot prevention

3. **Network Security**:
   - Use a VPN or private network for sensitive operations
   - Monitor logs for unauthorized access attempts
   - Keep firewall rules minimal (whitelist approach)

## Environment Configuration:

The configuration can be modified in the server GUI:
- Database tab: Update server connection settings
- Network tab: Configure ports and bindings
- Settings are saved to: `%APPDATA%\PServer\Config.settings.wlo`

## Troubleshooting:

**Registration page not accessible:**
- Verify DNS points to correct IP
- Check firewall allows port 8090
- Verify server is running: `Server.RegistrationServer started`

**Database connection failed:**
- Verify PostgreSQL is running on wlo.kiaoragaming.xyz:5423
- Check credentials: user=kiaorag1_wlo
- Verify network connectivity: `ping wlo.kiaoragaming.xyz`

**Users can't create accounts:**
- Check server logs for error messages
- Verify database table exists: `user`
- Ensure registration server is running
