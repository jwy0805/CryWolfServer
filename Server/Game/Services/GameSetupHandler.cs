using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public class GameSetupHandler : IGameSetupHandler
{
    private readonly INetworkFactory _networkFactory = new NetworkFactory();
    
    public Task StartRankGame(MatchSuccessPacketRequired packet, DateTime? startTime = null)
    {
        startTime ??= DateTime.UtcNow;
        GameLogic.Instance.Push(() =>
        {
            var room = GameLogic.Instance.CreateGameRoom(packet.MapId);
            if (packet.IsTestGame)
            {
                SetupTestGame(room, packet);
                SendMatchInfo(packet);
            }
            else if (packet.IsAiSimulation)
            {
                SetupAiSimulation(room, packet);
            }
            else
            {
                SetupRankGameOrRetry(room, packet, startTime.Value);
                SendMatchInfo(packet);
            }
            
            GameLogic.Instance.PushAfter(6000, () =>
            {
                room.RoomActivated = true;
                Console.WriteLine("RoomActivated = true (after 6s)");
            });
        });
        
        return Task.CompletedTask;
    }

    public Task StartFriendlyGame(FriendlyMatchPacketRequired packet, DateTime? startTime = null)
    {
        startTime ??= DateTime.UtcNow;
        GameLogic.Instance.Push(() =>
        {
            var room = GameLogic.Instance.CreateGameRoom(packet.MapId);
            
            SetupFriendlyGameOrRetry(room, packet, startTime.Value);
            SendMatchInfo(packet);

            GameLogic.Instance.PushAfter(6000, () =>
            {
                room.RoomActivated = true;
                Console.WriteLine("RoomActivated = true (after 6s)");
            });
        });

        return Task.CompletedTask;
    }
    
    public async Task<bool> StartSingleGameAsync(SinglePlayStartPacketRequired packet)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        GameLogic.Instance.Push(() =>
        {
            var room = GameLogic.Instance.CreateGameRoom(packet.MapId);
            var player = _networkFactory.CreatePlayerSingle(room, packet);
            _networkFactory.CreateNpc(room, player, (CharacterId)packet.EnemyCharacterId, packet.EnemyAssetId, packet.EnemyUnitIds);
            room.GameMode = GameMode.Single;
            room.StageId = packet.StageId;
            room.RoomActivated = true;
            tcs.SetResult(true);
        });
        
        return await tcs.Task;
    }

    public async Task<bool> StartTutorialAsync(TutorialStartPacketRequired packet)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        GameLogic.Instance.Push(() =>
        {
            var room = GameLogic.Instance.CreateGameRoom(packet.MapId);
            var player = _networkFactory.CreatePlayerTutorial(room, packet);
            _networkFactory.CreateNpc(room, player, (CharacterId)packet.EnemyCharacterId, packet.EnemyAssetId);
            room.GameMode = GameMode.Tutorial;
            room.RoomActivated = true;
            tcs.SetResult(true);
        });

        return await tcs.Task;
    }

    private void SendStartGamePacket(Player sheepPlayer, Player wolfPlayer, MatchSuccessPacketRequired packet)
    {
        var (matchPacketForSheep, matchPacketForWolf) = MakeMatchPacket(packet);
        sheepPlayer.Session?.Send(matchPacketForSheep);
        wolfPlayer.Session?.Send(matchPacketForWolf);
    }

    private Tuple<S_MatchMakingSuccess, S_MatchMakingSuccess> MakeMatchPacket(MatchSuccessPacketRequired packet)
    {
        var matchPacketForSheep = new S_MatchMakingSuccess
        {
            EnemyUserName = packet.WolfUserName,
            EnemyRankPoint = packet.WolfRankPoint,
            EnemyCharacterId = (int)packet.WolfCharacterId,
            EnemyAssetId = (int)packet.EnchantId,
        };

        foreach (var unitId in packet.WolfUnitIds)
        {
            matchPacketForSheep.EnemyUnitIds.Add((int)unitId);
        }
        
        foreach (var achievement in packet.WolfAchievements)
        {
            matchPacketForSheep.EnemyAchievements.Add(achievement);
        }
        
        var matchPacketForWolf = new S_MatchMakingSuccess
        {
            EnemyUserName = packet.SheepUserName,
            EnemyRankPoint = packet.SheepRankPoint,
            EnemyCharacterId = (int)packet.SheepCharacterId,
            EnemyAssetId = (int)packet.SheepId,
        };
        
        foreach (var unitId in packet.SheepUnitIds)
        {
            matchPacketForWolf.EnemyUnitIds.Add((int)unitId);
        }
        
        foreach (var achievement in packet.SheepAchievements)
        {
            matchPacketForWolf.EnemyAchievements.Add(achievement);
        }

        return new Tuple<S_MatchMakingSuccess, S_MatchMakingSuccess>(matchPacketForSheep, matchPacketForWolf);
    }
    
    public async Task<bool> SurrenderGameAsync(GameResultPacketRequired packet)
    {
        var tcs = new TaskCompletionSource<bool>();
        GameLogic.Instance.Push(() =>
        {
            var room = GameLogic.Instance.FindByUserId(packet.UserId);
            if (room == null)
            {
                Console.WriteLine($"Room not found.");
                tcs.SetResult(false);
            }
            else
            {
                var winnerId = room.FindPlayer(go =>
                    go is Player player && player.Session?.UserId != packet.UserId)?.Session?.UserId ?? -1;
                _ = room.GameOver(winnerId, packet.UserId);
                tcs.SetResult(true);
            }
        });

        return await tcs.Task;
    }

    private void SetupRankGameOrRetry(GameRoom room, MatchSuccessPacketRequired packet, DateTime startTime)
    {
        var sheepPlayer = room.FindPlayer(go => go is Player { Faction: Faction.Sheep }) ?? 
                          _networkFactory.CreatePlayer(room, packet, Faction.Sheep);
        var wolfPlayer = room.FindPlayer(go => go is Player { Faction: Faction.Wolf }) ?? 
                         _networkFactory.CreatePlayer(room, packet, Faction.Wolf);

        if (sheepPlayer.Session == null || wolfPlayer.Session == null)
        {
            if ((DateTime.UtcNow - startTime).TotalMilliseconds > 5000)
            {
                Console.WriteLine("Session timeout.");
                return;
            }

            Console.WriteLine("Session is not ready yet.");
            GameLogic.Instance.PushAfter(400, () => SetupRankGameOrRetry(room, packet, startTime));
            return;
        }

        room.GameMode = GameMode.Rank;
        SendStartGamePacket(sheepPlayer, wolfPlayer, packet);
    }
    
    private void SetupFriendlyGameOrRetry(GameRoom room, FriendlyMatchPacketRequired packet, DateTime startTime)
    {
        var sheepPlayer = room.FindPlayer(go => go is Player { Faction: Faction.Sheep }) ?? 
                          _networkFactory.CreatePlayerFriendly(room, packet, Faction.Sheep);
        var wolfPlayer = room.FindPlayer(go => go is Player { Faction: Faction.Wolf }) ?? 
                         _networkFactory.CreatePlayerFriendly(room, packet, Faction.Wolf);
            
        if (sheepPlayer.Session == null || wolfPlayer.Session == null)
        {
            if ((DateTime.UtcNow - startTime).TotalMilliseconds > 5000)
            {
                Console.WriteLine("Session timeout.");
                return;
            }
        
            Console.WriteLine("Session is not ready yet.");
            GameLogic.Instance.PushAfter(400, () => SetupFriendlyGameOrRetry(room, packet, startTime));
            return;
        }
            
        room.GameMode = GameMode.Friendly;
    }
    
    private void SetupTestGame(GameRoom room, MatchSuccessPacketRequired packet)
    {
        var faction = packet.SheepUserName == "Test" ? Faction.Wolf : Faction.Sheep;
        var player = _networkFactory.CreatePlayer(room, packet, faction);
        var npcCharacterId = faction == Faction.Sheep ? packet.WolfCharacterId : packet.SheepCharacterId;
        var npcAssetId = faction == Faction.Sheep ? (int)packet.EnchantId : (int)packet.SheepId;
        var npc = _networkFactory.CreateNpc(room, player, npcCharacterId, npcAssetId);
        var matchPacket = new S_MatchMakingSuccess
        {
            EnemyUserName = npc.Info.Name,
            EnemyRankPoint = packet.SheepRankPoint,
            EnemyCharacterId = (int)packet.SheepCharacterId,
            EnemyAssetId = player.Faction == Faction.Sheep ? (int)packet.EnchantId : (int)packet.SheepId,
        };
                
        room.GameMode = GameMode.Test;
        
        foreach (var unitId in packet.SheepUnitIds)
        {
            matchPacket.EnemyUnitIds.Add((int)unitId);
        }

        player.Session?.Send(matchPacket);
    }
    
    // API 서버로 매치 정보 전송하긴 하는데 그렇게 중요한 로직은 아님 - 단순 상태 저장
    private void SendMatchInfo(MatchSuccessPacketRequired packet)
    {
        _ = SendMatchInfo(packet.SheepUserId, packet.SheepSessionId, packet.WolfUserId, packet.WolfSessionId);
    }
    
    private void SendMatchInfo(FriendlyMatchPacketRequired packet)
    {
        _ = SendMatchInfo(packet.SheepUserId, packet.SheepSessionId, packet.WolfUserId, packet.WolfSessionId);
    }

    private async Task SendMatchInfo(int sheepUserId, int sheepSessionId, int wolfUserId, int wolfSessionId)
    {
        var sendPacket = new SendMatchInfoPacketRequired
        {
            SheepUserId = sheepUserId,
            SheepSessionId = sheepSessionId,
            WolfUserId = wolfUserId,
            WolfSessionId = wolfSessionId,
        };
        
        try
        {
            await NetworkManager.Instance.SendRequestToApiAsync<SendMatchInfoPacketResponse>(
                "Match/SetMatchInfo", sendPacket, HttpMethod.Post);
        }
        catch (Exception e)
        {
            Console.WriteLine($"SetMatchInfo failed: {e}");
        }
    }
    
    private void SetupAiSimulation(GameRoom room, MatchSuccessPacketRequired packet)
    {
        _networkFactory.CreateNpcForAiGame(
            room, Faction.Sheep, packet.SheepSessionId, packet.SheepCharacterId, (int)packet.SheepId);
        _networkFactory.CreateNpcForAiGame(
            room, Faction.Wolf, packet.WolfSessionId, packet.WolfCharacterId, (int)packet.EnchantId);
        room.GameMode = GameMode.AiSimulation;
    }
}