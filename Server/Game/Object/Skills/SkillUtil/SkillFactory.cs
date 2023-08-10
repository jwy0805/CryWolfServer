namespace Server.Game;

public class SkillFactory
{
    private static readonly Dictionary<string, Type?> _skillDict = new Dictionary<string, Type?>
    {
        { "BasicAttack", typeof(BasicAttack) }
    };
    
    public static Projectile CreateProjectile(string projectileType)
    {
        if (_skillDict.TryGetValue(projectileType, out var type))
            return (Projectile)Activator.CreateInstance(type);
        throw new ArgumentException("Invalid projectile type");
    }

    public static Effect CreateEffect(string effectType)
    {
        if (_skillDict.TryGetValue(effectType, out var type))
            return (Effect)Activator.CreateInstance(type);
        throw new ArgumentException("Invalid projectile type");
    }
}