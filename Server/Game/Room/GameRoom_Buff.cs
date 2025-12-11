namespace Server.Game;

public partial class GameRoom
{
    public void AddBuff(BuffId buffId, BuffParamType paramType,
        GameObject master, Creature caster, float param, long duration = 10000, bool nested = false)
    {
        if (!BuffManager.Instance.BuffDict.TryGetValue(buffId, out var factory)) return;

        var buff = factory.CreateBuff();
        buff.Init(paramType, this, master, caster, param, duration, nested);
        
        if (master.Invincible && buff.Type == BuffType.Debuff) return;
        if (buff.Nested == false && master.Buffs.Contains(buff.Id))
        {
            Buff? b = Buffs.FirstOrDefault(b => b.Caster == caster && b.Id == buffId);
            b?.RenewBuff(duration);
        }
        else
        {
            master.AddBuff(buff);
        }
    }
    
    public void RemoveBuff(BuffId buffId, Creature master)
    {
        var removeBuff = (from buff in Buffs 
            where buff.Master.Id == master.Id && buff.Id == buffId select buff).FirstOrDefault();
        if (removeBuff == null) return;
        master.Buffs.Remove(removeBuff.Id);
        removeBuff.RemoveBuff();
    }

    public void RemoveNestedBuff(BuffId buffId, Creature master)
    {
        var removeBuff = (from buff in Buffs 
            where buff.Master.Id == master.Id && buff.Id == buffId select buff).ToList();
        if (removeBuff.Count == 0) return;
        foreach (var buff in removeBuff)
        {
            master.Buffs.Remove(buff.Id);
            buff.RemoveBuff();
        }
    }

    public void RemoveAllBuffs(Creature master)
    {
        List<Buff> removeBuff = (from buff in Buffs where buff.Master.Id == master.Id select buff).ToList();
        master.Buffs.Clear();
        
        if (removeBuff.Count != 0)
        {
            foreach (var buff in removeBuff)
            {
                buff.RemoveBuff();
                Buffs.Remove(buff);
            }
        }
    }

    public void RemoveAllDebuffs(Creature master)
    {
        List<Buff> removeDebuff = (from buff in Buffs 
            where buff.Master.Id == master.Id && buff.Type == BuffType.Debuff select buff).ToList();

        if (removeDebuff.Count != 0)
        {
            foreach (var debuff in removeDebuff)
            {
                debuff.RemoveBuff();
                master.Buffs.Remove(debuff.Id);
                Buffs.Remove(debuff);
            }
        }
    }
    
    private void UpdateBuffs()
    {
        if (Buffs.Count == 0) return;
        
        List<Buff> expiredBuff = Buffs.Where(buff => buff.UpdateBuff(Stopwatch.ElapsedMilliseconds)).ToList();
        if (expiredBuff.Count != 0)
        {
            foreach (var buff in expiredBuff)
            {
                buff.RemoveBuff();
                Buffs.Remove(buff);
            }
        }
    }
}