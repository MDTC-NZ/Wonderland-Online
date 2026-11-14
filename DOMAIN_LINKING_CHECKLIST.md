# 🚀 Domain Linking - ACTION CHECKLIST

## Your Setup
- **Server IP:** 122.63.87.155
- **Domain:** wlo.kiaoragaming.xyz
- **Port:** 8090
- **Target URL:** http://wlo.kiaoragaming.xyz:8090

---

## ✅ IMMEDIATE ACTIONS REQUIRED

### ACTION 1: Configure DNS Record

Go to your domain registrar (Godaddy, Namecheap, etc.)

**Steps:**
1. Log in to your domain account
2. Find DNS settings for kiaoragaming.xyz
3. Add or Edit DNS Record:

```
Type:     A Record
Host:     wlo
Value:    122.63.87.155
TTL:      3600 (or default)
Save
```

**After saving, DNS will propagate in 15 minutes to 48 hours**

---

### ACTION 2: Configure Firewall

**Windows (Run as Administrator):**

Open Command Prompt and run:
```
netsh advfirewall firewall add rule name="WLO Registration Server" dir=in action=allow protocol=tcp localport=8090
```

**Linux:**
```bash
sudo ufw allow 8090/tcp
```

**Mac:**
Use System Preferences > Security & Privacy > Firewall

---

## 🔍 VERIFY CONFIGURATION

### Test 1: Check Server Running Locally
```
Open browser: http://localhost:8090
```
✓ Should show registration page

### Test 2: Check Firewall
```
netstat -ano | findstr :8090
```
✓ Should show LISTENING on port 8090

### Test 3: Check DNS (After propagation)
```
nslookup wlo.kiaoragaming.xyz
```
✓ Should return: 122.63.87.155

### Test 4: Access from Browser (After DNS/Firewall)
```
Open browser: http://wlo.kiaoragaming.xyz:8090
```
✓ Should show registration page

---

## 📋 Timeline

| Step | Action | Status | Time |
|------|--------|--------|------|
| 1 | Add DNS A record | TO DO | 5 min |
| 2 | Open firewall port | TO DO | 5 min |
| 3 | Wait for DNS propagation | TO DO | 15 min - 48 hrs |
| 4 | Verify DNS resolution | TO DO | 5 min |
| 5 | Test external access | TO DO | 5 min |

---

## 🎯 Once Complete

Players can register at:
```
http://wlo.kiaoragaming.xyz:8090
```

Game server:
```
wlo.kiaoragaming.xyz:7777
```

---

## 📞 Your Configuration Reference

**Copy and paste these values:**

```
Server IP:        122.63.87.155
Domain:           wlo.kiaoragaming.xyz
Port:             8090
Registration URL: http://wlo.kiaoragaming.xyz:8090
```

**DNS Record to Add:**
```
Type:  A
Host:  wlo
Value: 122.63.87.155
TTL:   3600
```

**Firewall Rule (Windows):**
```
netsh advfirewall firewall add rule name="WLO Registration Server" dir=in action=allow protocol=tcp localport=8090
```

---

## 📖 Detailed Guide

See: **DOMAIN_SETUP_GUIDE.md** for complete instructions, troubleshooting, and verification steps.

---

**Status:** ✅ Server Ready | ⏳ Awaiting DNS & Firewall Configuration
