#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Wonderland Online Registration Server
Handles user registration, login, and character management
Accessible from network at: 192.168.1.8:8090 (or your computer's IP)
"""

import http.server
import socketserver
import json
import sqlite3
import os
import subprocess
import socket
import sys
import io
from urllib.parse import parse_qs
from datetime import datetime

# Force UTF-8 output on Windows
if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')
from datetime import datetime

PORT = 8090
DB_PATH = os.path.expandvars("%APPDATA%\\PServer\\registration.db")
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
WEB_DIR = os.path.join(SCRIPT_DIR, "bin", "Debug", "Web")
GAME_LAUNCHER = os.path.join(SCRIPT_DIR, "game_launcher.py")

def get_local_ip():
    """Get the local network IP address"""
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        # Connect to a non-routable address to find local IP
        s.connect(("8.8.8.8", 80))
        ip = s.getsockname()[0]
        s.close()
        return ip
    except:
        return "localhost"

def ensure_db():
    os.makedirs(os.path.dirname(DB_PATH), exist_ok=True)
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    c.execute('''CREATE TABLE IF NOT EXISTS users (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        username TEXT UNIQUE NOT NULL,
        password TEXT NOT NULL,
        email TEXT,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        character1 TEXT,
        character2 TEXT
    )''')
    conn.commit()
    conn.close()

class RegistrationHandler(http.server.SimpleHTTPRequestHandler):
    def do_GET(self):
        try:
            if self.path == '/' or self.path == '/register.html':
                self.send_response(200)
                self.send_header('Content-type', 'text/html; charset=utf-8')
                self.end_headers()
                filepath = os.path.join(WEB_DIR, 'register.html')
                with open(filepath, 'r', encoding='utf-8') as f:
                    self.wfile.write(f.read().encode('utf-8'))
            elif self.path == '/dashboard.html':
                self.send_response(200)
                self.send_header('Content-type', 'text/html; charset=utf-8')
                self.end_headers()
                filepath = os.path.join(WEB_DIR, 'dashboard.html')
                with open(filepath, 'r', encoding='utf-8') as f:
                    self.wfile.write(f.read().encode('utf-8'))
            elif self.path == '/register.css':
                self.send_response(200)
                self.send_header('Content-type', 'text/css; charset=utf-8')
                self.end_headers()
                filepath = os.path.join(WEB_DIR, 'register.css')
                with open(filepath, 'r', encoding='utf-8') as f:
                    self.wfile.write(f.read().encode('utf-8'))
            elif self.path == '/dashboard.css':
                self.send_response(200)
                self.send_header('Content-type', 'text/css; charset=utf-8')
                self.end_headers()
                filepath = os.path.join(WEB_DIR, 'dashboard.css')
                with open(filepath, 'r', encoding='utf-8') as f:
                    self.wfile.write(f.read().encode('utf-8'))
            elif self.path == '/register.js':
                self.send_response(200)
                self.send_header('Content-type', 'application/javascript; charset=utf-8')
                self.end_headers()
                filepath = os.path.join(WEB_DIR, 'register.js')
                with open(filepath, 'r', encoding='utf-8') as f:
                    self.wfile.write(f.read().encode('utf-8'))
            elif self.path == '/dashboard.js':
                self.send_response(200)
                self.send_header('Content-type', 'application/javascript; charset=utf-8')
                self.end_headers()
                filepath = os.path.join(WEB_DIR, 'dashboard.js')
                with open(filepath, 'r', encoding='utf-8') as f:
                    self.wfile.write(f.read().encode('utf-8'))
            elif self.path == '/test.html':
                self.send_response(200)
                self.send_header('Content-type', 'text/html; charset=utf-8')
                self.end_headers()
                filepath = os.path.join(WEB_DIR, 'test.html')
                with open(filepath, 'r', encoding='utf-8') as f:
                    self.wfile.write(f.read().encode('utf-8'))
            elif self.path == '/config.js':
                self.send_response(200)
                self.send_header('Content-type', 'application/javascript; charset=utf-8')
                self.end_headers()
                filepath = os.path.join(WEB_DIR, 'config.js')
                with open(filepath, 'r', encoding='utf-8') as f:
                    self.wfile.write(f.read().encode('utf-8'))
            elif self.path == '/api/server-stats':
                self.send_response(200)
                self.send_header('Content-type', 'application/json; charset=utf-8')
                self.end_headers()
                
                # Get server stats from database
                try:
                    conn = sqlite3.connect(DB_PATH)
                    c = conn.cursor()
                    
                    # Count total accounts
                    c.execute('SELECT COUNT(*) FROM users')
                    total_accounts = c.fetchone()[0] or 0
                    
                    # Get all characters (from character1 and character2 fields)
                    c.execute('SELECT character1, character2 FROM users')
                    all_chars = []
                    for row in c.fetchall():
                        if row[0]:
                            try:
                                char_data = json.loads(row[0])
                                all_chars.append(char_data)
                            except:
                                pass
                        if row[1]:
                            try:
                                char_data = json.loads(row[1])
                                all_chars.append(char_data)
                            except:
                                pass
                    
                    conn.close()
                    
                    # Format response
                    stats = {
                        'activePlayers': 1,  # Placeholder
                        'totalAccounts': total_accounts,
                        'totalGuilds': 0,
                        'serverStatus': 'Online',
                        'uptime': '99.9%',
                        'topCharacters': all_chars[:5]
                    }
                    
                    self.wfile.write(json.dumps(stats).encode('utf-8'))
                except Exception as e:
                    self.wfile.write(json.dumps({'error': str(e), 'activePlayers': 0, 'totalAccounts': 0, 'totalGuilds': 0}).encode('utf-8'))
            else:
                self.send_response(404)
                self.send_header('Content-type', 'text/plain')
                self.end_headers()
                self.wfile.write(f"404 Not Found: {self.path}".encode())
        except Exception as e:
            print(f"[ERROR] {e}")
            self.send_response(500)
            self.send_header('Content-type', 'text/plain')
            self.end_headers()
            self.wfile.write(f"500 Server Error: {str(e)}".encode())
    
    def do_POST(self):
        content_length = int(self.headers.get('Content-Length', 0))
        body = self.rfile.read(content_length).decode()
        
        # Try to parse as JSON first, then fall back to form-encoded
        try:
            data = json.loads(body) if body else {}
        except:
            data = {}
            params = parse_qs(body)
            for key, val in params.items():
                data[key] = val[0] if val else ''
        
        response = {}
        
        try:
            if self.path == '/api/register':
                username = data.get('username', '')
                password = data.get('password', '')
                confirm = data.get('passwordConfirm', '')
                
                if not username or len(username) < 4:
                    response = {'ok': False, 'message': 'Username must be at least 4 characters'}
                elif password != confirm:
                    response = {'ok': False, 'message': 'Passwords do not match'}
                elif len(password) < 4:
                    response = {'ok': False, 'message': 'Password must be at least 4 characters'}
                else:
                    conn = sqlite3.connect(DB_PATH)
                    c = conn.cursor()
                    try:
                        c.execute('INSERT INTO users (username, password) VALUES (?, ?)', (username, password))
                        conn.commit()
                        response = {'ok': True, 'message': f'Account created! Welcome, {username}!'}
                    except sqlite3.IntegrityError:
                        response = {'ok': False, 'message': 'Username already exists'}
                    finally:
                        conn.close()
            
            elif self.path == '/api/login':
                username = data.get('username', '')
                password = data.get('password', '')
                
                conn = sqlite3.connect(DB_PATH)
                c = conn.cursor()
                c.execute('SELECT id FROM users WHERE username = ? AND password = ?', (username, password))
                user = c.fetchone()
                conn.close()
                
                if user:
                    response = {'ok': True, 'message': 'Login successful!', 'token': f'token_{user[0]}_{datetime.now().timestamp()}'}
                else:
                    response = {'ok': False, 'message': 'Invalid username or password'}
            
            elif self.path == '/api/reset-password':
                username = data.get('username', '')
                password = data.get('newPassword', '')
                
                if len(password) < 4:
                    response = {'ok': False, 'message': 'Password must be at least 4 characters'}
                else:
                    conn = sqlite3.connect(DB_PATH)
                    c = conn.cursor()
                    c.execute('UPDATE users SET password = ? WHERE username = ?', (password, username))
                    conn.commit()
                    if c.rowcount > 0:
                        response = {'ok': True, 'message': 'Password reset successful!'}
                    else:
                        response = {'ok': False, 'message': 'User not found'}
                    conn.close()
            
            elif self.path == '/api/user-stats':
                username = data.get('username', '')
                token = data.get('token', '')
                
                if not username or not token:
                    response = {'success': False, 'error': 'Missing username or token'}
                else:
                    conn = sqlite3.connect(DB_PATH)
                    c = conn.cursor()
                    c.execute('SELECT id, created_at FROM users WHERE username = ?', (username,))
                    user = c.fetchone()
                    conn.close()
                    
                    if user:
                        response = {
                            'success': True,
                            'stats': {
                                'username': username,
                                'created_at': user[1] or datetime.now().strftime('%m/%d/%Y'),
                                'last_login': datetime.now().strftime('%m/%d/%Y, %I:%M:%S %p'),
                                'playtime_hours': 0,
                                'total_level': 0,
                                'total_gold': 0,
                                'reward_points': 0,
                                'mall_points': 0,
                                'characters_created': 0
                            }
                        }
                    else:
                        response = {'success': False, 'error': 'User not found'}
            
            elif self.path == '/api/launch-game':
                character_name = data.get('character_name', '')
                character_id = data.get('character_id', '')
                user_token = data.get('user_token', '')
                
                if not character_name or not character_id or not user_token:
                    response = {'success': False, 'error': 'Missing required parameters'}
                else:
                    try:
                        # Launch the game in a separate process connecting to local game server
                        subprocess.Popen(
                            ['python', GAME_LAUNCHER, character_name, str(character_id), user_token, 'localhost', '6414'],
                            cwd=SCRIPT_DIR
                        )
                        response = {'success': True, 'message': f'Game launching for {character_name}...'}
                    except Exception as e:
                        response = {'success': False, 'error': f'Failed to launch game: {str(e)}'}
        
        except Exception as e:
            response = {'ok': False, 'success': False, 'message': f'Error: {str(e)}', 'error': str(e)}
        
        self.send_response(200)
        self.send_header('Content-type', 'application/json')
        self.send_header('Access-Control-Allow-Origin', '*')
        self.end_headers()
        self.wfile.write(json.dumps(response).encode())

ensure_db()

# Get local IP for network access
local_ip = get_local_ip()

# Bind to all interfaces (0.0.0.0) to allow network access
with socketserver.TCPServer(("0.0.0.0", PORT), RegistrationHandler) as httpd:
    print("╔════════════════════════════════════════════════════════════════╗")
    print("║         🎮 Wonderland Online Registration Server 🎮            ║")
    print("╚════════════════════════════════════════════════════════════════╝")
    print()
    print(f"✓ Server Running on Port: {PORT}")
    print()
    print("📍 Access from:")
    print(f"   • This computer: http://localhost:{PORT}")
    print(f"   • Local network: http://{local_ip}:{PORT}")
    print(f"   • Other computers: http://{local_ip}:{PORT}")
    print()
    print("📌 Share this link with other players:")
    print(f"   http://{local_ip}:{PORT}")
    print()
    print("🌐 Endpoints available:")
    print("   • /api/register - Create account")
    print("   • /api/login - Login")
    print("   • /api/reset-password - Reset password")
    print("   • /api/launch-game - Launch game")
    print()
    print("[*] Press Ctrl+C to stop the server")
    print()
    httpd.serve_forever()
