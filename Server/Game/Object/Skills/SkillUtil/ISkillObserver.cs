using Google.Protobuf.Protocol;

namespace Server.Game;

public interface ISkillObserver
{
    void OnSkillUpgrade(Skill skill);
}