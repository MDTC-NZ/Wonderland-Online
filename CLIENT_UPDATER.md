# Self-hosted game client updates

The self-hosted client updater is separate from the legacy updater built into the game client. `START_CLIENT.bat` checks the self-hosted manifest and verifies each downloaded file's size and SHA-256 hash before starting `Main.exe`. It does **not** disable or replace the client's legacy updater. The installed `web0.DAT` and `ISP0.DAT` still point at retired IGG update hosts, and the legacy updater's response protocol has not been implemented by this server.

The repository includes an empty manifest and **does not contain or package the game client or other game assets**. The server distributes only files an administrator explicitly places in a patch source folder.

The public registration page shows live game-server status, the running game-server count, and online characters. It also exposes the update launcher and latest manifest. To make a full game-client download available, set `clientDownloadUrl` in `Web\config.js` to a trusted HTTPS location for a client package you are authorized to distribute; until then the page explicitly indicates that no client download is configured.

## Publishing a patch

1. Put only files you are authorized to distribute in a patch source folder.
2. Build the server and updater in Release.
3. Publish those files into the server's `bin\Release\ClientUpdates` folder:

   ```powershell
   python .\publish_client_updates.py --source C:\path\to\authorized-patch --output .\bin\Release\ClientUpdates --version 1.0.1
   ```

The manifest is generated from the copied files. Existing unreferenced files in the server's `ClientUpdates\files` folder are not served through the manifest.

## Server and security requirements

The server exposes:

- `GET /client-updates/manifest.json`
- `GET /client-updates/files/{relative-path}`

The existing server listens on localhost when its configured certificate is absent. For internet distribution, install a trusted certificate with its private key at `Certificates\wlo.kiaoragaming.xyz.pfx`, make port 8080 reachable, rebuild/deploy the server, and confirm the certificate hostname matches the manifest URL. The updater refuses non-HTTPS update URLs except for loopback testing and uses normal Windows certificate validation. Do not disable certificate checks.

## Launching

`START_CLIENT.bat` defaults to the local manifest at `http://127.0.0.1:8080/client-updates/manifest.json` and installs to `C:\Program Files (x86)\Wonderland Online`. This works when the game client and server are on the same PC. For remote clients, change `MANIFEST_URL` to the server's trusted HTTPS manifest URL after installing the matching TLS certificate; `127.0.0.1` on a remote PC points to that PC, not the game server. If an update touches files under Program Files, run the launcher with permission to modify that folder. Close `Main.exe` and `aLogin.exe` before applying patches.

With the included empty manifest, the launcher downloads no files and starts `Main.exe`; it does not make the game client fully updated. Publish an authorized patch package before expecting actual client files to change. If the legacy `WL Online Update` dialog appears, the JSON updater cannot satisfy it; do not treat a successful manifest check as proof that the client is updated.
