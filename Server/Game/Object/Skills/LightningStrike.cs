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
      if (_isHit) return;
      _isHit = true;
      Parent?.Target?.OnDamaged(Parent, Parent.SkillDamage);
      Room?.LeaveGame(Id);
   }
}