using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/// <summary>
/// Standalone test server for registration page
/// Tests that the registration page and API are working correctly
/// </summary>
public class TestRegistrationServer
{
    const int Port = 8090;
    private TcpListener listener;
    private byte[] htmlContent;
    private byte[] cssContent;
    private byte[] jsContent;
    private bool running;

    public TestRegistrationServer(string webDirectory)
    {
        htmlContent = File.ReadAllBytes(Path.Combine(webDirectory, "register.html"));
        cssContent = File.ReadAllBytes(Path.Combine(webDirectory, "register.css"));
        jsContent = File.ReadAllBytes(Path.Combine(webDirectory, "register.js"));
        listener = new TcpListener(IPAddress.Any, Port);
    }

    public void Start()
    {
        listener.Start();
        running = true;
        Thread listenerThread = new Thread(Listen);
        listenerThread.IsBackground = true;
        listenerThread.Name = "Test Registration Server";
        listenerThread.Start();
        Console.WriteLine($"✓ Test Registration Server started on port {Port}");
        Console.WriteLine($"  Access at: http://localhost:{Port}");
    }

    public void Stop()
    {
        running = false;
        listener.Stop();
    }

    private void Listen()
    {
        while (running)
        {
            try
            {
                TcpClient client = listener.AcceptTcpClient();
                HandleClient(client);
            }
            catch (SocketException) { break; }
            catch (ObjectDisposedException) { break; }
        }
    }

    private void HandleClient(TcpClient client)
    {
        using (client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    // Read HTTP request
                    byte[] buffer = new byte[4096];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string requestText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    
                    string[] lines = requestText.Split(new[] { "\r\n" }, StringSplitOptions.None);
                    string requestLine = lines[0];
                    string[] parts = requestLine.Split(' ');
                    
                    if (parts.Length < 2) return;
                    
                    string method = parts[0];
                    string path = parts[1];

                    Console.WriteLine($"  {method} {path}");

                    // Route requests
                    if (path == "/" || path == "/index.html")
                    {
                        SendResponse(stream, 200, "text/html; charset=utf-8", htmlContent);
                    }
                    else if (path == "/register.css")
                    {
                        SendResponse(stream, 200, "text/css", cssContent);
                    }
                    else if (path == "/register.js")
                    {
                        SendResponse(stream, 200, "application/javascript", jsContent);
                    }
                    else if (path == "/api/create-account" && method == "POST")
                    {
                        string response = "{\"success\":true,\"message\":\"Test: Account creation would be processed here\"}";
                        SendResponse(stream, 200, "application/json", Encoding.UTF8.GetBytes(response));
                    }
                    else if (path == "/api/reset-password" && method == "POST")
                    {
                        string response = "{\"success\":true,\"message\":\"Test: Password reset would be processed here\"}";
                        SendResponse(stream, 200, "application/json", Encoding.UTF8.GetBytes(response));
                    }
                    else
                    {
                        SendResponse(stream, 404, "text/plain", Encoding.UTF8.GetBytes("Not Found"));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}");
            }
        }
    }

    private void SendResponse(NetworkStream stream, int statusCode, string contentType, byte[] content)
    {
        string statusMessage = statusCode == 200 ? "OK" : statusCode == 404 ? "Not Found" : "Error";
        
        StringBuilder response = new StringBuilder();
        response.AppendLine($"HTTP/1.1 {statusCode} {statusMessage}");
        response.AppendLine($"Content-Type: {contentType}");
        response.AppendLine($"Content-Length: {content.Length}");
        response.AppendLine("Connection: close");
        response.AppendLine();

        byte[] header = Encoding.UTF8.GetBytes(response.ToString());
        stream.Write(header, 0, header.Length);
        stream.Write(content, 0, content.Length);
        stream.Flush();
    }

    static void Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  Wonderland Online - Test Registration Server             ║");
        Console.WriteLine("║  Beautiful Registration Page Test                         ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        try
        {
            string webDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Web");
            
            if (!File.Exists(Path.Combine(webDirectory, "register.html")))
            {
                Console.WriteLine("✗ ERROR: Web directory not found or files missing");
                Console.WriteLine($"  Expected: {webDirectory}");
                return;
            }

            TestRegistrationServer server = new TestRegistrationServer(webDirectory);
            server.Start();

            Console.WriteLine("\n✨ Ready for testing! Open your browser and go to:");
            Console.WriteLine("   → http://localhost:8090");
            Console.WriteLine("\nFeatures to test:");
            Console.WriteLine("   ✓ Beautiful UI with 3 tabs (Create Account, Forgot Password, Login Info)");
            Console.WriteLine("   ✓ Real-time password strength meter");
            Console.WriteLine("   ✓ Form validation and error messages");
            Console.WriteLine("   ✓ Responsive design (resize browser to test)");
            Console.WriteLine("   ✓ Smooth animations and transitions\n");

            Console.WriteLine("Press Ctrl+C to stop the server...\n");

            // Keep server running
            while (true)
            {
                Thread.Sleep(1000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
        }
    }
}
