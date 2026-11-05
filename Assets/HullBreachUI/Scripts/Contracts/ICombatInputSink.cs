using UnityEngine;

namespace HullBreach.UI.Contracts
{
    public interface ICombatInputSink
    {
        void SetMoveInput(Vector2 direction);
        void SetAttackHeld(bool held);
        bool TryAttack();
        bool TryUseSkill();
    }
}
