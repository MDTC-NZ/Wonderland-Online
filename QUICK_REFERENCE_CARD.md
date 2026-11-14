# 📌 QUICK REFERENCE CARD

## Your Configuration

```
Server IP:     122.63.87.155
Domain:        wlo.kiaoragaming.xyz
Port:          8090
Full URL:      http://wlo.kiaoragaming.xyz:8090
```

---

## Step 1: DNS Setup (5 min)

**Go to:** Your domain registrar (Godaddy, Namecheap, etc.)

**Add this A Record:**
```
Type:  A
Host:  wlo
Value: 122.63.87.155
```

---

## Step 2: Firewall (5 min)

**Windows (Run as Admin):**
```cmd
netsh advfirewall firewall add rule name="WLO Registration Server" dir=in action=allow protocol=tcp localport=8090
```

**Linux:**
```bash
sudo ufw allow 8090/tcp
```

---

## Test It

```
Local:     http://localhost:8090
Domain:    http://wlo.kiaoragaming.xyz:8090
```

---

## Timeline

1. Add DNS (5 min)
2. Open firewall (5 min)  
3. Wait for DNS (15 min - 48 hrs)
4. Test and launch!

---

## Documentation

- `DOMAIN_LINKING_CHECKLIST.md` - Quick checklist
- `DOMAIN_SETUP_GUIDE.md` - Complete guide
