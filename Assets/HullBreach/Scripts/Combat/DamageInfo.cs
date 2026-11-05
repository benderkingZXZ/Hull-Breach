using UnityEngine;

namespace HullBreach.Combat
{
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly GameObject Source;
        public readonly Vector2 HitDirection;
        public readonly bool IgnoreInvulnerability;

        public DamageInfo(float amount, GameObject source, Vector2 hitDirection, bool ignoreInvulnerability = false)
        {
            Amount = amount;
            Source = source;
            HitDirection = hitDirection;
            IgnoreInvulnerability = ignoreInvulnerability;
        }
    }
}
