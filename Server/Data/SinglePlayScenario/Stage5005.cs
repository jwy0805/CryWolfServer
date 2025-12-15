using System.Numerics;
using Google.Protobuf.Protocol;
using Server.Game;

namespace Server.Data.SinglePlayScenario;

public class Stage5005 : Stage
{
    private readonly Dictionary<string, Tower> _towers = new();
    private bool _finishMove = false;
}