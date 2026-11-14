#!/usr/bin/env python3
"""
Wonderland Online Game Launcher
Handles launching the game client with character authentication
"""

import subprocess
import sys
import os
import json
import time
from pathlib import Path

# Get the directory where this script is located
SCRIPT_DIR = Path(__file__).resolve().parent

# Game client path
GAME_CLIENT_PATH = r"C:\Program Files (x86)\Wonderland Online\Wonderland.exe"

def launch_game(character_name, character_id, user_token, server_domain="localhost", server_port=6414):
    """
    Launch the Wonderland game client with character information
    
    Args:
        character_name: Name of the character to play
        character_id: ID of the character
        user_token: Authentication token from the registration server
        server_domain: Server domain (default: localhost)
        server_port: Server port (default: 6414)
    """
    
    # Check if game client exists
    if not os.path.exists(GAME_CLIENT_PATH):
        print(f"Error: Game client not found at {GAME_CLIENT_PATH}")
        print(f"Please install Wonderland Online at: C:\\Program Files (x86)\\Wonderland Online")
        return False
    
    # Create a temporary auth file that the game can read
    auth_data = {
        "character_name": character_name,
        "character_id": character_id,
        "user_token": user_token,
        "server": {
            "domain": server_domain,
            "port": server_port
        },
        "timestamp": time.time()
    }
    
    auth_file = os.path.join(SCRIPT_DIR, "auth.json")
    try:
        with open(auth_file, 'w', encoding='utf-8') as f:
            json.dump(auth_data, f, indent=2)
        print(f"[*] Auth file created: {auth_file}")
    except Exception as e:
        print(f"Error writing auth file: {e}")
        return False
    
    try:
        # Launch the game client
        game_dir = os.path.dirname(GAME_CLIENT_PATH)
        
        # Pass server info as command-line arguments
        args = [
            GAME_CLIENT_PATH,
            f"--server={server_domain}",
            f"--port={server_port}",
            f"--character={character_name}",
            f"--token={user_token}"
        ]
        
        process = subprocess.Popen(
            args,
            cwd=game_dir,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE
        )
        
        print(f"[✓] Game launched with PID: {process.pid}")
        print(f"[*] Character: {character_name}")
        print(f"[*] Server: {server_domain}:{server_port}")
        print(f"[*] Token: {user_token[:20]}...")
        
        return True
        
    except Exception as e:
        print(f"Error launching game: {e}")
        return False

if __name__ == "__main__":
    if len(sys.argv) < 4:
        print("Usage: python game_launcher.py <character_name> <character_id> <user_token> [server_domain] [server_port]")
        print("\nExample:")
        print("  python game_launcher.py 'MyCharacter' '12345' 'token_1_123456789'")
        print("  python game_launcher.py 'MyCharacter' '12345' 'token_1_123456789' 'wlo.kiaoragaming.xyz' '5423'")
        sys.exit(1)
    
    character_name = sys.argv[1]
    character_id = sys.argv[2]
    user_token = sys.argv[3]
    server_domain = sys.argv[4] if len(sys.argv) > 4 else "localhost"
    server_port = int(sys.argv[5]) if len(sys.argv) > 5 else 6414
    
    success = launch_game(character_name, character_id, user_token, server_domain, server_port)
    sys.exit(0 if success else 1)

