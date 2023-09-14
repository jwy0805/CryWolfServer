using Google.Protobuf.Protocol;

namespace Server.Game;

public class MosquitoStinger : MosquitoPester
{
    public override void Init()
    {
        base.Init();
        MonsterId = MonsterId.MosquitoStinger;
    }
}