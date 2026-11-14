#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Wonderland Online - Local Game Server
Handles game client connections and provides game session management
"""

import socket
import threading
import sys
import time
import struct
import json

# Force UTF-8 output on Windows
if sys.platform == 'win32':
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

HOST = '0.0.0.0'
PORT = 6414
TIMEOUT = 30  # Client timeout in seconds

class GameServer:
    def __init__(self, host, port):
        self.host = host
        self.port = port
        self.server_socket = None
        self.running = False
        self.client_count = 0
        self.connected_players = {}
        
    def start(self):
        try:
            self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            self.server_socket.bind((self.host, self.port))
            self.server_socket.listen(5)
            self.running = True
            
            print("\n" + "="*70)
            print("🎮 WONDERLAND ONLINE - LOCAL GAME SERVER")
            print("="*70)
            print(f"✅ Server running on {self.host}:{self.port}")
            print(f"📍 Ready to accept players...")
            print("="*70 + "\n")
            
            while self.running:
                try:
                    client_socket, address = self.server_socket.accept()
                    self.client_count += 1
                    player_id = self.client_count
                    
                    timestamp = time.strftime('%H:%M:%S')
                    print(f"[{timestamp}] ✅ PLAYER CONNECTED")
                    print(f"    IP: {address[0]}:{address[1]}")
                    print(f"    Player ID: {player_id}")
                    print(f"    Total Connected: {self.client_count}\n")
                    
                    # Handle client in separate thread
                    client_thread = threading.Thread(
                        target=self.handle_client,
                        args=(client_socket, address, player_id),
                        daemon=True
                    )
                    client_thread.start()
                    
                except KeyboardInterrupt:
                    print("\nShutting down...")
                    break
                except Exception as e:
                    if self.running:
                        print(f"[ERROR] {e}\n")
                        
        except Exception as e:
            print(f"FATAL ERROR: {e}")
            sys.exit(1)
        finally:
            self.stop()
    
    def handle_client(self, client_socket, address, player_id):
        """Handle individual player connection"""
        try:
            client_socket.settimeout(TIMEOUT)
            player_name = f"Player_{player_id}"
            self.connected_players[player_id] = {
                'ip': address[0],
                'connected_at': time.time(),
                'name': player_name
            }
            
            # Send welcome message
            welcome = f"WELCOME {player_name}".encode('utf-8')
            client_socket.send(welcome)
            
            # Keep connection alive and receive any data
            while self.running:
                try:
                    data = client_socket.recv(1024)
                    
                    if not data:
                        break
                    
                    message = data.decode('utf-8', errors='ignore').strip()
                    print(f"[{time.strftime('%H:%M:%S')}] ← Player {player_id}: {message}")
                    
                    # Send acknowledgment
                    response = f"ACK: {message}".encode('utf-8')
                    client_socket.send(response)
                    
                except socket.timeout:
                    # Keep connection alive, just waiting
                    pass
                except Exception as e:
                    print(f"[ERROR] Player {player_id}: {e}")
                    break
            
            # Player disconnected
            if player_id in self.connected_players:
                del self.connected_players[player_id]
            
            timestamp = time.strftime('%H:%M:%S')
            print(f"[{timestamp}] ❌ PLAYER DISCONNECTED")
            print(f"    Player ID: {player_id}")
            print(f"    Total Connected: {len(self.connected_players)}\n")
            
        except Exception as e:
            print(f"[FATAL] Player {player_id} handler error: {e}")
        finally:
            try:
                client_socket.close()
            except:
                pass
    
    def stop(self):
        """Stop the server"""
        self.running = False
        if self.server_socket:
            try:
                self.server_socket.close()
            except:
                pass
        
        print("\n" + "="*70)
        print("✅ Game server stopped gracefully")
        print("="*70 + "\n")

def main():
    server = GameServer(HOST, PORT)
    try:
        server.start()
    except KeyboardInterrupt:
        print("\nShutting down...")
        server.stop()
    except Exception as e:
        print(f"Fatal error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()
