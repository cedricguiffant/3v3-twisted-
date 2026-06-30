using UnityEngine;
using Twisted3v3.Combat;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Ragnor
{
    /// <summary>
    /// État runtime du passif Fureur des Forges. Gagne une charge à chaque fois que
    /// Ragnor subit des dégâts (il s'échauffe au combat) ; applique un bonus d'AD
    /// proportionnel au nombre de charges, qui retombe après un délai hors combat.
    /// </summary>
    public sealed class FureurDesForgesRuntime : MonoBehaviour
    {
        private Champion _champion;
        private Ragnor_Passive_FureurDesForges _config;
        private StatModifier _modifier;
        private int _stacks;
        private float _lastStackTime;
        private bool _subscribed;

        public void Initialize(Champion champion, Ragnor_Passive_FureurDesForges config)
        {
            _champion = champion;
            _config = config;
            if (_subscribed) _champion.Health.OnDamaged -= HandleDamaged;
            _champion.Health.OnDamaged += HandleDamaged;
            _subscribed = true;
        }

        public void Teardown()
        {
            if (_subscribed && _champion?.Health != null)
                _champion.Health.OnDamaged -= HandleDamaged;
            _subscribed = false;
            RemoveModifier();
        }

        private void OnDestroy() => Teardown();

        private void HandleDamaged(float amount, DamageInfo info)
        {
            if (_stacks >= _config.MaxStacks) { _lastStackTime = Time.time; return; }
            _stacks++;
            _lastStackTime = Time.time;
            ReapplyModifier();
        }

        private void Update()
        {
            if (_stacks > 0 && Time.time - _lastStackTime > _config.DecayDelay)
            {
                _stacks = 0;
                RemoveModifier();
            }
        }

        private void ReapplyModifier()
        {
            RemoveModifier();
            _modifier = new StatModifier(_stacks * _config.AttackDamagePerStack,
                                         ModifierType.Flat, this);
            _champion.Stats.AddModifier(StatType.AttackDamage, _modifier);
        }

        private void RemoveModifier()
        {
            if (_modifier != null)
            {
                _champion.Stats.Get(StatType.AttackDamage).RemoveModifier(_modifier);
                _modifier = null;
            }
        }
    }
}
