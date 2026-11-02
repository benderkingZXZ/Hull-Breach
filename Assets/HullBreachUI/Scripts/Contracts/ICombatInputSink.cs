using UnityEngine;

namespace HullBreach.UI.Contracts
{
    /// <summary>Куда экранные элементы управления отправляют ввод. Реализация: Bridge/CombatAdapter.</summary>
    public interface ICombatInputSink
    {
        /// <summary>Вектор стика, длина ≤ 1.</summary>
        void SetMoveInput(Vector2 direction);
        void SetAttackHeld(bool held);
        bool TryAttack();
        bool TryUseSkill();
    }
}
