#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Wonderland Online - Game Server v3
Proper protocol handling based on reverse-engineered C# server source
Protocol: Header (0x44F4) + Length (2 bytes) + Data
"""

import socket
import threading
import sys
import time
import logging
import os
import struct

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
PACKET_HEADER = 0x44F4

# Game data directory
GAMEDATA_DIR = os.path.join(os.path.dirname(__file__), 'gamedata')

def create_packet(action_a, action_b=0, data=b''):
    """
    Create a proper Wonderland protocol packet
    Format: [0x44F4 (header)] [length (2 bytes)] [action_a] [action_b?] [data...]
    """
    # Start with header
    packet = struct.pack('<H', PACKET_HEADER)  # 0x44F4
    
    # Build payload
    payload = struct.pack('<B', action_a)
    if action_b > 0:
        payload += struct.pack('<B', action_b)
    if data:
        payload += data
    
    # Add length (size of payload)
    packet += struct.pack('<H', len(payload))
    packet += payload
    
    return packet

def parse_packet(data):
    """Parse an incoming packet and extract action codes"""
    if len(data) < 4:
        return None, None, None
    
    # Check header
    header = struct.unpack('<H', data[0:2])[0]
    if header != PACKET_HEADER:
        logger.warning(f"Invalid packet header: 0x{header:04X} (expected 0x{PACKET_HEADER:04X})")
        return None, None, None
    
    # Get length
    length = struct.unpack('<H', data[2:4])[0]
    
    # Extract payload
    if len(data) < 4 + length:
        logger.warning(f"Incomplete packet: expected {4 + length} bytes, got {len(data)}")
        return None, None, None
    
    payload = data[4:4+length]
    
    if len(payload) >= 1:
        action_a = payload[0]
        action_b = payload[1] if len(payload) > 1 else None
        payload_data = payload[2:] if len(payload) > 2 else b''
        return action_a, action_b, payload_data
    
    return None, None, None

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

def handle_client(client_socket, address, session_id, game_data):
    """Handle a game client connection with proper protocol"""
    try:
        client_socket.settimeout(30)
        
        logger.info(f"[Session {session_id}] ✅ CONNECTED from {address[0]}:{address[1]}")
        
        # Send initial handshake packet
        # Action code: 0x01, 0x00 (login response/handshake)
        handshake_pkt = create_packet(0x01, 0x00, b'\x01')  # Success = 1
        client_socket.send(handshake_pkt)
        logger.info(f"[Session {session_id}] → Sent handshake packet (AC: 0x01, 0x00)")
        
        time.sleep(0.1)
        
        # Send character info packet
        # Action code: 0x03 (character/player info)
        char_data = struct.pack('<I', session_id)  # Character ID
        char_pkt = create_packet(0x03, 0x00, char_data)
        client_socket.send(char_pkt)
        logger.info(f"[Session {session_id}] → Sent character info packet (AC: 0x03, 0x00)")
        
        time.sleep(0.1)
        
        # Send map/world data packet
        # Action code: 0x05 (map/world data)
        map_data = struct.pack('<H', 60000) + b'\x00' * 32  # Map ID + padding
        map_pkt = create_packet(0x05, 0x00, map_data)
        client_socket.send(map_pkt)
        logger.info(f"[Session {session_id}] → Sent map data packet (AC: 0x05, 0x00)")
        
        # Main loop - handle client packets
        last_keep_alive = time.time()
        recv_buffer = b''
        
        while True:
            try:
                # Receive data from client
                data = client_socket.recv(4096)
                
                if not data:
                    logger.info(f"[Session {session_id}] ❌ Client disconnected")
                    break
                
                recv_buffer += data
                logger.info(f"[Session {session_id}] ← Received {len(data)} bytes (buffer: {len(recv_buffer)})")
                
                # Try to parse packets from buffer
                while len(recv_buffer) >= 4:
                    action_a, action_b, payload = parse_packet(recv_buffer)
                    
                    if action_a is None:
                        break
                    
                    # Calculate packet size
                    pkt_len = 4 + struct.unpack('<H', recv_buffer[2:4])[0]
                    recv_buffer = recv_buffer[pkt_len:]
                    
                    logger.info(f"[Session {session_id}] → Packet AC: 0x{action_a:02X}, 0x{action_b:02X if action_b else 0:02X} | Payload: {len(payload)} bytes")
                    
                    # Send acknowledgment
                    try:
                        ack_pkt = create_packet(0xFF, 0x00, b'\x00')
                        client_socket.send(ack_pkt)
                    except:
                        pass
                
            except socket.timeout:
                # Send keep-alive every 30 seconds
                current_time = time.time()
                if current_time - last_keep_alive > 30:
                    try:
                        keep_alive = create_packet(0x00, 0x00, b'')
                        client_socket.send(keep_alive)
                        logger.info(f"[Session {session_id}] → Keep-alive sent")
                        last_keep_alive = current_time
                    except:
                        logger.info(f"[Session {session_id}] ❌ Keep-alive failed")
                        break
            except ConnectionResetError:
                logger.info(f"[Session {session_id}] ❌ Connection reset by client")
                break
            except Exception as e:
                logger.warning(f"[Session {session_id}] ⚠️  Error: {e}")
                break
    
    except Exception as e:
        logger.error(f"[Session {session_id}] FATAL: {e}")
    finally:
        try:
            client_socket.close()
        except:
            pass
        logger.info(f"[Session {session_id}] 🔌 Connection closed")

def main():
    """Start the game server"""
    try:
        # Load game data
        logger.info("="*70)
        logger.info("🎮 WONDERLAND ONLINE - GAME SERVER v3")
        logger.info("="*70)
        logger.info("Loading game data...")
        logger.info("")
        
        game_data = GameDataManager(GAMEDATA_DIR)
        
        logger.info("")
        logger.info(f"✅ Loaded {len(game_data.data_files)} data files")
        logger.info("="*70)
        
        # Create and bind server socket
        server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        
        # Windows-specific socket options
        if hasattr(socket, 'SO_EXCLUSIVEADDRUSE'):
            try:
                server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_EXCLUSIVEADDRUSE, 1)
            except:
                pass
        
        # Bind and listen
        server_socket.bind((HOST, PORT))
        server_socket.listen(40)
        
        logger.info(f"✅ Listening on {HOST}:{PORT}")
        logger.info(f"📍 Serving {len(game_data.data_files)} game data files")
        logger.info(f"🔧 Protocol: WLO (0x44F4)")
        logger.info(f"⏱️  Timeout: {TIMEOUT} seconds")
        logger.info("="*70)
        logger.info("Waiting for connections...")
        logger.info("="*70)
        
        # Accept client connections
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
                logger.info("")
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
