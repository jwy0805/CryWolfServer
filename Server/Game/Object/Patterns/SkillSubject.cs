using Google.Protobuf.Protocol;

namespace Server.Game;

public class SkillSubject : ISkillSubject
{
    private readonly List<ISkillObserver> _observers = new();
    private Skill _skill;
    
    public void AddObserver(ISkillObserver observer)
    {
        _observers.Add(observer);
    }

    public void RemoveObserver(ISkillObserver observer)
    {
        if (_observers.IndexOf(observer) > 0) _observers.Remove(observer);
    }

    public void Notify()
    {
        foreach (var observer in _observers) observer.OnSkillUpgrade(_skill);
    }

    public void SkillUpgraded(Skill skill)
    {
        _skill = skill;
        Notify();
    }
}