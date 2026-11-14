#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Simple Wonderland Game Server
Handles basic game client connections on port 6414
"""

import socket
import threading
import sys
import time

# Force UTF-8 output on Windows
if sys.platform == 'win32':
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

HOST = '0.0.0.0'
PORT = 6414

class GameServer:
    def __init__(self, host, port):
        self.host = host
        self.port = port
        self.server_socket = None
        self.running = False
        self.client_count = 0
        
    def start(self):
        try:
            self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            self.server_socket.bind((self.host, self.port))
            self.server_socket.listen(5)
            self.running = True
            
            print("\n" + "="*60)
            print("🎮 WONDERLAND ONLINE - GAME SERVER")
            print("="*60)
            print(f"✅ Server started on {self.host}:{self.port}")
            print("🎯 Ready to accept player connections...")
            print("="*60 + "\n")
            
            while self.running:
                try:
                    client_socket, address = self.server_socket.accept()
                    self.client_count += 1
                    print(f"[{time.strftime('%H:%M:%S')}] ✅ Player connected from {address[0]}:{address[1]} (Total: {self.client_count})")
                    
                    # Handle client in separate thread
                    client_thread = threading.Thread(
                        target=self.handle_client,
                        args=(client_socket, address),
                        daemon=True
                    )
                    client_thread.start()
                    
                except KeyboardInterrupt:
                    break
                except Exception as e:
                    if self.running:
                        print(f"[ERROR] Connection error: {e}")
                        
        except Exception as e:
            print(f"❌ Failed to start server: {e}")
            sys.exit(1)
        finally:
            self.stop()
    
    def handle_client(self, client_socket, address):
        """Handle individual player connection"""
        try:
            # Try to connect to production server
            try:
                prod_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                prod_socket.settimeout(5)
                prod_socket.connect((PRODUCTION_SERVER, PRODUCTION_PORT))
                print(f"[{time.strftime('%H:%M:%S')}] 🔗 {address[0]} connected to production server")
            except Exception as prod_error:
                # Production server unavailable - send game data
                print(f"[{time.strftime('%H:%M:%S')}] ⚠️  Production server unreachable: {prod_error}")
                print(f"[{time.strftime('%H:%M:%S')}] 💾 Sending local game data to {address[0]}")
                
                # Send a simple game initialization packet
                game_data = b"GAME_SERVER_READY|localhost|6414|Welcome to Wonderland Online!\x00"
                client_socket.sendall(game_data)
                prod_socket = None
            
            # Bidirectional proxy
            if prod_socket:
                self.proxy_data(client_socket, prod_socket, address)
            else:
                # Keep connection open for a bit
                time.sleep(2)
                
        except Exception as e:
            print(f"[{time.strftime('%H:%M:%S')}] ❌ Error handling {address[0]}: {e}")
        finally:
            try:
                client_socket.close()
            except:
                pass
            if prod_socket:
                try:
                    prod_socket.close()
                except:
                    pass
            print(f"[{time.strftime('%H:%M:%S')}] 👋 {address[0]} disconnected")
    
    def proxy_data(self, client_socket, prod_socket, address):
        """Proxy data between client and production server"""
        def forward(source, dest, direction):
            try:
                while self.running:
                    data = source.recv(4096)
                    if not data:
                        break
                    dest.sendall(data)
            except:
                pass
            finally:
                try:
                    dest.close()
                except:
                    pass
        
        # Start bidirectional proxy threads
        t1 = threading.Thread(target=forward, args=(client_socket, prod_socket, "→"), daemon=True)
        t2 = threading.Thread(target=forward, args=(prod_socket, client_socket, "←"), daemon=True)
        t1.start()
        t2.start()
        t1.join()
        t2.join()
    
    def stop(self):
        self.running = False
        if self.server_socket:
            try:
                self.server_socket.close()
            except:
                pass
        print("\n✅ Game server stopped.\n")

if __name__ == "__main__":
    server = GameServer(HOST, PORT)
    try:
        server.start()
    except KeyboardInterrupt:
        print("\n🛑 Server shutdown requested")
        server.stop()
    except Exception as e:
        print(f"❌ Fatal error: {e}")
        sys.exit(1)
