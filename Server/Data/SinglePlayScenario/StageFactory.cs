namespace Server.Data.SinglePlayScenario;

public class StageFactory
{
    public interface IFactory<out T> where T : Stage
    {
        T Create();
    }
    
    private readonly Dictionary<int, IFactory<Stage>> _stageDict = new()
    {
        {1000, new StageTutorialSheepFactory()},
        {1001, new Stage1001Factory()},
        {1002, new Stage1002Factory()},
        {1003, new Stage1003Factory()},
        {1004, new Stage1004Factory()},
        {1005, new Stage1005Factory()},
        {1006, new Stage1006Factory()},
        {1007, new Stage1007Factory()},
        {1008, new Stage1008Factory()},
        {1009, new Stage1009Factory()},
        {5000, new StageTutorialWolfFactory()},
        {5001, new Stage5001Factory()},
        {5002, new Stage5002Factory()},
        {5003, new Stage5003Factory()},
        {5004, new Stage5004Factory()},
        {5005, new Stage5005Factory()},
        {5006, new Stage5006Factory()},
        {5007, new Stage5007Factory()},
        {5008, new Stage5008Factory()},
        {5009, new Stage5009Factory()},
    };
    
    public Stage Create(int id)
    {
        if (_stageDict.TryGetValue(id, out var factory))
        {
            return factory.Create();
        }

        throw new InvalidDataException();
    }
    
    private class StageTutorialSheepFactory : IFactory<StageTutorialSheep> { public StageTutorialSheep Create() => new(); }
    private class Stage1001Factory : IFactory<Stage1001> { public Stage1001 Create() => new(); }
    private class Stage1002Factory : IFactory<Stage1002> { public Stage1002 Create() => new(); }
    private class Stage1003Factory : IFactory<Stage1003> { public Stage1003 Create() => new(); }
    private class Stage1004Factory : IFactory<Stage1004> { public Stage1004 Create() => new(); }
    private class Stage1005Factory : IFactory<Stage1005> { public Stage1005 Create() => new(); }
    private class Stage1006Factory : IFactory<Stage1006> { public Stage1006 Create() => new(); }
    private class Stage1007Factory : IFactory<Stage1007> { public Stage1007 Create() => new(); }
    private class Stage1008Factory : IFactory<Stage1008> { public Stage1008 Create() => new(); }
    private class Stage1009Factory : IFactory<Stage1009> { public Stage1009 Create() => new(); }
    private class StageTutorialWolfFactory : IFactory<StageTutorialWolf> { public StageTutorialWolf Create() => new(); }
    private class Stage5001Factory : IFactory<Stage5001> { public Stage5001 Create() => new(); }
    private class Stage5002Factory : IFactory<Stage5002> { public Stage5002 Create() => new(); }
    private class Stage5003Factory : IFactory<Stage5003> { public Stage5003 Create() => new(); }
    private class Stage5004Factory : IFactory<Stage5004> { public Stage5004 Create() => new(); }
    private class Stage5005Factory : IFactory<Stage5005> { public Stage5005 Create() => new(); }
    private class Stage5006Factory : IFactory<Stage5006> { public Stage5006 Create() => new(); }
    private class Stage5007Factory : IFactory<Stage5007> { public Stage5007 Create() => new(); }
    private class Stage5008Factory : IFactory<Stage5008> { public Stage5008 Create() => new(); }
    private class Stage5009Factory : IFactory<Stage5009> { public Stage5009 Create() => new(); }
}