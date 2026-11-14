using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using DataBase;

namespace Server
{
    public sealed class RegistrationServer
    {
        const int Port = 8090;
        const int MaximumRequestBodyLength = 4096;

        readonly UserDataBase userDataBase;
        readonly TcpListener listener;
        readonly byte[] page;
        readonly byte[] stylesheet;
        readonly byte[] script;
        readonly byte[] dashboardPage;
        readonly byte[] dashboardStylesheet;
        readonly byte[] dashboardScript;
        readonly byte[] testPage;
        Thread listenerThread;
        volatile bool running;

        public RegistrationServer(UserDataBase userDataBase, string webDirectory, string certPath = null, string certPassword = null)
        {
            if (userDataBase == null)
                throw new ArgumentNullException("userDataBase");
            if (webDirectory == null)
                throw new ArgumentNullException("webDirectory");

            this.userDataBase = userDataBase;
            
            page = File.ReadAllBytes(Path.Combine(webDirectory, "register.html"));
            stylesheet = File.ReadAllBytes(Path.Combine(webDirectory, "register.css"));
            script = File.ReadAllBytes(Path.Combine(webDirectory, "register.js"));
            dashboardPage = File.ReadAllBytes(Path.Combine(webDirectory, "dashboard.html"));
            dashboardStylesheet = File.ReadAllBytes(Path.Combine(webDirectory, "dashboard.css"));
            dashboardScript = File.ReadAllBytes(Path.Combine(webDirectory, "dashboard.js"));
            testPage = File.ReadAllBytes(Path.Combine(webDirectory, "test.html"));
            // Allow connections from any IP address (not just localhost)
            listener = new TcpListener(IPAddress.Any, Port);
        }

        public void Start()
        {
            listener.Start();
            running = true;
            listenerThread = new Thread(Listen);
            listenerThread.IsBackground = true;
            listenerThread.Name = "Registration HTTP server";
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

                    using (NetworkStream stream = client.GetStream())
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
                            if (path == "/")
                                WriteResponse(stream, 200, page, "text/html; charset=utf-8");
                            else if (path == "/register.css")
                                WriteResponse(stream, 200, stylesheet, "text/css; charset=utf-8");
                            else if (path == "/register.js")
                                WriteResponse(stream, 200, script, "application/javascript; charset=utf-8");
                            else if (path == "/dashboard.html")
                                WriteResponse(stream, 200, dashboardPage, "text/html; charset=utf-8");
                            else if (path == "/dashboard.css")
                                WriteResponse(stream, 200, dashboardStylesheet, "text/css; charset=utf-8");
                            else if (path == "/dashboard.js")
                                WriteResponse(stream, 200, dashboardScript, "application/javascript; charset=utf-8");
                            else if (path == "/test.html")
                                WriteResponse(stream, 200, testPage, "text/html; charset=utf-8");
                            else
                                WriteResponse(stream, 404, "Not found.", "text/plain; charset=utf-8");
                            return;
                        }

                        if (requestParts[0] != "POST" || (path != "/api/register" && path != "/api/reset-password" && path != "/api/login"))
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

                        // Handle Password Reset
                        if (path == "/api/reset-password")
                        {
                            string username;
                            string newPassword;
                            if (!form.TryGetValue("username", out username) ||
                                !form.TryGetValue("newPassword", out newPassword))
                            {
                                WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Username and new password are required.\"}", "application/json; charset=utf-8");
                                return;
                            }

                            try
                            {
                                if (!userDataBase.ResetPassword(username, newPassword))
                                {
                                    WriteResponse(stream, 404, "{\"ok\":false,\"message\":\"Username not found.\"}", "application/json; charset=utf-8");
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

                            if (userDataBase.VerifyAccount(username, password))
                            {
                                string token = "token_" + username + "_" + DateTime.UtcNow.Ticks;
                                WriteResponse(stream, 200, "{\"ok\":true,\"token\":\"" + EscapeJson(token) + "\",\"message\":\"Login successful.\"}", "application/json; charset=utf-8");
                                return;
                            }
                            else
                            {
                                WriteResponse(stream, 401, "{\"ok\":false,\"message\":\"Invalid username or password.\"}", "application/json; charset=utf-8");
                                return;
                            }
                        }

                        // Handle Registration
                        string username2;
                        string password;
                        string confirmation;
                        if (!form.TryGetValue("username", out username2) ||
                            !form.TryGetValue("password", out password) ||
                            !form.TryGetValue("passwordConfirm", out confirmation) ||
                            password != confirmation)
                        {
                            WriteResponse(stream, 400, "{\"ok\":false,\"message\":\"Check the fields and make sure both passwords match.\"}", "application/json; charset=utf-8");
                            return;
                        }

                        try
                        {
                            if (!userDataBase.CreateAccount(username2, password))
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
                    }
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
                !Uri.TryCreate(origin, UriKind.Absolute, out originUri))
                return false;

            // Accept any request from localhost/127.0.0.1 on any port
            string originHost = originUri.Authority;
            if (originHost.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) ||
                originHost.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                originHost.StartsWith("wlo.kiaoragaming.xyz", StringComparison.OrdinalIgnoreCase))
            {
                if (originUri.Scheme == Uri.UriSchemeHttps)
                    return true;

                return originUri.Scheme == Uri.UriSchemeHttp;
            }

            return false;
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
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
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
                statusCode == 409 ? "Conflict" :
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
