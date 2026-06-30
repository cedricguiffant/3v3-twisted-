using UnityEngine;
using Twisted3v3.Core;

namespace Twisted3v3.Combat
{
    /// <summary>
    /// Cible d'entraînement minimale implémentant <see cref="IDamageable"/>.
    /// Pas d'armure ni de stats : affiche les dégâts bruts reçus dans la console
    /// pour valider rapidement les capacités. Requiert un Collider (sur le layer
    /// "Units") pour être détecté par les raycasts et les overlaps de zone.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class TrainingDummy : MonoBehaviour, IDamageable, IHealthInfo
    {
        [SerializeField] private Team _team = Team.Red;
        [SerializeField] private float _maxHealth = 2000f;
        [SerializeField] private bool _autoRespawn = true;

        private float _health;

        public Team Team => _team;
        public Transform Transform => transform;
        public bool IsDead => _health <= 0f;

        public float CurrentHealth => _health;
        public float MaxHealth => _maxHealth;

        private void Awake()
        {
            _health = _maxHealth;
            var bar = gameObject.AddComponent<Twisted3v3.UI.WorldHealthBar>();
            bar.SetColor(new Color(0.85f, 0.3f, 0.3f));
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead) return;
            _health -= info.Amount;
            Debug.Log($"[Dummy] -{info.Amount:0} dégâts {info.Type} → {Mathf.Max(0f, _health):0}/{_maxHealth:0} PV");

            if (IsDead)
            {
                Debug.Log("[Dummy] détruit.");
                // Récompense le tueur (or + XP) si un KillReward est présent.
                if (TryGetComponent<KillReward>(out var reward))
                    KillRewardService.Award(info.Source, reward);
                if (_autoRespawn) _health = _maxHealth;
            }
        }

        public void Heal(float amount, object source = null) =>
            _health = Mathf.Min(_maxHealth, _health + amount);
    }
}
