using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using DataBase;

namespace Server
{
    public sealed class RegistrationServerStandalone
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

        public RegistrationServerStandalone(UserDataBase userDataBase, string webDirectory)
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
            Console.WriteLine($"Registration server started on port {Port}");
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
                        Console.WriteLine("SocketException: " + ex.Message);
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
                    NetworkStream stream = client.GetStream();
                    using (stream)
                    {
                        byte[] buffer = new byte[MaximumRequestBodyLength];
                        int read = stream.Read(buffer, 0, buffer.Length);

                        string request = Encoding.ASCII.GetString(buffer, 0, read);
                        string[] lines = request.Split(new[] { "\r\n" }, StringSplitOptions.None);

                        if (lines.Length < 1)
                            return;

                        string[] requestLineTokens = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (requestLineTokens.Length < 2)
                            return;

                        string method = requestLineTokens[0];
                        string path = requestLineTokens[1];

                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {method} {path}");

                        // Parse request body for POST
                        string body = "";
                        int bodyStart = request.IndexOf("\r\n\r\n");
                        if (bodyStart > 0)
                        {
                            body = request.Substring(bodyStart + 4);
                        }

                        byte[] response = null;
                        string contentType = "text/html; charset=utf-8";

                        if (method == "GET")
                        {
                            if (path == "/" || path == "/index.html" || path == "/register.html")
                            {
                                response = page;
                            }
                            else if (path == "/register.css")
                            {
                                response = stylesheet;
                                contentType = "text/css";
                            }
                            else if (path == "/register.js")
                            {
                                response = script;
                                contentType = "application/javascript";
                            }
                            else if (path == "/dashboard.html")
                            {
                                response = dashboardPage;
                            }
                            else if (path == "/dashboard.css")
                            {
                                response = dashboardStylesheet;
                                contentType = "text/css";
                            }
                            else if (path == "/dashboard.js")
                            {
                                response = dashboardScript;
                                contentType = "application/javascript";
                            }
                            else if (path == "/test.html")
                            {
                                response = testPage;
                            }
                            else
                            {
                                SendError(stream, 404, "Not Found");
                                return;
                            }

                            SendOK(stream, response, contentType);
                        }
                        else if (method == "POST")
                        {
                            if (path == "/api/register")
                            {
                                HandleRegister(stream, body);
                            }
                            else if (path == "/api/login")
                            {
                                HandleLogin(stream, body);
                            }
                            else if (path == "/api/reset-password")
                            {
                                HandleResetPassword(stream, body);
                            }
                            else
                            {
                                SendError(stream, 404, "Not Found");
                            }
                        }
                        else
                        {
                            SendError(stream, 400, "Bad Request");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error handling client: " + ex.Message);
                }
            }
        }

        void HandleRegister(NetworkStream stream, string body)
        {
            try
            {
                Dictionary<string, string> data = ParseFormData(body);
                
                if (!data.ContainsKey("username") || !data.ContainsKey("password"))
                {
                    SendJSON(stream, "{\"ok\":false,\"message\":\"Missing username or password\"}");
                    return;
                }

                string username = data["username"];
                string password = data["password"];

                if (userDataBase.GetUser(username) != null)
                {
                    SendJSON(stream, "{\"ok\":false,\"message\":\"Username already exists\"}");
                    return;
                }

                if (username.Length < 4 || username.Length > 14)
                {
                    SendJSON(stream, "{\"ok\":false,\"message\":\"Username must be 4-14 characters\"}");
                    return;
                }

                if (password.Length < 4 || password.Length > 14)
                {
                    SendJSON(stream, "{\"ok\":false,\"message\":\"Password must be 4-14 characters\"}");
                    return;
                }

                userDataBase.InsertUser(username, password);
                SendJSON(stream, "{\"ok\":true,\"message\":\"Account created successfully\"}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in HandleRegister: " + ex.Message);
                SendJSON(stream, "{\"ok\":false,\"message\":\"Server error\"}");
            }
        }

        void HandleLogin(NetworkStream stream, string body)
        {
            try
            {
                Dictionary<string, string> data = ParseFormData(body);
                
                if (!data.ContainsKey("username") || !data.ContainsKey("password"))
                {
                    SendJSON(stream, "{\"ok\":false,\"message\":\"Missing username or password\"}");
                    return;
                }

                string username = data["username"];
                string password = data["password"];

                if (userDataBase.VerifyAccount(username, password))
                {
                    string token = "token_" + username + "_" + DateTime.Now.Ticks;
                    string json = "{\"ok\":true,\"token\":\"" + token + "\",\"message\":\"Login successful\"}";
                    SendJSON(stream, json);
                }
                else
                {
                    SendJSON(stream, "{\"ok\":false,\"message\":\"Invalid username or password\"}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in HandleLogin: " + ex.Message);
                SendJSON(stream, "{\"ok\":false,\"message\":\"Server error\"}");
            }
        }

        void HandleResetPassword(NetworkStream stream, string body)
        {
            try
            {
                Dictionary<string, string> data = ParseFormData(body);
                
                if (!data.ContainsKey("username") || !data.ContainsKey("password"))
                {
                    SendJSON(stream, "{\"ok\":false,\"message\":\"Missing username or password\"}");
                    return;
                }

                string username = data["username"];
                string newPassword = data["password"];

                if (userDataBase.GetUser(username) == null)
                {
                    SendJSON(stream, "{\"ok\":false,\"message\":\"User not found\"}");
                    return;
                }

                if (newPassword.Length < 4 || newPassword.Length > 14)
                {
                    SendJSON(stream, "{\"ok\":false,\"message\":\"Password must be 4-14 characters\"}");
                    return;
                }

                userDataBase.ResetPassword(username, newPassword);
                SendJSON(stream, "{\"ok\":true,\"message\":\"Password reset successfully\"}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in HandleResetPassword: " + ex.Message);
                SendJSON(stream, "{\"ok\":false,\"message\":\"Server error\"}");
            }
        }

        Dictionary<string, string> ParseFormData(string data)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            string[] pairs = data.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string pair in pairs)
            {
                string[] parts = pair.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    result[System.Net.WebUtility.UrlDecode(parts[0])] = System.Net.WebUtility.UrlDecode(parts[1]);
                }
            }
            
            return result;
        }

        void SendOK(NetworkStream stream, byte[] content, string contentType)
        {
            string header = "HTTP/1.1 200 OK\r\n";
            header += "Content-Type: " + contentType + "\r\n";
            header += "Content-Length: " + content.Length + "\r\n";
            header += "Connection: close\r\n";
            header += "\r\n";

            byte[] headerBytes = Encoding.ASCII.GetBytes(header);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(content, 0, content.Length);
            stream.Flush();
        }

        void SendJSON(NetworkStream stream, string json)
        {
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            string header = "HTTP/1.1 200 OK\r\n";
            header += "Content-Type: application/json; charset=utf-8\r\n";
            header += "Content-Length: " + jsonBytes.Length + "\r\n";
            header += "Connection: close\r\n";
            header += "\r\n";

            byte[] headerBytes = Encoding.ASCII.GetBytes(header);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(jsonBytes, 0, jsonBytes.Length);
            stream.Flush();
        }

        void SendError(NetworkStream stream, int code, string message)
        {
            string html = "<html><body><h1>" + code + " " + message + "</h1></body></html>";
            byte[] content = Encoding.UTF8.GetBytes(html);
            string header = "HTTP/1.1 " + code + " " + message + "\r\n";
            header += "Content-Type: text/html\r\n";
            header += "Content-Length: " + content.Length + "\r\n";
            header += "Connection: close\r\n";
            header += "\r\n";

            byte[] headerBytes = Encoding.ASCII.GetBytes(header);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(content, 0, content.Length);
            stream.Flush();
        }
    }

    // Main program to run the server
    class StandaloneProgram
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting Wonderland Registration Server...");
                
                // Set up database
                string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                if (!Directory.Exists(dataDir))
                    Directory.CreateDirectory(dataDir);

                UserDataBase userDB = new UserDataBase(dataDir);
                userDB.Initialize();

                // Set up web directory
                string webDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Web");
                if (!Directory.Exists(webDir))
                {
                    Console.WriteLine("ERROR: Web directory not found at: " + webDir);
                    return;
                }

                // Start server
                RegistrationServerStandalone server = new RegistrationServerStandalone(userDB, webDir);
                server.Start();

                Console.WriteLine("Server running on http://localhost:8090");
                Console.WriteLine("Press CTRL+C to stop...");

                // Keep running
                while (true)
                {
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FATAL ERROR: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
