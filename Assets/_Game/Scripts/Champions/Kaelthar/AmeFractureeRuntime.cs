using UnityEngine;
using Twisted3v3.Combat;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Kaelthar
{
    /// <summary>
    /// État runtime du passif Âme Fracturée. Écoute les dégâts subis, cumule le
    /// pourcentage de PV perdu et octroie un bouclier à chaque seuil franchi.
    /// Ajouté automatiquement par <see cref="Kaelthar_Passive_AmeFracturee"/>.
    /// </summary>
    public sealed class AmeFractureeRuntime : MonoBehaviour
    {
        private Champion _champion;
        private Kaelthar_Passive_AmeFracturee _config;
        private float _accumulatedDamage;
        private bool _subscribed;

        public void Initialize(Champion champion, Kaelthar_Passive_AmeFracturee config)
        {
            _champion = champion;
            _config = config;

            // (Re)abonnement propre au système de vie.
            if (_subscribed) _champion.Health.OnDamaged -= HandleDamaged;
            _champion.Health.OnDamaged += HandleDamaged;
            _subscribed = true;
        }

        public void Teardown()
        {
            if (_subscribed && _champion?.Health != null)
                _champion.Health.OnDamaged -= HandleDamaged;
            _subscribed = false;
        }

        private void OnDestroy() => Teardown();

        private void HandleDamaged(float damageDealt, DamageInfo info)
        {
            _accumulatedDamage += damageDealt;
            float threshold = _champion.Stats.Value(StatType.MaxHealth) * _config.HealthLostThreshold;
            if (threshold <= 0f) return;

            // Franchit potentiellement plusieurs seuils d'un gros coup.
            while (_accumulatedDamage >= threshold)
            {
                _accumulatedDamage -= threshold;
                GrantShield();
            }
        }

        private void GrantShield()
        {
            float shield = _config.BaseShield
                         + _config.ShieldPerLevel * (_champion.Level - 1)
                         + _config.MaxHealthRatio * _champion.Stats.Value(StatType.MaxHealth);

            _champion.Health.AddShield(shield, _config.ShieldDuration);
            // TODO (VFX) : effet visuel d'âme fracturée autour de Kaelthar.
        }
    }
}
