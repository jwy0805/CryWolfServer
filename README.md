# CryWolfServer - Authoritative Real-Time TCP Game Server for the Live Mobile PvP Game "Cry Wolf"
production socket server that runs authoritative 1v1 PvP matches (room-per-match), processes packet-driven gameplay events,
and keeps game state consistent under concurrency.

## Proof (Live)
- iOS (App Store): https://apps.apple.com/kr/app/id6745862935
- Android (Google Play): https://play.google.com/store/apps/details?id=com.hamonstudio.crywolf&hl=ko

For a full product write-up,
- see the resume: https://www.notion.so/WooYoung-Jeong-2f42f5b151de80d39cd2ea9900bfe6f3?source=copy_link
- see the portfolio: https://www.notion.so/Cry-Wolf-Portfolio-2f52f5b151de80529d24c00c87a685fa?source=copy_link

## HighLights
- **Actor-model concurrency**: rooms are processed in parallel, while each room handles state changes sequentially to prevent race conditions and process traffic efficiently.
- **Authoritative gameplay**: clients send intents; the server owns the truth (never trust the client)
- **Room-per-match lifecycle**: rooms are created/released dynamically to isolate failures and keep runtime predictable.
- **Packet-driven networking**: clear boundaries between transport, packet parsing, and gameplay logic.
- **Built-in load/testing toolchain**: dummy clients + packet generator + server tests to validate behaviour and performance.

## Start Here (Review Guide)
If you only have 5 minutes, follow these entry points
- **Actor-model concurrency**: `Server/Game/Scheduler/RoomActorScheduler.cs` see how the per-room job queue and scheduling ensures sequential state mutation and thread safety while allowing parallel processing across rooms
- **Room life cycle**: `Server/Game/Services/GameSetupHandler.cs` + `Server/Game/Room/GameRoom.cs`
- **Match making by session**: `Server/Managers/NetworkManager.cs` locate where the match making starts and sessions are created
- **Simulation tests**: `CryWolfServerTset` see how load/scenario tests drive the server
- **State serialization**: `Server/Game/Job` find the per-room queue / job scheduling that ensures sequential state mutation and thread safety

## Repository Map
- `Server/` - gameplay server: all game logics include room lifecycle, session handling, authoritative match simulation
- `Server/Game/Scheduler/` - per-room job queue and scheduling to ensure single-threaded state mutation
- `ServerCore/` - TCP transport abstraction + packet encode/decode foundation
- `PacketGenerator/` - tooling for generating packet definitions used by client/server
- `CryWolfServerTest/` - automated tests / simulation harness for stability and load checks

## Security & Configuration
This repository does not publish runnable production configuration or secrets.
Production deployment injects environment-specific settings securely.

## Contact
- Email: hamonstd@gmail.com
