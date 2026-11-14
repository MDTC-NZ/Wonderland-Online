# Wonderland Online - Game Server v3 Protocol Documentation

## Overview
Game Server v3 implements the correct Wonderland Online network protocol based on reverse-engineering the C# server source code found in `./wlo.pserver.core/Network/`.

## Packet Format

All packets follow this structure:

```
[Header (2 bytes)] [Length (2 bytes)] [Payload (variable)]
```

### Header
- **Value**: `0x44F4` (Little-endian)
- **Purpose**: Identifies the packet as a valid WLO protocol message
- **Bytes**: `F4 44`

### Length
- **Size**: 2 bytes (Little-endian unsigned short)
- **Content**: Size of the payload ONLY (excludes header and length fields)
- **Range**: 0-65535 bytes

### Payload
- **Byte 1**: Action Code A (Command Type)
- **Byte 2**: Action Code B (Sub-command) - Optional
- **Bytes 3+**: Data (variable length)

## Packet Types

### Handshake / Login Response
```
Action A: 0x01
Action B: 0x00
Data: 0x01 (success=1, failure=0)
```

### Character/Player Info
```
Action A: 0x03
Action B: 0x00
Data: Character ID (4 bytes, little-endian)
```

### Map/World Data
```
Action A: 0x05
Action B: 0x00
Data: Map ID (2 bytes) + padding
```

### Keep-Alive Heartbeat
```
Action A: 0x00
Action B: 0x00
Data: Empty
```

### Acknowledgment
```
Action A: 0xFF
Action B: 0x00
Data: Varies
```

## Python Implementation

### Create Packet
```python
def create_packet(action_a, action_b=0, data=b''):
    # Build header (0x44F4)
    packet = struct.pack('<H', 0x44F4)
    
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
```

### Parse Packet
```python
def parse_packet(data):
    if len(data) < 4:
        return None, None, None
    
    # Check header
    header = struct.unpack('<H', data[0:2])[0]
    if header != 0x44F4:
        return None, None, None
    
    # Get length
    length = struct.unpack('<H', data[2:4])[0]
    
    # Extract payload
    if len(data) < 4 + length:
        return None, None, None
    
    payload = data[4:4+length]
    
    action_a = payload[0]
    action_b = payload[1] if len(payload) > 1 else None
    payload_data = payload[2:] if len(payload) > 2 else b''
    
    return action_a, action_b, payload_data
```

## Source Code References

Protocol information was extracted from:
- `./wlo.pserver.core/Network/Packet.cs` - Packet structure and packing
- `./wlo.pserver.core/Network/Socket.cs` - Socket interface definitions
- `./Src/Server/LoginServer.cs` - Server-side connection handling
- `./wlo.pserver.core/Network/ActionCodes/AC.cs` - Action code processing

## Handshake Flow

```
Client                          Server
  |                               |
  |-------- TCP Connect -------->  |
  |                               |
  |  <----- AC: 0x01, 0x00 -----  | (Handshake/Login Success)
  |                               |
  |  <----- AC: 0x03, 0x00 -----  | (Character Info)
  |                               |
  |  <----- AC: 0x05, 0x00 -----  | (Map/World Data)
  |                               |
  | (Client can now send packets) |
  |                               |
  | ------- AC: XXXX, XX ------->  | (Client Action)
  |                               |
  |  <----- AC: 0xFF, 0x00 -----  | (Acknowledgment)
  |                               |
  | ... (Keep-alive every 30s) ...|
  |                               |
```

## Known Limitations

The current protocol implementation is based on educated reverse-engineering:
- Not all action codes have been identified
- The exact format of complex data payloads is unknown
- Game world state synchronization protocol is incomplete
- Player-to-player communication protocol is not implemented

## Future Improvements

To fully implement the protocol, we would need:
1. Complete action code specification (there may be 100+ codes)
2. Detailed data structure definitions for each packet type
3. Game state machine specification
4. Complete network packet capture from official server

## Testing

Monitor the server logs to verify correct packet handling:
```
[Session 1] ✅ CONNECTED from 127.0.0.1:58074
[Session 1] → Sent handshake packet (AC: 0x01, 0x00)
[Session 1] → Sent character info packet (AC: 0x03, 0x00)
[Session 1] → Sent map data packet (AC: 0x05, 0x00)
[Session 1] ← Received X bytes (buffer: Y)
[Session 1] → Packet AC: 0x12, 0x34 | Payload: Z bytes
```

