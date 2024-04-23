using Google.Protobuf.Protocol;

namespace Server.Game;

public class LightningStrike : Effect
{
   private bool _isHit;
   
   public override void Init()
   {
      _isHit = false;
   }

   public override void Update()
   {
      base.Update();
      if (_isHit == false && PacketReceived) SetEffectEffect();
   }

   protected override void SetEffectEffect()
   {
      if (Parent?.Target == null) return;
      _isHit = true;
      Parent.Target.OnDamaged(Parent, Parent.SkillDamage, Damage.Magical);
      base.SetEffectEffect();
   }

   public override PositionInfo SetEffectPos(GameObject parent, PositionInfo? effectPos = null)
   {
      if (parent.Target == null) return new PositionInfo { PosX = -100, PosY = -100, PosZ = -100 };
      return parent.Target.PosInfo;
   }
}