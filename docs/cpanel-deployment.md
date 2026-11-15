# cPanel website deployment

## Current hosting details

- Hostname: `wlo.kiaoragaming.xyz`
- Document root: `/home/kiaorag1/wlo.kiaoragaming.xyz`
- cPanel Node.js application: use Node.js 22 (or another supported version 18+)
- DNS A record observed: `31.22.4.94`
- HTTPS responds, but currently serves a directory index. The checked page and API paths return 404.
- PHP extensions `pdo_pgsql` and `curl` have not been confirmed.
- SSH/SFTP port 22 is not reachable from the deployment environment; no files have been uploaded.

## Deployment boundary

Only reviewed public website files belong in the document root. Do not upload the repository root, game-server binaries, `Src/`, `Data/`, database files, backups, logs, private keys, or credentials.

The cPanel website entry point is `Web/app.js`. It serves an allowlist of public HTML, CSS, and JavaScript files, disables directory listing by design, and returns a clear `503` for API requests until a separate account/game API is deployed. The `.htaccess` file is defense in depth if the same files are served directly by Apache.

### cPanel Node.js setup

1. In cPanel, create a Node.js application for `wlo.kiaoragaming.xyz`, choose Node.js 22 (not the old Node 10 virtual environment), and set the startup file to `app.js`.
2. Set the application root to a private folder under `/home/kiaorag1`, for example `wlo-site`. Upload `app.js`, `package.json`, and the reviewed HTML/CSS/JavaScript assets there; do not upload `test.html` or replace the `.htaccess` file created by cPanel.
3. Run `npm install` from the application root, then restart the application in cPanel.
4. Verify `/`, `/register.html`, `/dashboard.html`, and `/healthz` over HTTPS. `/api/server-status` and account API routes return `503` until an independent backend is configured.
5. Keep using cPanel’s HTTPS for the public domain. Do not forward the home server’s port 8080 from the router to make the cPanel domain work.

For private-LAN access, the Windows game-server website binds to all network interfaces on port 8080 and requires a valid TLS certificate at `bin\Release\Certificates\wlo.kiaoragaming.xyz.pfx`. It will not start the registration website if that certificate is missing, expired, or lacks its private key; it never falls back to HTTP. This PC currently has `192.168.1.9`, but its Wi-Fi network is marked **Public**. From an elevated PowerShell window on the server PC, add a LAN-only firewall exception:

```powershell
New-NetFirewallRule -DisplayName "Wonderland Website LAN Port 8080" -Direction Inbound -Protocol TCP -LocalPort 8080 -RemoteAddress LocalSubnet -Profile Public -Action Allow
```

Configure local DNS on the router so `wlo.kiaoragaming.xyz` resolves to the server's LAN address, then visit `https://wlo.kiaoragaming.xyz:8080/`. Do not use the raw IP unless the certificate includes that IP as a subject alternative name. Reserve the LAN address in the router. Do not create a router port-forward for 8080; the public domain remains on cPanel.

Password changes require a signed-in session and the current password. Lost-account recovery must be handled by a server administrator; the public username-only reset endpoint has been removed.

The empty Supabase schema has been applied from migrations `20261115092440_wlo_core_schema` and `20261115092607_secure_wlo_schema_and_review_pk`. Its account, character, inventory, stats, and mall tables have RLS enabled; public and signed-in client roles have no table grants or policies, while the server-only `service_role` has access. The no-policy advisor notice is intentional for these server-only tables; do not add permissive client policies. See the [RLS advisor guidance](https://supabase.com/docs/guides/database/database-linter?lint=0008_rls_enabled_no_policy). No account or game-user records were copied. The existing public `rls_auto_enable()` event function also had client RPC execution revoked while preserving its enabled event trigger.

## Important backend limitation

The registration, login, player dashboard, and GM/Admin APIs currently run inside the Windows game-server application on port 8080. They work only when that application is running on the LAN. They are not implemented as cPanel endpoints, so public pages can remain available on cPanel while those account and management actions still return an unavailable response.

The active game database layer supports SQLite and MySQL, not PostgreSQL. Supabase is PostgreSQL. Before enabling web account writes, the web API and game server must be migrated to a compatible shared database and tested together. Do not create a second website-only account store; it would not authenticate in the game server. Do not put a Supabase service key or database password in browser JavaScript or the public document root.

## Safe publication checklist

1. Keep the current hosting backup and confirm the cPanel document root.
2. For Node hosting, upload the reviewed app bundle and preserve cPanel’s generated `.htaccess`. For static Apache hosting instead, upload only reviewed public assets and the `Web/.htaccess`; exclude `test.html`, `app.js`, `package.json`, and `package-lock.json`.
3. Confirm `index.html` loads over HTTPS and directory listing is disabled.
4. Confirm CSS and JavaScript requests return successfully.
5. Keep registration and GM/Admin actions disabled until an independent authenticated API and shared database are deployed.
6. After backend deployment, test registration, login, password changes, account ownership checks, admin authorization, and game-server-offline behavior before announcing the site.