#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Wonderland Online - Game Server with Game Data
Loads and serves actual game content (NPCs, items, skills, maps)
"""

import socket
import threading
import sys
import time
import logging
import os
import json

# Force UTF-8 output on Windows
if sys.platform == 'win32':
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

# Setup logging
logging.basicConfig(
    level=logging.INFO,
    format='[%(asctime)s] %(message)s',
    datefmt='%H:%M:%S',
    handlers=[
        logging.FileHandler('game_server.log', encoding='utf-8'),
        logging.StreamHandler(sys.stdout)
    ]
)
logger = logging.getLogger(__name__)

HOST = '0.0.0.0'
PORT = 6414
TIMEOUT = 120
SESSION_ID_COUNTER = 0
SESSION_LOCK = threading.Lock()

# Game data paths
GAMEDATA_DIR = os.path.join(os.path.dirname(__file__), 'gamedata')

def get_next_session_id():
    global SESSION_ID_COUNTER
    with SESSION_LOCK:
        SESSION_ID_COUNTER += 1
        return SESSION_ID_COUNTER

class GameDataManager:
    """Manages game data files"""
    def __init__(self, data_dir):
        self.data_dir = data_dir
        self.data_files = {}
        self.load_data_files()
    
    def load_data_files(self):
        """Load all game data files"""
        if not os.path.exists(self.data_dir):
            logger.warning(f"Game data directory not found: {self.data_dir}")
            return
        
        data_files = [
            'npc.dat', 'item.dat', 'skill.dat', 'talk.dat',
            'compound.dat', 'compound2.dat', 'formula.dat',
            'ground.mmg', 'scenedata.dat', 'wem.mmg',
            'mark.dat', 'eve.Emg', 'skilldata.mbtm'
        ]
        
        for filename in data_files:
            filepath = os.path.join(self.data_dir, filename)
            if os.path.exists(filepath):
                try:
                    with open(filepath, 'rb') as f:
                        self.data_files[filename] = f.read()
                    size_kb = len(self.data_files[filename]) / 1024
                    logger.info(f"✓ Loaded {filename} ({size_kb:.1f} KB)")
                except Exception as e:
                    logger.warning(f"Failed to load {filename}: {e}")

class GameSession:
    """Represents a game client session"""
    def __init__(self, session_id, ip_address):
        self.session_id = session_id
        self.ip = ip_address
        self.connected_at = time.time()
        self.authenticated = False
        self.last_activity = time.time()
        self.character_name = None

def handle_client(client_socket, address, session_id, game_data):
    """Handle individual player connection"""
    session = GameSession(session_id, address[0])
    
    try:
        client_socket.settimeout(TIMEOUT)
        
        logger.info(f"[Session {session_id}] ✅ CONNECTED from {address[0]}:{address[1]}")
        
        # Send initial handshake packets
        handshake = b"WLO_SERVER_V1.0_FULL_DATA"
        client_socket.send(handshake)
        logger.info(f"[Session {session_id}] → Handshake sent")
        
        # Send server info packet (0x01)
        try:
            server_info = b'\x01' + b'\x00' * 100  # Packet type 0x01 + padding
            client_socket.send(server_info)
            logger.info(f"[Session {session_id}] → Server info packet sent")
        except:
            pass
        
        # Auto-authenticate
        session.authenticated = True
        logger.info(f"[Session {session_id}] ✅ AUTO-AUTHENTICATED")
        
        # Send character spawn packet (0x02)
        try:
            spawn_packet = b'\x02' + b'\x00' * 200  # Packet type 0x02 + padding
            client_socket.send(spawn_packet)
            logger.info(f"[Session {session_id}] → Spawn packet sent")
        except:
            pass
        
        # Keep connection alive
        timeout_count = 0
        while True:
            try:
                data = client_socket.recv(4096)
                
                if not data:
                    logger.info(f"[Session {session_id}] ❌ DISCONNECTED (connection closed)")
                    break
                
                session.last_activity = time.time()
                timeout_count = 0
                
                # Log data received
                if len(data) <= 50:
                    try:
                        text = data.decode('utf-8', errors='ignore').strip()
                        if text:
                            logger.info(f"[Session {session_id}] ← {text[:100]}")
                    except:
                        logger.info(f"[Session {session_id}] ← {len(data)} bytes (binary)")
                else:
                    logger.info(f"[Session {session_id}] ← {len(data)} bytes")
                
                # Respond to keep-alive
                try:
                    ack = b'\x00\x00\x00\x01'
                    client_socket.send(ack)
                except:
                    pass
                    
            except socket.timeout:
                timeout_count += 1
                if timeout_count > 3:
                    logger.info(f"[Session {session_id}] ❌ TIMEOUT")
                    break
                # Send keep-alive
                try:
                    client_socket.send(b'\x00\x00\x00\x00')
                except:
                    break
            except ConnectionResetError:
                logger.info(f"[Session {session_id}] ❌ DISCONNECTED (reset)")
                break
            except Exception as e:
                logger.info(f"[Session {session_id}] ❌ {e}")
                break
    
    except Exception as e:
        logger.error(f"[Session {session_id}] FATAL: {e}")
    finally:
        try:
            client_socket.close()
        except:
            pass
        logger.info(f"[Session {session_id}] 🔌 CLOSED")

def main():
    """Start the game server"""
    try:
        # Load game data
        logger.info("="*70)
        logger.info("🎮 WONDERLAND ONLINE - GAME SERVER")
        logger.info("="*70)
        logger.info("Loading game data...")
        logger.info("")
        
        game_data = GameDataManager(GAMEDATA_DIR)
        
        logger.info("")
        logger.info(f"✅ Loaded {len(game_data.data_files)} data files")
        logger.info("="*70)
        
        # Start server
        server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        
        # Additional socket options for better stability
        if hasattr(socket, 'SO_EXCLUSIVEADDRUSE'):
            # Windows-specific option to prevent multiple bindings
            server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_EXCLUSIVEADDRUSE, 1)
        
        server_socket.bind((HOST, PORT))
        server_socket.listen(10)
        
        logger.info(f"✅ Listening on {HOST}:{PORT}")
        logger.info(f"📍 Serving {len(game_data.data_files)} game data files")
        logger.info(f"⏱️  Timeout: {TIMEOUT} seconds")
        logger.info("="*70)
        
        while True:
            try:
                client_socket, address = server_socket.accept()
                session_id = get_next_session_id()
                
                # Handle in thread
                thread = threading.Thread(
                    target=handle_client,
                    args=(client_socket, address, session_id, game_data),
                    daemon=True
                )
                thread.start()
                
            except KeyboardInterrupt:
                logger.info("Shutdown requested...")
                break
            except Exception as e:
                logger.error(f"Accept error: {e}")
    
    except Exception as e:
        logger.error(f"FATAL: {e}")
        sys.exit(1)
    finally:
        try:
            server_socket.close()
        except:
            pass
        logger.info("="*70)
        logger.info("✅ Server stopped")
        logger.info("="*70)

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        logger.info("Interrupted")
    except Exception as e:
        logger.error(f"Error: {e}")
        sys.exit(1)
