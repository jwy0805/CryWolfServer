namespace Server.Game;

public interface IJobSerializer
{
    void Push(IJob job);
    void Flush();
    IJob? Pop();
}