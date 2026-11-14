# 🌐 Domain Configuration Guide for wlo.kiaoragaming.xyz

## 📍 Your Server Details

| Property | Value |
|----------|-------|
| **Server IP** | 122.63.87.155 |
| **Domain** | wlo.kiaoragaming.xyz |
| **Port** | 8090 |
| **Full URL** | http://wlo.kiaoragaming.xyz:8090 |

---

## ✅ Current Status

✅ Registration server running on localhost:8090
✅ Server configured for external access (IPAddress.Any)
✅ All Web files in place
✅ API endpoints ready
✅ Database configuration set

---

## 🔧 Configuration Steps Required

### Step 1: DNS Configuration ⚠️ **YOU NEED TO DO THIS**

**If you have a domain registrar (like Godaddy, Namecheap, etc.):**

1. Log in to your domain registrar
2. Find the DNS settings for kiaoragaming.xyz
3. Add or update an A record:

```
Type:     A Record
Host:     wlo
Points to: 122.63.87.155
TTL:      3600 (or default)
```

**Result:** wlo.kiaoragaming.xyz → 122.63.87.155

**Alternative setup (if you want without subdomain):**
```
Type:     A Record
Host:     @ (or blank)
Points to: 122.63.87.155
TTL:      3600
```

**Note:** DNS changes can take 15 minutes to 48 hours to propagate globally.

---

### Step 2: Firewall Configuration ⚠️ **YOU NEED TO CHECK THIS**

**On Windows Firewall (if your server is Windows):**

1. Open Command Prompt (Admin)
2. Run this command:

```cmd
netsh advfirewall firewall add rule name="WLO Registration Server" dir=in action=allow protocol=tcp localport=8090
```

**On Linux (if your server is Linux):**

```bash
# Using UFW
sudo ufw allow 8090/tcp

# Or using firewall-cmd
sudo firewall-cmd --permanent --add-port=8090/tcp
sudo firewall-cmd --reload
```

**On macOS:**

```bash
# Use System Preferences > Security & Privacy > Firewall Options
# Or use terminal:
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add /path/to/app
```

---

### Step 3: Server Configuration ✅ **ALREADY DONE**

Your registration server is already configured correctly:

✅ **RegistrationServer.cs**
```csharp
// Binds to all network interfaces (not just localhost)
listener = new TcpListener(IPAddress.Any, Port);
```

✅ **Port Binding**
```
Port 8090: Listening on 0.0.0.0:8090 (all interfaces)
```

✅ **External Access**
```
Server accepts connections from:
- localhost:8090 (local testing)
- localhost:8080 (local testing)
- 127.0.0.1:8090 (local testing)
- 122.63.87.155:8090 (remote access)
- 122.63.87.155:8080 (remote access)
- wlo.kiaoragaming.xyz:8090 (after DNS configured)
- wlo.kiaoragaming.xyz:8080 (after DNS configured)
```

---

### Step 4: Verify Configuration

#### Test 1: Check Server is Listening (Local)
```bash
netstat -ano | findstr :8090
```

Expected output shows listening on port 8090

#### Test 2: Test External Access (After DNS/Firewall Set)
```bash
# From your local machine:
curl -I http://wlo.kiaoragaming.xyz:8090

# Or in browser:
http://wlo.kiaoragaming.xyz:8090
```

Expected response: HTTP 200 OK with HTML content

#### Test 3: Check DNS Propagation
```bash
# Check if DNS is resolving
nslookup wlo.kiaoragaming.xyz
# Should show: 122.63.87.155

# Or use:
ping wlo.kiaoragaming.xyz
# Should show IP: 122.63.87.155
```

---

## 📊 Network Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│  Players / Browsers                                        │
│           │                                                │
│           │ DNS lookup: wlo.kiaoragaming.xyz              │
│           │                                                │
│           ├─────────────────────────────────────────────┐  │
│           │     resolves to 122.63.87.155              │  │
│           │                                             │  │
│           ▼                                             │  │
│  ┌─────────────────────────────────┐                   │  │
│  │  Your Server (122.63.87.155)    │                   │  │
│  │                                  │                   │  │
│  │  Port 8090: Registration Server │◄─────────────────┘  │
│  │  Port 7777: Game Server         │                      │
│  │  Port 5423: Database (internal) │                      │
│  │                                  │                      │
│  └─────────────────────────────────┘                      │
│                                                             │
│  Access URLs:                                             │
│  • http://wlo.kiaoragaming.xyz:8090  (Registration)      │
│  • wlo.kiaoragaming.xyz:7777          (Game Server)      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔐 Ports Overview

| Port | Service | Access | Protocol |
|------|---------|--------|----------|
| 8090 | Registration Server | External (HTTP) | HTTP |
| 7777 | Game Server | External | Custom |
| 5423 | Database (PostgreSQL) | Internal Only | PostgreSQL |

---

## ✨ After Configuration

Once you've completed steps 1 & 2, your registration page will be accessible:

### Local Testing (Right Now)
```
http://localhost:8090
http://127.0.0.1:8090
```

### External Testing (After DNS/Firewall)
```
http://122.63.87.155:8090
http://wlo.kiaoragaming.xyz:8090
```

### Players Can Register At
```
http://wlo.kiaoragaming.xyz:8090
```

---

## 🧪 Testing Checklist

### Before Making DNS Changes
- [ ] Local server accessible at http://localhost:8090
- [ ] Registration form works
- [ ] Password reset works
- [ ] API endpoints respond

### After Firewall Configuration
- [ ] Port 8090 is open (check with netstat)
- [ ] Can connect from another machine on local network
- [ ] No firewall errors in logs

### After DNS Configuration
- [ ] DNS resolves: `nslookup wlo.kiaoragaming.xyz` returns 122.63.87.155
- [ ] Can ping: `ping wlo.kiaoragaming.xyz`
- [ ] Can access: http://wlo.kiaoragaming.xyz:8090 in browser
- [ ] Registration page loads completely
- [ ] Forms work correctly
- [ ] API endpoints respond

---

## 🚀 Full Deployment Checklist

### Phase 1: Local Setup ✅
- [x] Registration page created
- [x] Backend API implemented
- [x] Test server running
- [x] Local testing works

### Phase 2: Server Configuration ⏳ (YOUR ACTION NEEDED)
- [ ] DNS A record created (host: wlo, value: 122.63.87.155)
- [ ] Firewall rule added for port 8090
- [ ] DNS propagation verified

### Phase 3: External Testing ⏳
- [ ] Access http://wlo.kiaoragaming.xyz:8090
- [ ] Test registration form
- [ ] Test password reset
- [ ] Verify all features work

### Phase 4: Production Deployment ⏳
- [ ] Build tools complete
- [ ] Project compiled
- [ ] Executable deployed to server
- [ ] Database verified
- [ ] Full system tested

### Phase 5: Launch 🎉
- [ ] Announce to players
- [ ] Monitor for issues
- [ ] Support registrations

---

## 🆘 Troubleshooting

### Problem: Can't Access from Outside Network

**Check 1: Firewall Rule**
```bash
# Windows
netstat -ano | findstr :8090
# Should show LISTENING

# Check Windows Firewall
netsh advfirewall firewall show rule name="WLO Registration Server"
```

**Check 2: DNS Resolution**
```bash
nslookup wlo.kiaoragaming.xyz
# Should return 122.63.87.155

ping wlo.kiaoragaming.xyz
# Should respond from 122.63.87.155
```

**Check 3: Server Binding**
```bash
# Check if server is listening on all interfaces
netstat -ano | findstr "LISTENING" | findstr "8090"
# Should show 0.0.0.0:8090 or similar
```

### Problem: DNS Not Resolving

**Solutions:**
1. Wait 15-48 hours for DNS propagation
2. Flush DNS cache:
   ```bash
   ipconfig /flushdns  (Windows)
   sudo dscacheutil -flushcache  (Mac)
   sudo systemctl restart systemd-resolved  (Linux)
   ```
3. Try different DNS server (8.8.8.8):
   ```bash
   nslookup wlo.kiaoragaming.xyz 8.8.8.8
   ```

### Problem: Connection Timeout

**Possible Causes:**
1. Firewall blocking port 8090
2. Server not running
3. DNS not yet propagated
4. Server IP incorrect in DNS record

**Solutions:**
1. Add firewall rule
2. Start the server
3. Wait for DNS propagation
4. Verify DNS A record points to 122.63.87.155

---

## 📚 Related Documentation

- **REGISTRATION_SERVER_LIVE.md** - Current status and testing
- **QUICK_START.md** - Deployment guide
- **DOMAIN_CONFIGURATION.md** - Original network setup doc
- **REGISTRATION_PAGE_GUIDE.md** - API endpoints

---

## 🎯 Next Steps

1. **Immediately:** Complete DNS and Firewall configuration
2. **Wait:** For DNS propagation (15 min - 48 hours)
3. **Verify:** Test external access at http://wlo.kiaoragaming.xyz:8090
4. **Monitor:** Watch server logs for any issues
5. **Announce:** Tell players about registration

---

## 📞 Quick Reference

**Your Setup:**
```
Domain:    wlo.kiaoragaming.xyz
Server IP: 122.63.87.155
Port:      8090
URL:       http://wlo.kiaoragaming.xyz:8090
```

**DNS Record:**
```
Type:  A
Host:  wlo
Value: 122.63.87.155
```

**Firewall (Windows):**
```
netsh advfirewall firewall add rule name="WLO Registration Server" 
  dir=in action=allow protocol=tcp localport=8090
```

**Test:**
```
http://wlo.kiaoragaming.xyz:8090
```

---

**Document Updated:** 2026-11-15
**Server IP:** 122.63.87.155
**Status:** ✅ Server Ready | ⏳ DNS/Firewall Configuration Required
