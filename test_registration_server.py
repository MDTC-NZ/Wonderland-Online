#!/usr/bin/env python3
"""
Standalone test server for Wonderland Online registration page
Serves the beautiful registration interface without requiring the full game server
"""

import http.server
import socketserver
import os
import json
import urllib.parse
from pathlib import Path

PORT = 8090
WEB_DIR = Path(__file__).parent / "Web"

# In-memory user database for testing
test_users = {}

class RegistrationHandler(http.server.SimpleHTTPRequestHandler):
    def do_GET(self):
        """Handle GET requests"""
        if self.path == "/" or self.path == "/index.html":
            self.serve_file("register.html", "text/html")
        elif self.path == "/register.html":
            self.serve_file("register.html", "text/html")
        elif self.path == "/register.css":
            self.serve_file("register.css", "text/css")
        elif self.path == "/register.js":
            self.serve_file("register.js", "application/javascript")
        else:
            self.send_error(404)
    
    def do_POST(self):
        """Handle POST requests (API calls)"""
        content_length = int(self.headers.get('Content-Length', 0))
        body = self.rfile.read(content_length)
        
        # Parse form data
        params = urllib.parse.parse_qs(body.decode('utf-8'))
        
        if self.path == "/api/create-account":
            # Extract parameters
            username = params.get('username', [''])[0]
            password = params.get('password', [''])[0]
            
            # Validate username
            if not username or len(username) < 4 or len(username) > 14:
                self.send_json_response(400, {
                    "success": False,
                    "message": "Username must be 4-14 characters"
                })
                return
            
            # Validate password
            if not password or len(password) < 4 or len(password) > 14:
                self.send_json_response(400, {
                    "success": False,
                    "message": "Password must be 4-14 characters"
                })
                return
            
            # Check if username already exists
            if username.lower() in [u.lower() for u in test_users.keys()]:
                self.send_json_response(400, {
                    "success": False,
                    "message": "Username already exists"
                })
                return
            
            # Store the account (in real system, would hash password)
            test_users[username] = {
                'password': password,
                'username': username
            }
            
            self.send_json_response(200, {
                "success": True,
                "message": f"Account '{username}' created successfully! You can now login."
            })
            print(f"  ✓ Account created: {username}")
            
        elif self.path == "/api/reset-password":
            # Parse request body
            params = urllib.parse.parse_qs(body.decode('utf-8'))
            step = params.get('step', ['1'])[0]
            username = params.get('username', [''])[0]
            
            if step == '1':
                # Step 1: Verify username exists
                user_exists = any(u.lower() == username.lower() for u in test_users.keys())
                
                if user_exists:
                    self.send_json_response(200, {
                        "success": True,
                        "exists": True,
                        "message": "Account found. Enter new password."
                    })
                else:
                    self.send_json_response(200, {
                        "success": True,
                        "exists": False,
                        "message": "Account not found"
                    })
            
            elif step == '2':
                # Step 2: Update password
                new_password = params.get('password', [''])[0]
                
                # Find the user (case-insensitive)
                actual_username = None
                for u in test_users.keys():
                    if u.lower() == username.lower():
                        actual_username = u
                        break
                
                if not actual_username:
                    self.send_json_response(400, {
                        "success": False,
                        "message": "Account not found"
                    })
                    return
                
                # Validate new password
                if not new_password or len(new_password) < 4 or len(new_password) > 14:
                    self.send_json_response(400, {
                        "success": False,
                        "message": "Password must be 4-14 characters"
                    })
                    return
                
                # Update password
                test_users[actual_username]['password'] = new_password
                
                self.send_json_response(200, {
                    "success": True,
                    "message": "Password reset successfully!"
                })
                print(f"  ✓ Password reset for: {actual_username}")
            
            else:
                self.send_json_response(400, {
                    "success": False,
                    "message": "Invalid step"
                })
        else:
            self.send_error(404)
    
    def serve_file(self, filename, content_type):
        """Serve a static file"""
        file_path = WEB_DIR / filename
        try:
            with open(file_path, 'rb') as f:
                content = f.read()
            
            self.send_response(200)
            self.send_header("Content-Type", content_type)
            self.send_header("Content-Length", len(content))
            self.end_headers()
            self.wfile.write(content)
            
            # Log the request
            print(f"  ✓ {self.command} {self.path} - {len(content)} bytes")
        except FileNotFoundError:
            self.send_error(404)
    
    def send_json_response(self, status_code, data):
        """Send a JSON response"""
        import json
        response = json.dumps(data).encode('utf-8')
        self.send_response(status_code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", len(response))
        self.end_headers()
        self.wfile.write(response)
        print(f"  ✓ {self.command} {self.path} - JSON response")
    
    def log_message(self, format, *args):
        """Override to use custom logging"""
        pass  # Suppress default logging

if __name__ == "__main__":
    import sys
    # Set UTF-8 encoding for Windows console
    if sys.platform == 'win32':
        import codecs
        sys.stdout = codecs.getwriter('utf-8')(sys.stdout.buffer, 'strict')
    
    print("=" * 62)
    print("  Wonderland Online - Registration Server (Test Mode)")
    print("  Beautiful Registration Page Demonstration")
    print("=" * 62 + "\n")
    
    # Change to the script directory
    os.chdir(Path(__file__).parent)
    
    # Check if Web directory exists
    if not WEB_DIR.exists():
        print("✗ ERROR: Web directory not found")
        print(f"  Expected: {WEB_DIR}")
        exit(1)
    
    # Check for required files
    required_files = ["register.html", "register.css", "register.js"]
    missing_files = [f for f in required_files if not (WEB_DIR / f).exists()]
    
    if missing_files:
        print(f"✗ Missing files: {', '.join(missing_files)}")
        exit(1)
    
    print(f"✓ Web directory found: {WEB_DIR}")
    print(f"✓ All registration files present\n")
    
    # Start server
    try:
        with socketserver.TCPServer(("", PORT), RegistrationHandler) as httpd:
            print(f"🚀 Registration Server started on port {PORT}\n")
            print("✨ Your beautiful registration page is ready!\n")
            print("📍 Access at:")
            print(f"   → http://localhost:{PORT}")
            print(f"   → http://127.0.0.1:{PORT}\n")
            print("Features to explore:")
            print("   ✓ 3-Tab Interface (Create Account, Forgot Password, Login Info)")
            print("   ✓ Real-time Password Strength Meter")
            print("   ✓ Beautiful Dark Theme with Gradients and Animations")
            print("   ✓ Form Validation with Instant Feedback")
            print("   ✓ Responsive Design (Test on mobile/tablet/desktop)")
            print("   ✓ Smooth Transitions and Hover Effects\n")
            print("API Endpoints (Test Mode):")
            print("   POST /api/create-account")
            print("   POST /api/reset-password\n")
            print("Press Ctrl+C to stop the server...\n")
            
            httpd.serve_forever()
    except KeyboardInterrupt:
        print("\n✓ Server stopped")
    except OSError as e:
        print(f"✗ Error: {e}")
        if "Address already in use" in str(e):
            print(f"  Port {PORT} is already in use")
            print("  Try: netstat -ano | findstr :8090")
