using Google.Protobuf.Protocol;

namespace Server.Game;

public class MosquitoPester : MosquitoBug
{
    public override void Init()
    {
        base.Init();
        MonsterId = MonsterId.MosquitoPester;
    }
}