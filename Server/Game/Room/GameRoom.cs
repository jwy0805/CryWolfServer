using System.Numerics;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Resources;
using Server.Util;

namespace Server.Game;

public partial class GameRoom : JobSerializer
{
    private readonly object _lock = new();
    public int RoomId { get; set; }

    private Dictionary<int, Player> _players = new();
    private Dictionary<int, Tower> _towers = new();
    private Dictionary<int, Monster> _monsters = new();
    private Dictionary<int, Sheep> _sheeps = new();
    private Dictionary<int, Fence> _fences = new();
    private Dictionary<int, Effect> _effects = new();
    private Dictionary<int, Projectile> _projectiles = new();

    public Map Map { get; private set; } = new();

    private int _storageLevel = 0;

    public void Init(int mapId)
    {
        Map.LoadMap(mapId);
        Map.MapSetting();
        GameInit();
    }

    public void Update()
    {
        Flush();
    }

    public void EnterGame(GameObject gameObject)
    {
        GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);
        
        switch (type)
        {
            case GameObjectType.Player:
                Player player = (Player)gameObject;
                _players.Add(gameObject.Id, player);
                player.Room = this;
                player.Init();

                // 본인에게 정보 전송
            {
                S_EnterGame enterPacket = new S_EnterGame { Player = player.Info };
                player.Session.Send(enterPacket);

                S_Spawn spawnPacket = new S_Spawn();
                foreach (var p in _players.Values)
                {
                    if (player != p) spawnPacket.Objects.Add(p.Info);
                }
                foreach (var f in _fences.Values) spawnPacket.Objects.Add(f.Info);
                foreach (var m in _monsters.Values) spawnPacket.Objects.Add(m.Info);
                foreach (var t in _towers.Values) spawnPacket.Objects.Add(t.Info);
                foreach (var e in _effects.Values) spawnPacket.Objects.Add(e.Info);
                
                player.Session.Send(spawnPacket);
            }
                break;
            
            case GameObjectType.Tower:
                Tower tower = (Tower)gameObject;
                gameObject.Info.Name = Enum.Parse(typeof(TowerId), tower.TowerNum.ToString()).ToString();
                gameObject.PosInfo.State = State.Idle;
                gameObject.Info.PosInfo = gameObject.PosInfo;
                tower.Info = gameObject.Info;
                _towers.Add(gameObject.Id, tower);
                Map.ApplyMap(tower);
                tower.Update();
                break;
            
            case GameObjectType.Monster:
                Monster monster = (Monster)gameObject;
                gameObject.Info.Name = Enum.Parse(typeof(MonsterId), monster.MonsterNum.ToString()).ToString();
                gameObject.PosInfo.State = State.Idle;
                monster.Info = gameObject.Info;
                _monsters.Add(gameObject.Id, monster);
                Map.ApplyMap(monster);
                monster.Update();
                break;
            
            case GameObjectType.Fence:
                Fence fence = (Fence)gameObject;
                fence.Info = gameObject.Info;
                _fences.Add(gameObject.Id, fence);
                fence.Room = this;
                Map.ApplyMap(fence);
                break;
            
            case GameObjectType.Sheep:
                Sheep sheep = (Sheep)gameObject;
                gameObject.PosInfo.State = State.Idle;
                gameObject.Info.Name = "Sheep";
                sheep.Info = gameObject.Info;
                _sheeps.Add(gameObject.Id, sheep);
                sheep.Room = this;
                Map.ApplyMap(sheep);
                sheep.Update();
                break;
            
            case GameObjectType.Effect:
                Effect effect = (Effect)gameObject;
                _effects.Add(gameObject.Id, effect);
                effect.Room = this;
                effect.Update();
                break;
            
            case GameObjectType.Projectile:
                Projectile projectile = (Projectile)gameObject;
                _projectiles.Add(projectile.Id, projectile);
                projectile.Room = this;
                projectile.Update();
                break;
            
            case GameObjectType.Resource:
                Resource resource = (Resource)gameObject;
                resource.Info.Name = Enum.Parse(typeof(ResourceId), resource.ResourceNum.ToString()).ToString();
                resource.Room = this;
                resource.Player = gameObject.Player;
                resource.Info = gameObject.Info;
                resource.Init();
                resource.Update();
                break;
        }
        // 타인에게 정보 전송
        {
            S_Spawn spawnPacket = new S_Spawn();
            spawnPacket.Objects.Add(gameObject.Info);
            foreach (var player in _players.Values)
            {
                if (player.Id != gameObject.Id) player.Session.Send(spawnPacket);
            }
        }
    }

    public void EnterGame_Parent(GameObject gameObject, GameObject parent)
    {
        GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

        switch (type)
        {
            case GameObjectType.Effect:
                Effect effect = (Effect)gameObject;
                _effects.Add(gameObject.Id, effect);
                effect.Parent = parent;
                effect.Room = this;
                effect.Update();
                break;
        }

        S_SpawnParent spawnPacket = new S_SpawnParent { Object = gameObject.Info, ParentId = parent.Id };
        foreach (var player in _players.Values.Where(player => player.Id != gameObject.Id))
        {
            player.Session.Send(spawnPacket);
        }
    }

    public void LeaveGame(int objectId)
    {
        GameObjectType type = ObjectManager.GetObjectTypeById(objectId);

        switch (type)   
        {
            case GameObjectType.Player:
                if (_players.Remove(objectId, out var player) == false) return;
                player.OnLeaveGame();
                Map.ApplyLeave(player);
                player.Room = null;

                S_LeaveGame leavePacket = new S_LeaveGame();
                player.Session.Send(leavePacket);
                break;
            
            case GameObjectType.Monster:
                if (_monsters.Remove(objectId, out var monster) == false) return;
                Map.ApplyLeave(monster);
                monster.Room = null;
                break;
            
            case GameObjectType.Tower:
                if (_towers.Remove(objectId, out var tower) == false) return;
                Map.ApplyLeave(tower);
                tower.Room = null;
                break;
            
            case GameObjectType.Fence:
                if (_fences.Remove(objectId, out var fence) == false) return;
                Map.ApplyLeave(fence);
                fence.Room = null;
                break;
            
            case GameObjectType.Sheep:
                if (_sheeps.Remove(objectId, out var sheep) == false) return;
                Map.ApplyLeave(sheep);
                sheep.Room = null;
                break;
            
            case GameObjectType.Projectile:
                if (_projectiles.Remove(objectId, out var projectile) == false) return;
                projectile.Room = null;
                break;
        }

        S_Despawn despawnPacket = new S_Despawn();
        despawnPacket.ObjectIds.Add(objectId);
        foreach (var player in _players.Values)
        {
            if (player.Id != objectId) player.Session.Send(despawnPacket);
        }
    }

    public Player? FindPlayer(Func<GameObject, bool> condition)
    {
        foreach (var player in _players.Values)
        {
            if (condition.Invoke(player)) return player;
        }

        return null;
    }
    
    public void Broadcast(IMessage packet)
    {
        lock (_lock)
        {
            foreach (var p in _players.Values)
            {
                p.Session.Send(packet);
            }
        }
    }
}