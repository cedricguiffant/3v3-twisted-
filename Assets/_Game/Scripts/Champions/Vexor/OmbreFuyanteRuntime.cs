using System.Collections;
using UnityEngine;
using Twisted3v3.Combat;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Vexor
{
    /// <summary>
    /// État runtime du passif Ombre Fuyante : surveille les PV ; sous le seuil et hors
    /// récurrence, octroie vitesse + bouclier pour s'échapper.
    /// </summary>
    public sealed class OmbreFuyanteRuntime : MonoBehaviour
    {
        private Champion _champion;
        private Vexor_Passive_OmbreFuyante _config;
        private float _readyTime;
        private bool _subscribed;

        public void Initialize(Champion champion, Vexor_Passive_OmbreFuyante config)
        {
            _champion = champion;
            _config = config;
            if (!_subscribed) { _champion.Health.OnDamaged += HandleDamaged; _subscribed = true; }
        }

        public void Teardown()
        {
            if (_subscribed && _champion?.Health != null) _champion.Health.OnDamaged -= HandleDamaged;
            _subscribed = false;
        }

        private void OnDestroy() => Teardown();

        private void HandleDamaged(float amount, DamageInfo info)
        {
            if (Time.time < _readyTime || _champion.IsDead) return;
            if (_champion.Health.HealthPercent > _config.HealthThreshold) return;

            _readyTime = Time.time + _config.Cooldown;
            _champion.Health.AddShield(_config.Shield, _config.Duration);
            StartCoroutine(SpeedBurst());
        }

        private IEnumerator SpeedBurst()
        {
            var mod = new StatModifier(_config.MoveSpeedBonus, ModifierType.PercentAdditive, this);
            _champion.Stats.AddModifier(StatType.MoveSpeed, mod);
            yield return new WaitForSeconds(_config.Duration);
            _champion.Stats.Get(StatType.MoveSpeed).RemoveModifier(mod);
        }
    }
}
