using Google.Protobuf.Protocol;

namespace Server.Game;

public class LightningStrike : Effect
{
   public override void Update()
   {
      Parent?.Target?.OnDamaged(Parent, Parent.SkillDamage, Damage.Magical);
      Room?.LeaveGameOnlyServer(Id);
   }

   protected override void SetEffectEffect()
   {
      if (Parent?.Target == null) return;
      Parent.Target.OnDamaged(Parent, Parent.SkillDamage, Damage.Magical);
      base.SetEffectEffect();
   }
}