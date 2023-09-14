using Google.Protobuf.Protocol;

namespace Server.Game;

public class MosquitoBug : Monster
{
    public override void Init()
    {
        base.Init();
        MonsterId = MonsterId.MosquitoBug;
    }
}