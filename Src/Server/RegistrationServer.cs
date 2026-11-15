using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using DataBase;
using Game;
using Game.Maps;

namespace Server
{
    public sealed class RegistrationServer
    {
        const int Port = 8080;
        const int MaximumRequestBodyLength = 4096;
        const long MaximumClientUpdateFileLength = 536870912;
        const int MaximumClientUpdateManifestLength = 1048576;
        const long MaximumUpdaterDownloadLength = 67108864;

        readonly UserDataBase userDataBase;
        readonly CharacterDataBase characterDataBase;
        readonly string clientUpdatesDirectory;
        readonly X509Certificate2 serverCertificate;
        readonly TcpListener listener;
        readonly byte[] homePage;
        readonly byte[] homeStylesheet;
        readonly byte[] page;
        readonly byte[] stylesheet;
        readonly byte[] script;
        readonly byte[] dashboardPage;
        readonly byte[] dashboardStylesheet;
        readonly byte[] dashboardScript;
        readonly byte[] configScript;
        readonly ConcurrentDictionary<string, WebSession> sessions = new ConcurrentDictionary<string, WebSession>();
        Thread listenerThread;
        volatile bool running;

        sealed class WebSession
        {
            public uint UserID;
            public string Username;
            public DateTime ExpiresAt;
        }

        public RegistrationServer(UserDataBase userDataBase, CharacterDataBase characterDataBase, string webDirectory, string certPath = null, string certPassword = null, string clientUpdatesDirectory = null)
        {
            if (userDataBase == null)
                throw new ArgumentNullException("userDataBase");
            if (characterDataBase == null)
                throw new ArgumentNullException("characterDataBase");
            if (webDirectory == null)
                throw new ArgumentNullException("webDirectory");

            this.userDataBase = userDataBase;
            this.characterDataBase = characterDataBase;
            this.clientUpdatesDirectory = Path.GetFullPath(clientUpdatesDirectory ??
                Path.Combine(Path.GetDirectoryName(Path.GetFullPath(webDirectory)), "ClientUpdates"));

            if (string.IsNullOrWhiteSpace(certPath) || !File.Exists(certPath))
                throw new FileNotFoundException("A TLS server certificate is required; the registration server will not start without it.", certPath);

            serverCertificate = new X509Certificate2(certPath, certPassword);
            if (!serverCertificate.HasPrivateKey)
                throw new InvalidOperationException("The web server certificate does not include its private key.");
            DateTime certificateTime = DateTime.Now;
            if (certificateTime < serverCertificate.NotBefore || certificateTime > serverCertificate.NotAfter)
                throw new InvalidOperationException("The web server certificate is not currently valid.");
            
            homePage = File.ReadAllBytes(Path.Combine(webDirectory, "index.html"));
            homeStylesheet = File.ReadAllBytes(Path.Combine(webDirectory, "home.css"));
            page = File.ReadAllBytes(Path.Combine(webDirectory, "register.html"));
            stylesheet = File.ReadAllBytes(Path.Combine(webDirectory, "register.css"));
            script = File.ReadAllBytes(Path.Combine(webDirectory, "register.js"));
            dashboardPage = File.ReadAllBytes(Path.Combine(webDirectory, "dashboard.html"));
            dashboardStylesheet = File.ReadAllBytes(Path.Combine(webDirectory, "dashboard.css"));
            dashboardScript = File.ReadAllBytes(Path.Combine(webDirectory, "dashboard.js"));
            configScript = File.ReadAllBytes(Path.Combine(webDirectory, "config.js"));
            listener = new TcpListener(IPAddress.Any, Port);
        }

        public void Start()
        {
            listener.Start();
            running = true;
            listenerThread = new Thread(Listen);
            listenerThread.IsBackground = true;
            listenerThread.Name = "Registration HTTPS server";
            listenerThread.Start();
        }

        public void Stop()
        {
            if (!running)
                return;

            running = false;
            listener.Stop();
            if (listenerThread != null && listenerThread != Thread.CurrentThread)
                listenerThread.Join(1000);
        }

        void Listen()
        {
            while (running)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(delegate(object state)
                    {
                        HandleClient((TcpClient)state);
                    }, client);
                }
                catch (SocketException ex)
                {
                    if (running)
                        DebugSystem.Write(new ExceptionData(ex));
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        void HandleClient(TcpClient client)
        {
            using (client)
            {
                try
                {
                    client.ReceiveTimeout = 5000;
                    client.SendTimeout = 5000;

                    using (Stream stream = CreateRequestStream(client))
                    {
                        string requestLine = ReadLine(stream, 2048);
                        if (string.IsNullOrEmpty(requestLine))
                            return;

                        string[] requestParts = requestLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (requestParts.Length != 3 ||
                            (requestParts[2] != "HTTP/1.0" && requestParts[2] != "HTTP/1.1"))
                        {
                            WriteResponse(stream, 400, "Bad request.", "text/plain; charset=utf-8");
                            return;
                        }

                        Dictionary<string, string> headers = ReadHeaders(stream);
                        int contentLength = GetContentLength(headers);
                        if (contentLength < 0 || contentLength > MaximumRequestBodyLength)
                        {
                            WriteResponse(stream, 413, "Request too large.", "text/plain; charset=utf-8");
                            return;
                        }
                        byte[] body = ReadBody(stream, contentLength);

                        string path = requestParts[1].Split('?')[0];
                        if (requestParts[0] == "GET" && contentLength == 0)
                        {
                            if (path == "/api/server-status")
                                ServePublicServerStatus(stream);
                            else if (path == "/client-updates/manifest.json")
                                ServeClientUpdateManifest(stream);
                            else if (path.StartsWith("/client-updates/files/", StringComparison.Ordinal))
                                ServeClientUpdateFile(stream, path.Substring("/client-updates/files/".Length));
                            else if (path == "/downloads/WloClientUpdater.exe")
                                ServeClientUpdaterDownload(stream);
                            else if (path == "/" || path == "/index.html")
                                WriteResponse(stream, 200, homePage, "text/html; charset=utf-8");
                            else if (path == "/home.css")
                                WriteResponse(stream, 200, homeStylesheet, "text/css; charset=utf-8");
                            else if (path == "/register.html")
                                WriteResponse(stream, 200, page, "text/html; charset=utf-8");
                            else if (path == "/register.css")
                                WriteResponse(stream, 200, stylesheet, "text/css; charset=utf-8");
                            else if (path == "/register.js")
                                WriteResponse(stream, 200, script, "application/javascript; charset=utf-8");
                            else if (path == "/dashboard" || path == "/dashboard.html")
                                WriteResponse(stream, 200, dashboardPage, "text/html; charset=utf-8");
                            else if (path == "/dashboard.css")
                                WriteResponse(stream, 200, dashboardStylesheet, "text/css; charset=utf-8");
                            else if (path == "/dashboard.js")
                                WriteResponse(stream, 200, dashboardScript, "application/javascript; charset=utf-8");
                            else if (path == "/config.js")
                                WriteResponse(stream, 200, configScript, "application/javascript; charset=utf-8");
                            else
                                WriteResponse(stream, 404, "Not found.", "text/plain; charset=utf-8");
                            return;
                        }

                        if (requestParts[0] != "POST" ||
                            (path != "/api/register" &&
                             path != "/api/login" &&
                             path != "/api/characters" &&
                             path != "/api/characters/create" &&
                             path != "/api/characters/delete" &&
                             path != "/api/user-stats" &&
                             path != "/api/mall/characters" &&
                             path != "/api/mall/catalog" &&
                             path != "/api/mall/purchase" &&
                             path != "/api/mall/reviews" &&
                             path != "/api/mall/submit-review" &&
                             path != "/api/mall/newsletter" &&
                             path != "/api/change-password" &&
                             path != "/api/admin/status" &&
                             path != "/api/admin/accounts" &&
                             path != "/api/admin/account/create" &&
                             path != "/api/admin/account/password" &&
                             path != "/api/admin/account/role" &&
                             path != "/api/admin/account/delete" &&
                             path != "/api/admin/characters" &&
                             path != "/api/admin/character/create" &&
                             path != "/api/admin/character/update" &&
                             path != "/api/admin/character/delete" &&
                             path != "/api/admin/online-players" &&
                             path != "/api/admin/game-players" &&
                             path != "/api/admin/game-teleport" &&
                             path != "/api/admin/game-give-item"))
                        {
                            WriteResponse(stream, 404, "Not found.", "text/plain; charset=utf-8");
                            return;
                        }

                        if (!HasSameOrigin(headers))
                        {
                            WriteResponse(stream, 403, "{\"ok\":false,\"message\":\"Request origin was rejected.\"}", "application/json; charset=utf-8");
                            return;
                        }

                        string contentType;
                        if (!headers.TryGetValue("content-type", out contentType) ||
                            !contentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
                        {
                            WriteResponse(stream, 415, "{\"ok\":false,\"message\":\"Unsupported request format.\"}", "application/json; charset=utf-8");
                            return;
                        }

                        Dictionary<string, string> form;
                        if (!TryParseForm(Encoding.UTF8.GetString(body), out form))
                        {
                            WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Invalid form data.\"}", "application/json; charset=utf-8");
                            return;
                        }

                        // Handle Login
                        if (path == "/api/login")
                        {
                            string username;
                            string password;
                            if (!form.TryGetValue("username", out username) ||
                                !form.TryGetValue("password", out password))
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Username and password are required.\"}", "application/json; charset=utf-8");
                                return;
                            }

                            uint userID;
                            string[] userData;
                            if (userDataBase.GetUserData(username, password, out userID, out userData))
                            {
                                string token = CreateSessionToken();
                                sessions[token] = new WebSession
                                {
                                    UserID = userID,
                                    Username = userData[0],
                                    ExpiresAt = DateTime.UtcNow.AddHours(12)
                                };
                                WriteResponse(stream, 200, "{\"ok\":true,\"token\":\"" + EscapeJson(token) + "\",\"message\":\"Login successful.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            else
                            {
                                WriteResponse(stream, 401, "{\"ok\":false,\"message\":\"Invalid username or password.\"}", "application/json; charset=utf-8");
                                return;
                            }
                        }

                        if (path == "/api/register")
                        {
                            string username;
                            string password;
                            string confirmation;
                            if (!form.TryGetValue("username", out username) ||
                                !form.TryGetValue("password", out password) ||
                                (form.TryGetValue("passwordConfirm", out confirmation) && password != confirmation))
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Check the fields and make sure both passwords match.\"}", "application/json; charset=utf-8");
                                return;
                            }

                            try
                            {
                                if (!userDataBase.CreateAccount(username, password))
                                {
                                    WriteResponse(stream, 409, "{\"ok\":false,\"message\":\"That username is already taken.\"}", "application/json; charset=utf-8");
                                    return;
                                }
                            }
                            catch (ArgumentException ex)
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"" + EscapeJson(ex.Message) + "\"}", "application/json; charset=utf-8");
                                return;
                            }

                            WriteResponse(stream, 201, "{\"ok\":true,\"message\":\"Account created successfully. The server may still need setup before gameplay is available.\"}", "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/mall/newsletter")
                        {
                            string email;
                            if (!form.TryGetValue("email", out email) || email.Length > 254)
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Enter a valid email address.\"}", "application/json; charset=utf-8");
                                return;
                            }

                            try
                            {
                                email = new MailAddress(email.Trim()).Address.ToLowerInvariant();
                                userDataBase.SubscribeMallNewsletter(email);
                            }
                            catch (FormatException)
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Enter a valid email address.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            catch (ArgumentException)
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Enter a valid email address.\"}", "application/json; charset=utf-8");
                                return;
                            }

                            WriteResponse(stream, 200, "{\"ok\":true,\"message\":\"You are subscribed to Mall updates.\"}", "application/json; charset=utf-8");
                            return;
                        }

                        WebSession session;
                        if (!TryGetSession(form, out session))
                        {
                            WriteResponse(stream, 401, "{\"ok\":false,\"message\":\"Your session has expired. Please log in again.\"}", "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/admin/status")
                        {
                            bool isAdmin = userDataBase.GetLocalGMLevel(session.UserID) > 0;
                            WriteResponse(stream, 200, "{\"ok\":true,\"isAdmin\":" + (isAdmin ? "true" : "false") + "}", "application/json; charset=utf-8");
                            return;
                        }

                        if (path.StartsWith("/api/admin/", StringComparison.Ordinal) &&
                            userDataBase.GetLocalGMLevel(session.UserID) <= 0)
                        {
                            WriteResponse(stream, 403, "{\"ok\":false,\"message\":\"GM/Admin access is required.\"}", "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/admin/accounts")
                        {
                            WriteResponse(stream, 200, BuildAdminAccountsJson(userDataBase.GetLocalAccounts()), "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/admin/account/create")
                        {
                            string username;
                            string password;
                            string gmText;
                            bool makeAdmin = form.TryGetValue("isAdmin", out gmText) &&
                                string.Equals(gmText, "true", StringComparison.OrdinalIgnoreCase);
                            if (!form.TryGetValue("username", out username) ||
                                !form.TryGetValue("password", out password))
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Username and password are required.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            try
                            {
                                if (!userDataBase.CreateAccount(username, password, makeAdmin ? 1 : 0))
                                {
                                    WriteResponse(stream, 409, "{\"ok\":false,\"message\":\"That username is already in use.\"}", "application/json; charset=utf-8");
                                    return;
                                }
                            }
                            catch (ArgumentException ex)
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"" + EscapeJson(ex.Message) + "\"}", "application/json; charset=utf-8");
                                return;
                            }
                            WriteResponse(stream, 201, BuildAdminAccountsJson(userDataBase.GetLocalAccounts()), "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/admin/account/password")
                        {
                            uint accountId;
                            string accountIdText;
                            string password;
                            if (!form.TryGetValue("accountId", out accountIdText) ||
                                !uint.TryParse(accountIdText, NumberStyles.None, CultureInfo.InvariantCulture, out accountId) ||
                                !form.TryGetValue("password", out password))
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Choose an account and enter a password.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            try
                            {
                                if (!userDataBase.ResetLocalPassword(accountId, password))
                                {
                                    WriteResponse(stream, 404, "{\"ok\":false,\"message\":\"That account was not found.\"}", "application/json; charset=utf-8");
                                    return;
                                }
                            }
                            catch (ArgumentException ex)
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"" + EscapeJson(ex.Message) + "\"}", "application/json; charset=utf-8");
                                return;
                            }
                            WriteResponse(stream, 200, "{\"ok\":true,\"message\":\"Password reset successfully.\"}", "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/admin/account/role")
                        {
                            uint accountId;
                            int gmLevel;
                            string accountIdText;
                            string gmLevelText;
                            if (!form.TryGetValue("accountId", out accountIdText) ||
                                !uint.TryParse(accountIdText, NumberStyles.None, CultureInfo.InvariantCulture, out accountId) ||
                                !form.TryGetValue("gmLevel", out gmLevelText) ||
                                !int.TryParse(gmLevelText, NumberStyles.None, CultureInfo.InvariantCulture, out gmLevel) ||
                                gmLevel < 0 || gmLevel > 1)
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Choose a valid account role.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            if (accountId == session.UserID && gmLevel == 0)
                            {
                                WriteResponse(stream, 409, "{\"ok\":false,\"message\":\"You cannot remove your own GM/Admin access.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            if (!userDataBase.SetLocalGMLevel(accountId, gmLevel))
                            {
                                WriteResponse(stream, 404, "{\"ok\":false,\"message\":\"That account was not found.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            WriteResponse(stream, 200, BuildAdminAccountsJson(userDataBase.GetLocalAccounts()), "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/admin/account/delete")
                        {
                            uint accountId;
                            string accountIdText;
                            if (!form.TryGetValue("accountId", out accountIdText) ||
                                !uint.TryParse(accountIdText, NumberStyles.None, CultureInfo.InvariantCulture, out accountId))
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Choose a valid account.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            if (accountId == session.UserID)
                            {
                                WriteResponse(stream, 409, "{\"ok\":false,\"message\":\"You cannot delete the account you are using.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            if (characterDataBase.HasWebCharacters(accountId))
                            {
                                WriteResponse(stream, 409, "{\"ok\":false,\"message\":\"Delete this account's characters before deleting the account.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            if (!userDataBase.DeleteLocalAccount(accountId))
                            {
                                WriteResponse(stream, 404, "{\"ok\":false,\"message\":\"That account was not found.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            InvalidateUserSessions(accountId);
                            WriteResponse(stream, 200, BuildAdminAccountsJson(userDataBase.GetLocalAccounts()), "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/admin/characters" || path == "/api/admin/online-players")
                        {
                            List<WebCharacterInfo> characters = GetAdminCharacters();
                            if (path == "/api/admin/online-players")
                                characters = characters.Where(character => character.IsOnline).ToList();
                            WriteResponse(stream, 200, BuildAdminCharactersJson(characters), "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/admin/game-players")
                        {
                            if (cGlobal.gWorld == null)
                            {
                                WriteResponse(stream, 503, "{\"ok\":false,\"message\":\"The game world is not available.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            WriteResponse(stream, 200, BuildLiveGamePlayersJson(), "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/admin/game-teleport")
                        {
                            string characterIDText;
                            string mapIDText;
                            string xText;
                            string yText;
                            uint characterID;
                            ushort mapID;
                            ushort x;
                            ushort y;
                            if (!TryFormValue(form, "characterId", out characterIDText) ||
                                !uint.TryParse(characterIDText, NumberStyles.None, CultureInfo.InvariantCulture, out characterID) ||
                                !TryFormValue(form, "mapId", out mapIDText) ||
                                !ushort.TryParse(mapIDText, NumberStyles.None, CultureInfo.InvariantCulture, out mapID) ||
                                !TryFormValue(form, "x", out xText) ||
                                !ushort.TryParse(xText, NumberStyles.None, CultureInfo.InvariantCulture, out x) ||
                                !TryFormValue(form, "y", out yText) ||
                                !ushort.TryParse(yText, NumberStyles.None, CultureInfo.InvariantCulture, out y))
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Enter a valid character, loaded map, and coordinates.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            if (cGlobal.gWorld == null || !cGlobal.gWorld.TeleportPlayer(characterID, mapID, x, y))
                            {
                                WriteResponse(stream, 409, "{\"ok\":false,\"message\":\"The player is offline, in a battle, or the destination map is unavailable.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            DebugSystem.Write("[AdminAudit] " + session.Username + " teleported character " +
                                characterID.ToString(CultureInfo.InvariantCulture) + " to map " +
                                mapID.ToString(CultureInfo.InvariantCulture) + " (" +
                                x.ToString(CultureInfo.InvariantCulture) + "," +
                                y.ToString(CultureInfo.InvariantCulture) + ").");
                            WriteResponse(stream, 200, "{\"ok\":true,\"message\":\"Player teleported.\"}", "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/admin/game-give-item")
                        {
                            string characterIDText;
                            string itemIDText;
                            string amountText;
                            uint characterID;
                            ushort itemID;
                            byte amount;
                            if (!TryFormValue(form, "characterId", out characterIDText) ||
                                !uint.TryParse(characterIDText, NumberStyles.None, CultureInfo.InvariantCulture, out characterID) ||
                                !TryFormValue(form, "itemId", out itemIDText) ||
                                !ushort.TryParse(itemIDText, NumberStyles.None, CultureInfo.InvariantCulture, out itemID) ||
                                !TryFormValue(form, "amount", out amountText) ||
                                !byte.TryParse(amountText, NumberStyles.None, CultureInfo.InvariantCulture, out amount) ||
                                amount == 0 || amount > 99)
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Enter a valid character, item ID, and amount from 1 to 99.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            if (cGlobal.gWorld == null)
                            {
                                WriteResponse(stream, 503, "{\"ok\":false,\"message\":\"The game world is not available.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            int delivered = cGlobal.gWorld.GiveItemToPlayer(characterID, itemID, amount);
                            if (delivered <= 0)
                            {
                                WriteResponse(stream, 409, "{\"ok\":false,\"message\":\"No items were delivered. Check that the player is online, the item ID exists, and the inventory has space.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            DebugSystem.Write("[AdminAudit] " + session.Username + " gave " +
                                delivered.ToString(CultureInfo.InvariantCulture) + " of item " +
                                itemID.ToString(CultureInfo.InvariantCulture) + " to character " +
                                characterID.ToString(CultureInfo.InvariantCulture) + ".");
                            string message = delivered == amount ? "Items delivered." :
                                "Only " + delivered.ToString(CultureInfo.InvariantCulture) + " of " +
                                amount.ToString(CultureInfo.InvariantCulture) + " items could be delivered.";
                            WriteResponse(stream, 200, "{\"ok\":true,\"delivered\":" +
                                delivered.ToString(CultureInfo.InvariantCulture) + ",\"message\":\"" +
                                EscapeJson(message) + "\"}", "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/admin/character/create")
                        {
                            uint accountId;
                            byte slot;
                            byte bodyValue;
                            byte head;
                            byte element;
                            string accountIdText;
                            string slotText;
                            string bodyText;
                            string headText;
                            string elementText;
                            string name;
                            string username = null;
                            if (!form.TryGetValue("accountId", out accountIdText) ||
                                !uint.TryParse(accountIdText, NumberStyles.None, CultureInfo.InvariantCulture, out accountId) ||
                                !form.TryGetValue("slot", out slotText) ||
                                !byte.TryParse(slotText, NumberStyles.None, CultureInfo.InvariantCulture, out slot) ||
                                !form.TryGetValue("body", out bodyText) ||
                                !byte.TryParse(bodyText, NumberStyles.None, CultureInfo.InvariantCulture, out bodyValue) ||
                                !form.TryGetValue("head", out headText) ||
                                !byte.TryParse(headText, NumberStyles.None, CultureInfo.InvariantCulture, out head) ||
                                !form.TryGetValue("element", out elementText) ||
                                !byte.TryParse(elementText, NumberStyles.None, CultureInfo.InvariantCulture, out element) ||
                                !form.TryGetValue("name", out name))
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Complete all character fields with valid values.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            DataTable accounts = userDataBase.GetLocalAccounts();
                            foreach (DataRow account in accounts.Rows)
                            {
                                if (Convert.ToUInt32(account["userID"], CultureInfo.InvariantCulture) == accountId)
                                {
                                    username = account["username"].ToString();
                                    break;
                                }
                            }
                            if (username == null)
                            {
                                WriteResponse(stream, 404, "{\"ok\":false,\"message\":\"That account was not found.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            if (characterDataBase.GetWebCharacters(accountId).Any(character => character.Slot == slot))
                            {
                                WriteResponse(stream, 409, "{\"ok\":false,\"message\":\"That character slot is already occupied.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            try
                            {
                                characterDataBase.CreateWebCharacter(accountId, username, slot, name,
                                    (BodyStyle)bodyValue, head, (Affinity)element);
                            }
                            catch (ArgumentException ex)
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"" + EscapeJson(ex.Message) + "\"}", "application/json; charset=utf-8");
                                return;
                            }
                            catch (InvalidOperationException ex)
                            {
                                WriteResponse(stream, 409, "{\"ok\":false,\"message\":\"" + EscapeJson(ex.Message) + "\"}", "application/json; charset=utf-8");
                                return;
                            }
                            WriteResponse(stream, 201, BuildAdminCharactersJson(GetAdminCharacters()), "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/admin/character/update")
                        {
                            uint charId;
                            uint gold;
                            long experience;
                            int hp;
                            int sp;
                            int strength;
                            int constitution;
                            int intelligence;
                            int wisdom;
                            int agility;
                            ushort map;
                            ushort x;
                            ushort y;
                            string value;
                            if (!TryFormValue(form, "characterId", out value) || !uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out charId) ||
                                !TryFormValue(form, "experience", out value) || !long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out experience) ||
                                !TryFormValue(form, "hp", out value) || !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out hp) ||
                                !TryFormValue(form, "sp", out value) || !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out sp) ||
                                !TryFormValue(form, "strength", out value) || !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out strength) ||
                                !TryFormValue(form, "constitution", out value) || !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out constitution) ||
                                !TryFormValue(form, "intelligence", out value) || !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out intelligence) ||
                                !TryFormValue(form, "wisdom", out value) || !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out wisdom) ||
                                !TryFormValue(form, "agility", out value) || !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out agility) ||
                                !TryFormValue(form, "gold", out value) || !uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out gold) ||
                                !TryFormValue(form, "map", out value) || !ushort.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out map) ||
                                !TryFormValue(form, "x", out value) || !ushort.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out x) ||
                                !TryFormValue(form, "y", out value) || !ushort.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out y))
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Enter valid character values.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            try
                            {
                                if (!characterDataBase.UpdateWebCharacter(charId, experience, hp, sp,
                                    strength, constitution, intelligence, wisdom, agility, gold, map, x, y))
                                {
                                    WriteResponse(stream, 404, "{\"ok\":false,\"message\":\"That character was not found.\"}", "application/json; charset=utf-8");
                                    return;
                                }
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"" + EscapeJson(ex.Message) + "\"}", "application/json; charset=utf-8");
                                return;
                            }
                            catch (InvalidOperationException ex)
                            {
                                WriteResponse(stream, 409, "{\"ok\":false,\"message\":\"" + EscapeJson(ex.Message) + "\"}", "application/json; charset=utf-8");
                                return;
                            }
                            WriteResponse(stream, 200, BuildAdminCharactersJson(GetAdminCharacters()), "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/admin/character/delete")
                        {
                            uint accountId;
                            uint charId;
                            string accountIdText;
                            string charIdText;
                            if (!form.TryGetValue("accountId", out accountIdText) ||
                                !uint.TryParse(accountIdText, NumberStyles.None, CultureInfo.InvariantCulture, out accountId) ||
                                !form.TryGetValue("characterId", out charIdText) ||
                                !uint.TryParse(charIdText, NumberStyles.None, CultureInfo.InvariantCulture, out charId))
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Choose a valid character.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            try
                            {
                                if (!characterDataBase.DeleteWebCharacter(accountId, charId))
                                {
                                    WriteResponse(stream, 404, "{\"ok\":false,\"message\":\"That character was not found on the selected account.\"}", "application/json; charset=utf-8");
                                    return;
                                }
                            }
                            catch (InvalidOperationException ex)
                            {
                                WriteResponse(stream, 409, "{\"ok\":false,\"message\":\"" + EscapeJson(ex.Message) + "\"}", "application/json; charset=utf-8");
                                return;
                            }
                            WriteResponse(stream, 200, BuildAdminCharactersJson(GetAdminCharacters()), "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/user-stats")
                        {
                            int balance = userDataBase.GetMallBalance(session.UserID);
                            WriteResponse(stream, 200, "{\"success\":true,\"stats\":{\"mall_points\":" + balance.ToString(CultureInfo.InvariantCulture) + "}}", "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/characters")
                        {
                            WriteResponse(stream, 200, BuildWebCharactersJson(characterDataBase.GetWebCharacters(session.UserID)), "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/characters/create")
                        {
                            string name;
                            string bodyText;
                            string elementText;
                            byte bodyValue;
                            byte elementValue;
                            if (!form.TryGetValue("name", out name) ||
                                !form.TryGetValue("body", out bodyText) ||
                                !byte.TryParse(bodyText, NumberStyles.None, CultureInfo.InvariantCulture, out bodyValue) ||
                                !form.TryGetValue("element", out elementText) ||
                                !byte.TryParse(elementText, NumberStyles.None, CultureInfo.InvariantCulture, out elementValue) ||
                                !Enum.IsDefined(typeof(BodyStyle), (int)bodyValue) ||
                                !Enum.IsDefined(typeof(Affinity), (int)elementValue))
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Choose a valid character appearance and element.\"}", "application/json; charset=utf-8");
                                return;
                            }

                            List<MallCharacterInfo> existing = userDataBase.GetMallCharacters(session.UserID);
                            bool slotOneOccupied = existing.Any(character => character.Slot == 1);
                            bool slotTwoOccupied = existing.Any(character => character.Slot == 2);
                            if (slotOneOccupied && slotTwoOccupied)
                            {
                                WriteResponse(stream, 409, "{\"ok\":false,\"message\":\"Both character slots are already occupied.\"}", "application/json; charset=utf-8");
                                return;
                            }

                            byte slot = slotOneOccupied ? (byte)2 : (byte)1;
                            try
                            {
                                characterDataBase.CreateWebCharacter(session.UserID, session.Username, slot, name, (BodyStyle)bodyValue, 0, (Affinity)elementValue);
                            }
                            catch (ArgumentException ex)
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"" + EscapeJson(ex.Message) + "\"}", "application/json; charset=utf-8");
                                return;
                            }
                            catch (InvalidOperationException ex)
                            {
                                WriteResponse(stream, 409, "{\"ok\":false,\"message\":\"" + EscapeJson(ex.Message) + "\"}", "application/json; charset=utf-8");
                                return;
                            }

                            WriteResponse(stream, 201, BuildWebCharactersJson(characterDataBase.GetWebCharacters(session.UserID)), "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/characters/delete")
                        {
                            string charIDText;
                            uint charID;
                            if (!form.TryGetValue("characterId", out charIDText) ||
                                !uint.TryParse(charIDText, NumberStyles.None, CultureInfo.InvariantCulture, out charID))
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Invalid character.\"}", "application/json; charset=utf-8");
                                return;
                            }

                            try
                            {
                                if (!characterDataBase.DeleteWebCharacter(session.UserID, charID))
                                {
                                    WriteResponse(stream, 404, "{\"ok\":false,\"message\":\"That character was not found on your account.\"}", "application/json; charset=utf-8");
                                    return;
                                }
                            }
                            catch (InvalidOperationException ex)
                            {
                                WriteResponse(stream, 409, "{\"ok\":false,\"message\":\"" + EscapeJson(ex.Message) + "\"}", "application/json; charset=utf-8");
                                return;
                            }

                            WriteResponse(stream, 200, BuildWebCharactersJson(characterDataBase.GetWebCharacters(session.UserID)), "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/mall/characters")
                        {
                            WriteResponse(stream, 200, BuildCharactersJson(userDataBase.GetMallCharacters(session.UserID)), "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/mall/catalog")
                        {
                            List<MallProductInfo> products = userDataBase.GetMallCatalog(global::System.cGlobal.ItemDatManager);
                            if (products.Count == 0)
                            {
                                WriteResponse(stream, 503, "{\"ok\":false,\"message\":\"The in-game item catalog is not ready.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            WriteResponse(stream, 200, BuildCatalogJson(products), "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/mall/purchase")
                        {
                            string charIDText;
                            string itemIDText;
                            string requestID;
                            uint charID;
                            ushort itemID;
                            if (!form.TryGetValue("characterId", out charIDText) ||
                                !uint.TryParse(charIDText, NumberStyles.None, CultureInfo.InvariantCulture, out charID) ||
                                !form.TryGetValue("itemId", out itemIDText) ||
                                !ushort.TryParse(itemIDText, NumberStyles.None, CultureInfo.InvariantCulture, out itemID) ||
                                !form.TryGetValue("requestId", out requestID) ||
                                requestID.Length > 64 ||
                                !IsValidRequestID(requestID))
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Invalid purchase details.\"}", "application/json; charset=utf-8");
                                return;
                            }

                            MallProductInfo product = userDataBase.GetMallCatalog(global::System.cGlobal.ItemDatManager)
                                .FirstOrDefault(candidate => candidate.ItemId == itemID);
                            if (product == null)
                            {
                                WriteResponse(stream, 404, "{\"ok\":false,\"message\":\"This item is not available for purchase.\"}", "application/json; charset=utf-8");
                                return;
                            }

                            MallPurchaseResult purchase = userDataBase.CreateMallPurchase(session.UserID, charID, product, requestID);
                            if (purchase.CharacterNotFound)
                            {
                                WriteResponse(stream, 404, "{\"ok\":false,\"message\":\"That character does not belong to your game account.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            if (purchase.InsufficientPoints)
                            {
                                WriteResponse(stream, 409, "{\"ok\":false,\"message\":\"You do not have enough Mall points.\",\"balance\":" + purchase.Balance.ToString(CultureInfo.InvariantCulture) + "}", "application/json; charset=utf-8");
                                return;
                            }

                            string orderMessage = purchase.Status == "delivered"
                                ? "This purchase was already completed."
                                : "Purchase queued. The item will be delivered when that character next logs in.";
                            WriteResponse(stream, 200,
                                "{\"ok\":true,\"orderId\":\"" + EscapeJson(purchase.OrderId) + "\",\"status\":\"" +
                                EscapeJson(purchase.Status) + "\",\"balance\":" +
                                purchase.Balance.ToString(CultureInfo.InvariantCulture) + ",\"message\":\"" +
                                EscapeJson(orderMessage) + "\"}",
                                "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/mall/reviews")
                        {
                            string itemIDText;
                            ushort itemID;
                            if (!form.TryGetValue("itemId", out itemIDText) ||
                                !ushort.TryParse(itemIDText, NumberStyles.None, CultureInfo.InvariantCulture, out itemID))
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Invalid item.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            if (!userDataBase.GetMallCatalog(global::System.cGlobal.ItemDatManager).Any(candidate => candidate.ItemId == itemID))
                            {
                                WriteResponse(stream, 404, "{\"ok\":false,\"message\":\"This item is not available.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            WriteResponse(stream, 200, BuildReviewsJson(userDataBase.GetMallReviews(itemID)), "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/mall/submit-review")
                        {
                            string itemIDText;
                            string ratingText;
                            string reviewText;
                            ushort itemID;
                            int rating;
                            if (!form.TryGetValue("itemId", out itemIDText) ||
                                !ushort.TryParse(itemIDText, NumberStyles.None, CultureInfo.InvariantCulture, out itemID) ||
                                !form.TryGetValue("rating", out ratingText) ||
                                !int.TryParse(ratingText, NumberStyles.None, CultureInfo.InvariantCulture, out rating) ||
                                !form.TryGetValue("review", out reviewText) ||
                                reviewText.Length > 500)
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Check the rating and review text (maximum 500 characters).\"}", "application/json; charset=utf-8");
                                return;
                            }
                            if (!userDataBase.GetMallCatalog(global::System.cGlobal.ItemDatManager).Any(candidate => candidate.ItemId == itemID))
                            {
                                WriteResponse(stream, 404, "{\"ok\":false,\"message\":\"This item is not available.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            if (!userDataBase.SaveMallReview(session.UserID, itemID, rating, reviewText))
                            {
                                WriteResponse(stream, 403, "{\"ok\":false,\"message\":\"You can review an item after it has been delivered to your character.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            WriteResponse(stream, 200, "{\"ok\":true,\"message\":\"Your review was saved.\"}", "application/json; charset=utf-8");
                            return;
                        }

                        if (path == "/api/change-password")
                        {
                            string currentPassword;
                            string newPassword;
                            if (!form.TryGetValue("currentPassword", out currentPassword) ||
                                !form.TryGetValue("newPassword", out newPassword) ||
                                !userDataBase.VerifyAccount(session.Username, currentPassword))
                            {
                                WriteResponse(stream, 401, "{\"ok\":false,\"message\":\"Current password is incorrect.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            try
                            {
                                if (!userDataBase.ResetPassword(session.Username, newPassword))
                                {
                                    WriteResponse(stream, 404, "{\"ok\":false,\"message\":\"Game account not found.\"}", "application/json; charset=utf-8");
                                    return;
                                }
                                InvalidateUserSessions(session.UserID);
                            }
                            catch (ArgumentException ex)
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"" + EscapeJson(ex.Message) + "\"}", "application/json; charset=utf-8");
                                return;
                            }
                            WriteResponse(stream, 200, "{\"ok\":true,\"message\":\"Password updated.\"}", "application/json; charset=utf-8");
                            return;
                        }

                    }
                }
                catch (AuthenticationException ex)
                {
                    DebugSystem.Write(new ExceptionData(ex));
                }
                catch (IOException ex)
                {
                    DebugSystem.Write(new ExceptionData(ex));
                }
                catch (Exception ex)
                {
                    DebugSystem.Write(new ExceptionData(ex));
                    try
                    {
                        using (NetworkStream stream = client.GetStream())
                            WriteResponse(stream, 500, "{\"ok\":false,\"message\":\"Registration is temporarily unavailable.\"}", "application/json; charset=utf-8");
                    }
                    catch (IOException)
                    {
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
        }

        Stream CreateRequestStream(TcpClient client)
        {
            NetworkStream networkStream = client.GetStream();
            SslStream sslStream = new SslStream(networkStream, false);
            try
            {
                sslStream.AuthenticateAsServer(serverCertificate, false, SslProtocols.Tls12, false);
                return sslStream;
            }
            catch
            {
                sslStream.Dispose();
                throw;
            }
        }

        void ServeClientUpdateManifest(Stream stream)
        {
            string manifestPath = Path.Combine(clientUpdatesDirectory, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                WriteResponse(stream, 503, "Client updates are not published.", "text/plain; charset=utf-8");
                return;
            }

            if (ContainsReparsePoint(clientUpdatesDirectory, manifestPath))
            {
                WriteResponse(stream, 404, "Client update manifest not found.", "text/plain; charset=utf-8");
                return;
            }

            FileInfo manifest = new FileInfo(manifestPath);
            if (manifest.Length > MaximumClientUpdateManifestLength)
            {
                WriteResponse(stream, 413, "Client update manifest is too large.", "text/plain; charset=utf-8");
                return;
            }

            WriteFileResponse(stream, 200, manifest, "application/json; charset=utf-8");
        }

        void ServePublicServerStatus(Stream stream)
        {
            bool gameOnline = cGlobal.gLoginServer != null && cGlobal.gLoginServer.IsListening;
            int playersOnline = cGlobal.gCharacterDataBase == null ? 0 : cGlobal.gCharacterDataBase.OnlineCharacterCount;
            string checkedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            string json = "{\"gameOnline\":" + (gameOnline ? "true" : "false") +
                ",\"serversRunning\":" + (gameOnline ? "1" : "0") +
                ",\"configuredServers\":1,\"playersOnline\":" + playersOnline.ToString(CultureInfo.InvariantCulture) +
                ",\"gamePort\":6414,\"checkedAt\":\"" + EscapeJson(checkedAt) + "\"}";
            WriteResponse(stream, 200, json, "application/json; charset=utf-8");
        }

        static void ServeClientUpdaterDownload(Stream stream)
        {
            string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WloClientUpdater.exe");
            if (!File.Exists(updaterPath))
            {
                WriteResponse(stream, 404, "Client updater is not available.", "text/plain; charset=utf-8");
                return;
            }

            FileInfo updater = new FileInfo(updaterPath);
            if (updater.Length > MaximumUpdaterDownloadLength)
            {
                WriteResponse(stream, 413, "Client updater is too large.", "text/plain; charset=utf-8");
                return;
            }
            WriteFileResponse(stream, 200, updater, "application/octet-stream");
        }

        void ServeClientUpdateFile(Stream stream, string encodedRelativePath)
        {
            string relativePath;
            if (!TryNormalizeClientUpdatePath(encodedRelativePath, out relativePath))
            {
                WriteResponse(stream, 400, "Invalid update file path.", "text/plain; charset=utf-8");
                return;
            }

            string filesDirectory = Path.GetFullPath(Path.Combine(clientUpdatesDirectory, "files"));
            string filePath = Path.GetFullPath(Path.Combine(filesDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            string filesPrefix = filesDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            if (!filePath.StartsWith(filesPrefix, StringComparison.OrdinalIgnoreCase))
            {
                WriteResponse(stream, 400, "Invalid update file path.", "text/plain; charset=utf-8");
                return;
            }

            if (ContainsReparsePoint(filesDirectory, filePath) || !File.Exists(filePath))
            {
                WriteResponse(stream, 404, "Update file not found.", "text/plain; charset=utf-8");
                return;
            }

            FileInfo file = new FileInfo(filePath);
            if (file.Length > MaximumClientUpdateFileLength)
            {
                WriteResponse(stream, 413, "Update file is too large.", "text/plain; charset=utf-8");
                return;
            }

            WriteFileResponse(stream, 200, file, "application/octet-stream");
        }

        static bool TryNormalizeClientUpdatePath(string encodedPath, out string relativePath)
        {
            relativePath = null;
            string decodedPath;
            try
            {
                decodedPath = Uri.UnescapeDataString(encodedPath);
            }
            catch (UriFormatException)
            {
                return false;
            }

            if (string.IsNullOrEmpty(decodedPath) ||
                decodedPath.Length > 240 ||
                decodedPath.IndexOf('\\') >= 0 ||
                decodedPath.IndexOf(':') >= 0 ||
                decodedPath.IndexOf('\0') >= 0 ||
                decodedPath.StartsWith("/", StringComparison.Ordinal))
                return false;

            string[] segments = decodedPath.Split('/');
            foreach (string segment in segments)
            {
                if (segment.Length == 0 || segment == "." || segment == ".." ||
                    segment.EndsWith(".", StringComparison.Ordinal) ||
                    segment.EndsWith(" ", StringComparison.Ordinal) ||
                    IsReservedWindowsFileName(segment))
                    return false;
                foreach (char character in segment)
                    if (!char.IsLetterOrDigit(character) && character != ' ' &&
                        character != '.' && character != '_' && character != '-')
                        return false;
            }

            relativePath = string.Join(Path.DirectorySeparatorChar.ToString(), segments);
            return true;
        }

        static bool ContainsReparsePoint(string root, string path)
        {
            string normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string normalizedPath = Path.GetFullPath(path);
            if (!normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(normalizedPath, normalizedRoot, StringComparison.OrdinalIgnoreCase))
                return true;

            string current = normalizedPath;
            while (!string.Equals(current.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(current) &&
                    (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    return true;
                if (Directory.Exists(current) &&
                    (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    return true;

                current = Path.GetDirectoryName(current);
                if (string.IsNullOrEmpty(current))
                    return true;
            }
            return Directory.Exists(normalizedRoot) &&
                (File.GetAttributes(normalizedRoot) & FileAttributes.ReparsePoint) != 0;
        }

        static bool IsReservedWindowsFileName(string name)
        {
            string baseName = name.Split('.')[0].TrimEnd(' ');
            if (string.Equals(baseName, "CON", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(baseName, "PRN", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(baseName, "AUX", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(baseName, "NUL", StringComparison.OrdinalIgnoreCase))
                return true;
            if (baseName.Length == 4 &&
                (baseName.StartsWith("COM", StringComparison.OrdinalIgnoreCase) ||
                 baseName.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) &&
                baseName[3] >= '1' && baseName[3] <= '9')
                return true;
            return false;
        }

        static void WriteFileResponse(Stream stream, int statusCode, FileInfo file, string contentType)
        {
            string reason = statusCode == 200 ? "OK" : "Internal Server Error";
            string headers = "HTTP/1.1 " + statusCode + " " + reason + "\r\n" +
                "Content-Type: " + contentType + "\r\n" +
                "Content-Length: " + file.Length.ToString(CultureInfo.InvariantCulture) + "\r\n" +
                "Connection: close\r\n" +
                "Cache-Control: no-store\r\n" +
                "X-Content-Type-Options: nosniff\r\n" +
                "Content-Disposition: attachment\r\n\r\n";
            byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);
            using (FileStream fileStream = file.OpenRead())
                fileStream.CopyTo(stream);
        }

        static Dictionary<string, string> ReadHeaders(Stream stream)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int totalLength = 0;
            while (true)
            {
                string line = ReadLine(stream, 8192);
                if (line == null)
                    throw new IOException("Incomplete HTTP headers.");
                totalLength += line.Length;
                if (totalLength > 16384)
                    throw new IOException("HTTP headers are too large.");
                if (line.Length == 0)
                    return headers;

                int separator = line.IndexOf(':');
                if (separator <= 0)
                    throw new IOException("Invalid HTTP header.");
                string name = line.Substring(0, separator).Trim();
                string value = line.Substring(separator + 1).Trim();
                if (headers.ContainsKey(name))
                    throw new IOException("Duplicate HTTP header.");
                headers.Add(name, value);
            }
        }

        static int GetContentLength(Dictionary<string, string> headers)
        {
            string transferEncoding;
            if (headers.TryGetValue("transfer-encoding", out transferEncoding))
                return -1;

            string value;
            if (!headers.TryGetValue("content-length", out value))
                return 0;

            int length;
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out length))
                return -1;
            return length;
        }

        static bool HasSameOrigin(Dictionary<string, string> headers)
        {
            string origin;
            string host;
            Uri originUri;
            if (!headers.TryGetValue("origin", out origin) ||
                !headers.TryGetValue("host", out host) ||
                !Uri.TryCreate(origin, UriKind.Absolute, out originUri) ||
                !string.Equals(originUri.Authority, host, StringComparison.OrdinalIgnoreCase))
                return false;

            string originHost = originUri.Host;
            bool localOrigin = string.Equals(originHost, "localhost", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(originHost, "127.0.0.1", StringComparison.OrdinalIgnoreCase);
            bool productionOrigin = string.Equals(originHost, "wlo.kiaoragaming.xyz", StringComparison.OrdinalIgnoreCase);
            bool privateLanOrigin = IsPrivateIpv4Address(originHost);
            if (productionOrigin && originUri.Scheme == Uri.UriSchemeHttps)
                return true;

            if (localOrigin || privateLanOrigin)
            {
                return originUri.Scheme == Uri.UriSchemeHttp ||
                    originUri.Scheme == Uri.UriSchemeHttps;
            }

            return false;
        }

        static bool IsPrivateIpv4Address(string host)
        {
            IPAddress address;
            if (!IPAddress.TryParse(host, out address) ||
                address.AddressFamily != AddressFamily.InterNetwork)
                return false;

            byte[] bytes = address.GetAddressBytes();
            return bytes[0] == 10 ||
                (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                (bytes[0] == 192 && bytes[1] == 168);
        }

        static byte[] ReadBody(Stream stream, int contentLength)
        {
            byte[] body = new byte[contentLength];
            int offset = 0;
            while (offset < body.Length)
            {
                int count = stream.Read(body, offset, body.Length - offset);
                if (count == 0)
                    throw new IOException("Incomplete HTTP body.");
                offset += count;
            }
            return body;
        }

        static bool TryParseForm(string body, out Dictionary<string, string> form)
        {
            form = new Dictionary<string, string>(StringComparer.Ordinal);
            string[] fields = body.Split('&');
            try
            {
                foreach (string field in fields)
                {
                    int separator = field.IndexOf('=');
                    if (separator < 0)
                        return false;
                    string name = DecodeFormValue(field.Substring(0, separator));
                    string value = DecodeFormValue(field.Substring(separator + 1));
                    if (form.ContainsKey(name))
                        return false;
                    form.Add(name, value);
                }
            }
            catch (UriFormatException)
            {
                return false;
            }
            return true;
        }

        static string DecodeFormValue(string value)
        {
            return Uri.UnescapeDataString(value.Replace("+", " "));
        }

        static string ReadLine(Stream stream, int maximumLength)
        {
            StringBuilder line = new StringBuilder();
            while (line.Length <= maximumLength)
            {
                int value = stream.ReadByte();
                if (value < 0)
                    return line.Length == 0 ? null : line.ToString();
                if (value == '\n')
                    return line.ToString();
                if (value != '\r')
                    line.Append((char)value);
            }
            throw new IOException("HTTP line is too long.");
        }

        static string EscapeJson(string value)
        {
            if (value == null)
                return "";
            StringBuilder escaped = new StringBuilder(value.Length + 8);
            foreach (char character in value)
            {
                switch (character)
                {
                    case '\\': escaped.Append("\\\\"); break;
                    case '"': escaped.Append("\\\""); break;
                    case '\b': escaped.Append("\\b"); break;
                    case '\f': escaped.Append("\\f"); break;
                    case '\n': escaped.Append("\\n"); break;
                    case '\r': escaped.Append("\\r"); break;
                    case '\t': escaped.Append("\\t"); break;
                    default:
                        if (character < 0x20)
                            escaped.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            escaped.Append(character);
                        break;
                }
            }
            return escaped.ToString();
        }

        List<WebCharacterInfo> GetAdminCharacters()
        {
            DataTable accounts = userDataBase.GetLocalAccounts();
            List<WebCharacterInfo> characters = new List<WebCharacterInfo>();
            foreach (DataRow account in accounts.Rows)
            {
                uint accountId = Convert.ToUInt32(account["userID"], CultureInfo.InvariantCulture);
                string username = account["username"].ToString();
                foreach (WebCharacterInfo character in characterDataBase.GetWebCharacters(accountId))
                {
                    character.AccountId = accountId;
                    character.AccountUsername = username;
                    characters.Add(character);
                }
            }
            return characters;
        }

        void InvalidateUserSessions(uint userID)
        {
            foreach (KeyValuePair<string, WebSession> entry in sessions)
            {
                if (entry.Value.UserID == userID)
                {
                    WebSession removed;
                    sessions.TryRemove(entry.Key, out removed);
                }
            }
        }

        static string BuildAdminAccountsJson(DataTable accounts)
        {
            StringBuilder json = new StringBuilder("{\"ok\":true,\"accounts\":[");
            for (int i = 0; i < accounts.Rows.Count; i++)
            {
                DataRow account = accounts.Rows[i];
                if (i > 0) json.Append(',');
                json.Append("{\"id\":").Append(Convert.ToUInt32(account["userID"], CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture))
                    .Append(",\"username\":\"").Append(EscapeJson(account["username"].ToString()))
                    .Append("\",\"gmLevel\":").Append(Convert.ToInt32(account["gmLevel"], CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture))
                    .Append('}');
            }
            return json.Append("]}").ToString();
        }

        static string BuildAdminCharactersJson(List<WebCharacterInfo> characters)
        {
            StringBuilder json = new StringBuilder("{\"ok\":true,\"characters\":[");
            for (int i = 0; i < characters.Count; i++)
            {
                WebCharacterInfo character = characters[i];
                if (i > 0) json.Append(',');
                json.Append("{\"id\":").Append(character.CharacterId.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"accountId\":").Append(character.AccountId.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"accountUsername\":\"").Append(EscapeJson(character.AccountUsername))
                    .Append("\",\"name\":\"").Append(EscapeJson(character.Name))
                    .Append("\",\"slot\":").Append(character.Slot.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"level\":").Append(character.Level.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"online\":").Append(character.IsOnline ? "true" : "false")
                    .Append(",\"experience\":").Append(character.Experience.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"hp\":").Append(character.CurrentHp.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"sp\":").Append(character.CurrentSp.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"strength\":").Append(character.Strength.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"constitution\":").Append(character.Constitution.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"intelligence\":").Append(character.Intelligence.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"wisdom\":").Append(character.Wisdom.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"agility\":").Append(character.Agility.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"gold\":").Append(character.Gold.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"map\":").Append(character.LocationMap.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"x\":").Append(character.LocationX.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"y\":").Append(character.LocationY.ToString(CultureInfo.InvariantCulture)).Append('}');
            }
            return json.Append("]}").ToString();
        }

        static string BuildLiveGamePlayersJson()
        {
            List<Player> players = cGlobal.gWorld.GetOnlinePlayers();
            StringBuilder json = new StringBuilder("{\"ok\":true,\"players\":[");
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                GameMap map = player.CurMap as GameMap;
                if (i > 0) json.Append(',');
                json.Append("{\"characterId\":").Append(player.CharID.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"name\":\"").Append(EscapeJson(player.CharName))
                    .Append("\",\"level\":").Append(player.Level.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"mapId\":").Append((map == null ? 0u : map.MapID).ToString(CultureInfo.InvariantCulture))
                    .Append(",\"mapName\":\"").Append(EscapeJson(map == null ? string.Empty : map.MapName))
                    .Append("\",\"x\":").Append(player.CurX.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"y\":").Append(player.CurY.ToString(CultureInfo.InvariantCulture)).Append('}');
            }
            return json.Append("]}").ToString();
        }

        static bool TryFormValue(Dictionary<string, string> form, string name, out string value)
        {
            if (!form.TryGetValue(name, out value) || string.IsNullOrWhiteSpace(value))
            {
                value = null;
                return false;
            }
            return true;
        }

        bool TryGetSession(Dictionary<string, string> form, out WebSession session)
        {
            session = null;
            string token;
            if (!form.TryGetValue("token", out token) || string.IsNullOrEmpty(token))
                return false;
            if (!sessions.TryGetValue(token, out session))
                return false;
            if (session.ExpiresAt <= DateTime.UtcNow)
            {
                WebSession removed;
                sessions.TryRemove(token, out removed);
                session = null;
                return false;
            }
            session.ExpiresAt = DateTime.UtcNow.AddHours(12);
            return true;
        }

        static string CreateSessionToken()
        {
            byte[] random = new byte[32];
            using (RNGCryptoServiceProvider generator = new RNGCryptoServiceProvider())
                generator.GetBytes(random);
            return Convert.ToBase64String(random).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        static bool IsValidRequestID(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            foreach (char character in value)
                if (!char.IsLetterOrDigit(character) && character != '-' && character != '_')
                    return false;
            return true;
        }

        static string BuildCharactersJson(List<MallCharacterInfo> characters)
        {
            StringBuilder json = new StringBuilder("{\"ok\":true,\"characters\":[");
            for (int i = 0; i < characters.Count; i++)
            {
                MallCharacterInfo character = characters[i];
                if (i > 0) json.Append(',');
                json.Append("{\"id\":").Append(character.CharacterId.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"name\":\"").Append(EscapeJson(character.Name))
                    .Append("\",\"slot\":").Append(character.Slot.ToString(CultureInfo.InvariantCulture)).Append('}');
            }
            return json.Append("]}").ToString();
        }

        static string BuildWebCharactersJson(List<WebCharacterInfo> characters)
        {
            StringBuilder json = new StringBuilder("{\"ok\":true,\"characters\":[");
            for (int i = 0; i < characters.Count; i++)
            {
                WebCharacterInfo character = characters[i];
                if (i > 0) json.Append(',');
                json.Append("{\"id\":").Append(character.CharacterId.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"name\":\"").Append(EscapeJson(character.Name))
                    .Append("\",\"slot\":").Append(character.Slot.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"body\":").Append(character.Body.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"head\":").Append(character.Head.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"element\":").Append(character.Element.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"gold\":").Append(character.Gold.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"map\":").Append(character.LocationMap.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"x\":").Append(character.LocationX.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"y\":").Append(character.LocationY.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"experience\":").Append(character.Experience.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"hp\":").Append(character.CurrentHp.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"sp\":").Append(character.CurrentSp.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"strength\":").Append(character.Strength.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"constitution\":").Append(character.Constitution.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"intelligence\":").Append(character.Intelligence.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"wisdom\":").Append(character.Wisdom.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"agility\":").Append(character.Agility.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"equipment\":[");
                for (int equipmentIndex = 0; equipmentIndex < character.EquipmentItemIds.Count; equipmentIndex++)
                {
                    if (equipmentIndex > 0) json.Append(',');
                    json.Append(character.EquipmentItemIds[equipmentIndex].ToString(CultureInfo.InvariantCulture));
                }
                json.Append("]}");
            }
            return json.Append("]}").ToString();
        }

        static string BuildCatalogJson(List<MallProductInfo> products)
        {
            StringBuilder json = new StringBuilder("{\"ok\":true,\"products\":[");
            for (int i = 0; i < products.Count; i++)
            {
                MallProductInfo product = products[i];
                if (i > 0) json.Append(',');
                json.Append("{\"id\":").Append(product.ItemId.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"category\":\"").Append(EscapeJson(product.Category))
                    .Append("\",\"basePrice\":").Append(product.BasePrice.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"price\":").Append(product.Price.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"name\":\"").Append(EscapeJson(product.Name))
                    .Append("\",\"description\":\"").Append(EscapeJson(product.Description))
                    .Append("\",\"icon\":\"").Append(EscapeJson(product.Icon))
                    .Append("\",\"average\":").Append(product.AverageRating.ToString("0.0", CultureInfo.InvariantCulture))
                    .Append(",\"reviewCount\":").Append(product.ReviewCount.ToString(CultureInfo.InvariantCulture)).Append('}');
            }
            return json.Append("]}").ToString();
        }

        static string BuildReviewsJson(DataTable reviews)
        {
            StringBuilder json = new StringBuilder("{\"ok\":true,\"reviews\":[");
            double ratingTotal = 0;
            for (int i = 0; i < reviews.Rows.Count; i++)
            {
                DataRow review = reviews.Rows[i];
                int rating = Convert.ToInt32(review["rating"], CultureInfo.InvariantCulture);
                ratingTotal += rating;
                if (i > 0) json.Append(',');
                json.Append("{\"username\":\"").Append(EscapeJson(review["username"].ToString()))
                    .Append("\",\"rating\":").Append(rating.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"review\":\"").Append(EscapeJson(review["reviewText"].ToString()))
                    .Append("\",\"createdAt\":\"").Append(EscapeJson(review["createdAt"].ToString())).Append("\"}");
            }
            double average = reviews.Rows.Count == 0 ? 0 : ratingTotal / reviews.Rows.Count;
            return json.Append("],\"average\":").Append(average.ToString("0.0", CultureInfo.InvariantCulture))
                .Append(",\"count\":").Append(reviews.Rows.Count.ToString(CultureInfo.InvariantCulture)).Append('}').ToString();
        }

        static void WriteResponse(Stream stream, int statusCode, string body, string contentType)
        {
            WriteResponse(stream, statusCode, Encoding.UTF8.GetBytes(body), contentType);
        }

        static void WriteResponse(Stream stream, int statusCode, byte[] body, string contentType)
        {
            string reason = statusCode == 200 ? "OK" :
                statusCode == 201 ? "Created" :
                statusCode == 400 ? "Bad Request" :
                statusCode == 403 ? "Forbidden" :
                statusCode == 404 ? "Not Found" :
                statusCode == 401 ? "Unauthorized" :
                statusCode == 409 ? "Conflict" :
                statusCode == 503 ? "Service Unavailable" :
                statusCode == 413 ? "Payload Too Large" :
                statusCode == 415 ? "Unsupported Media Type" : "Internal Server Error";
            string headers = "HTTP/1.1 " + statusCode + " " + reason + "\r\n" +
                "Content-Type: " + contentType + "\r\n" +
                "Content-Length: " + body.Length.ToString(CultureInfo.InvariantCulture) + "\r\n" +
                "Connection: close\r\n" +
                "Cache-Control: no-store\r\n" +
                "X-Content-Type-Options: nosniff\r\n" +
                "Content-Security-Policy: default-src 'self'; style-src 'self'; script-src 'self'; form-action 'self'; frame-ancestors 'none'; base-uri 'none'\r\n\r\n";
            byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(body, 0, body.Length);
            stream.Flush();
        }
    }
}
