using Twisted3v3.Core;
using Twisted3v3.Stats;

namespace Twisted3v3.Combat.StatusEffects
{
    /// <summary>Ralentissement : modificateur négatif sur la vitesse de déplacement.</summary>
    public sealed class SlowEffect : StatusEffect
    {
        private readonly float _amount; // 0..1
        private StatModifier _modifier;

        public SlowEffect(float amount, float duration)
        {
            _amount = amount;
            Duration = duration;
            CCType = CrowdControlType.Slow;
        }

        protected override void OnApply()
        {
            _modifier = new StatModifier(-_amount, ModifierType.PercentAdditive, this);
            Target.Stats.AddModifier(StatType.MoveSpeed, _modifier);
        }

        public override void OnExpire() =>
            Target.Stats.Get(StatType.MoveSpeed).RemoveModifier(_modifier);
    }

    /// <summary>Étourdissement : empêche déplacement, attaque et sorts.</summary>
    public sealed class StunEffect : StatusEffect
    {
        public StunEffect(float duration)
        {
            Duration = duration;
            CCType = CrowdControlType.Stun;
        }

        protected override void OnApply() => Target.IsStunned = true;
        public override void OnExpire() => Target.IsStunned = false;
    }

    /// <summary>Immobilisation : bloque le déplacement mais autorise attaques/sorts.</summary>
    public sealed class RootEffect : StatusEffect
    {
        public RootEffect(float duration)
        {
            Duration = duration;
            CCType = CrowdControlType.Root;
        }

        protected override void OnApply() => Target.IsRooted = true;
        public override void OnExpire() => Target.IsRooted = false;
    }

    /// <summary>Silence : bloque les capacités, autorise déplacement et attaques.</summary>
    public sealed class SilenceEffect : StatusEffect
    {
        public SilenceEffect(float duration)
        {
            Duration = duration;
            CCType = CrowdControlType.Silence;
        }

        protected override void OnApply() => Target.IsSilenced = true;
        public override void OnExpire() => Target.IsSilenced = false;
    }
}
