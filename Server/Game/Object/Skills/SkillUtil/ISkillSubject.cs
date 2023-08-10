namespace Server.Game;

public interface ISkillSubject
{
    void AddObserver(ISkillObserver observer);
    void RemoveObserver(ISkillObserver observer);
    void Notify();
}