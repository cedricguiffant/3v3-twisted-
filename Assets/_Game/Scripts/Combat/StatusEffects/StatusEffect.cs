using Twisted3v3.Core;
using Twisted3v3.Champions;

namespace Twisted3v3.Combat.StatusEffects
{
    /// <summary>
    /// Effet runtime appliqué à un champion : CC (stun, slow, silence, root) ou buff
    /// temporisé. Instancié par les capacités, géré par le <see cref="StatusEffectController"/>.
    /// </summary>
    public abstract class StatusEffect
    {
        public float Duration { get; protected set; }
        public CrowdControlType CCType { get; protected set; } = CrowdControlType.None;

        /// <summary>Si vrai, une nouvelle application rafraîchit la durée au lieu d'empiler.</summary>
        public virtual bool RefreshOnReapply => true;

        protected Champion Target;
        private float _elapsed;

        public bool IsExpired => _elapsed >= Duration;
        public bool IsCrowdControl => CCType != CrowdControlType.None;

        /// <summary>Branche l'effet sur sa cible. La tenacity raccourcit déjà les CC ici.</summary>
        public void Apply(Champion target)
        {
            Target = target;
            _elapsed = 0f;
            OnApply();
        }

        public void Refresh() => _elapsed = 0f;

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            OnTick(deltaTime);
        }

        protected virtual void OnApply() { }
        protected virtual void OnTick(float deltaTime) { }
        public virtual void OnExpire() { }
    }
}
