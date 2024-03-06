using System.Numerics;
using Google.Protobuf.Protocol;

namespace Server.Data;

public class GameData
{
    // Game 초기 설정
    
    // 불변
    public static readonly float GroundHeight = 6.0f;
    public static readonly float AirHeight = 9.0f;
    public static Vector3 Center = new(0.0f, 6.0f, 0.0f); // Center of the Map
    public static readonly int[] ZCoordinatesOfMap = { 112, 84, 52, 20, 0, -20, -52, -84, -112 }; // Vector2Int, Vector3 * 4
    
    public static Vector3[] SpawnerPos { get; set; } = {
        new(0.0f, 6.0f, 30.0f), // North
        new(0.0f, 6.0f, -30.0f) // South
    };
    
    #region FenceData

    public static readonly string[] FenceName = { "", "FenceLv1", "FenceLv2", "FenceLv3" };
    public static readonly int NorthFenceMax = 8;
    public static readonly int SouthFenceMax = 8;
    public static readonly Vector3 FenceStartPos = new(-7, 6, -5);
    public static readonly Vector3 FenceCenter = new(0, 6, 0);
    public static readonly Vector3 FenceSize = new(12, 6, 10);
    
    public static readonly List<Vector3> FenceBounds = new()
    {
        new Vector3(FenceCenter.X - FenceSize.X / 2 , 6, FenceCenter.Z + FenceSize.Z / 2),
        new Vector3(FenceCenter.X - FenceSize.X / 2 , 6, FenceCenter.Z - FenceSize.Z / 2),
        new Vector3(FenceCenter.X + FenceSize.X / 2 , 6, FenceCenter.Z - FenceSize.Z / 2),
        new Vector3(FenceCenter.X + FenceSize.X / 2 , 6, FenceCenter.Z + FenceSize.Z / 2)
    };

    public static readonly List<Vector3> SheepBounds = new()
    {
        new Vector3(Center.X - 4f, Center.Y, Center.Z + 3f),
        new Vector3(Center.X - 4f, Center.Y, Center.Z - 3f),
        new Vector3(Center.X + 6f, Center.Y, Center.Z - 3f),
        new Vector3(Center.X + 6f, Center.Y, Center.Z + 3f)
    };
        
    public static Vector3[] GetPos(int cnt, int row, Vector3 startPos)
    {
        Vector3[] posArr = new Vector3[cnt];

        for (int i = 0; i < row; i++)
        {
            posArr[i] = startPos with { X = startPos.X + i * 2, Z = -5}; // south fence
            posArr[row + i] = startPos with { X = startPos.X + i * 2, Z = 5 }; // north fence
        }

        return posArr;
    }
    
    public static float[] GetRotation(int cnt, int row)
    {
        float[] rotationArr = new float[cnt];
        
        for (int i = 0; i < row; i++)
        {
            rotationArr[i] = 180;
            rotationArr[row + i] = 0;
        }

        return rotationArr;
    }

    #endregion

    // 게임 진행 정보
    #region GameInfo

    public static readonly long RoundTime = 20000;
    public static int[] StorageLvUpCost = { 0, 600, 2000 };
    
    #endregion

    public static readonly Dictionary<UnitId, Skill[]> OwnSkills = new()
    {
        
    };
    
    public static readonly Dictionary<Skill, Skill[]> SkillTree = new()
    {
        { Skill.BudAttackSpeed, new[] { Skill.NoSkill } },
        { Skill.BudRange, new[] { Skill.NoSkill } },
        
        { Skill.MosquitoStingerSheepDeath, new [] { Skill.MosquitoStingerInfection } },
    
        { Skill.HermitFireResist, new [] { Skill.NoSkill } },
    };
}