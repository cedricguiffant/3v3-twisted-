using System;
using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Stats;
using Twisted3v3.Combat;
using Twisted3v3.Combat.StatusEffects;
using Twisted3v3.Abilities;

namespace Twisted3v3.Champions
{
    /// <summary>
    /// Entité champion runtime. Façade qui compose les sous-systèmes :
    /// stats (<see cref="StatSheet"/>), vie (<see cref="HealthSystem"/>),
    /// capacités (<see cref="AbilitySystem"/>). Implémente <see cref="IDamageable"/>
    /// pour être ciblable par n'importe quelle capacité.
    ///
    /// Volontairement sans logique d'input ni d'IA : un ChampionController
    /// (joueur) ou un ChampionAI (bot) appelle ses méthodes publiques.
    /// </summary>
    [RequireComponent(typeof(AbilitySystem))]
    public sealed class Champion : MonoBehaviour, IDamageable, IHealthInfo
    {
        [SerializeField] private ChampionData _data;
        [SerializeField] private Team _team = Team.Blue;
        [SerializeField, Range(1, 18)] private int _level = 1;

        public ChampionData Data => _data;
        public StatSheet Stats { get; private set; }
        public HealthSystem Health { get; private set; }
        public AbilitySystem Abilities { get; private set; }
        public StatusEffectController Status { get; private set; }
        public ChampionMotor Motor { get; private set; }

        public int Level => _level;
        public float CurrentMana { get; private set; }

        private bool _networkDriven;

        /// <summary>
        /// Vrai côté client multijoueur : PV/mana viennent des snapshots serveur
        /// (pas de régénération ni de dégâts locaux).
        /// </summary>
        public bool NetworkDriven
        {
            get => _networkDriven;
            set
            {
                _networkDriven = value;
                if (Health != null) Health.NetworkDriven = value;
            }
        }

        // --- CC flags (pilotés par un futur StatusEffectController) ---
        public bool IsDead => Health?.IsDead ?? false;
        public bool IsSilenced { get; set; }
        public bool IsStunned { get; set; }
        public bool IsRooted { get; set; }

        // --- IDamageable ---
        public Team Team => _team;
        public Transform Transform => transform;

        // --- IHealthInfo (pour l'UI) ---
        float IHealthInfo.CurrentHealth => Health != null ? Health.CurrentHealth : 0f;
        float IHealthInfo.MaxHealth => Health != null ? Health.MaxHealth : 1f;
        bool IHealthInfo.IsDead => IsDead;

        public const int MaxLevel = 18;

        public event Action<Champion> OnSpawned;
        public event Action<Champion> OnDied;
        public event Action<Champion> OnLeveledUp;
        public event Action<Champion> OnRespawned;

        // Ne peut agir que vivant et libre de tout CC bloquant.
        public bool CanAct => !IsDead && !IsStunned;
        public bool CanCast => CanAct && !IsSilenced;

        private void Awake()
        {
            Abilities = GetComponent<AbilitySystem>();
            Status = GetComponent<StatusEffectController>() ?? gameObject.AddComponent<StatusEffectController>();
            Motor = GetComponent<ChampionMotor>() ?? gameObject.AddComponent<ChampionMotor>();
            if (_data != null) Setup(_data, _team, _level);
        }

        /// <summary>Initialise (ou ré-initialise) le champion à partir de ses données.</summary>
        public void Setup(ChampionData data, Team team, int level)
        {
            _data = data;
            _team = team;
            _level = Mathf.Clamp(level, 1, 18);

            // 1) Stats
            Stats = new StatSheet();
            _data.ApplyBaseStats(Stats, _level);

            // 2) Vie + ressource
            Health = new HealthSystem(Stats);
            CurrentMana = Stats.Value(StatType.MaxMana);
            Health.OnDeath += HandleDeath;

            // Sous-systèmes composants.
            Status.Initialize(this);

            // 3) Modèle visuel
            if (_data.ModelPrefab != null)
                Instantiate(_data.ModelPrefab, transform);

            // 4) Capacités
            Abilities.Initialize(this, _data.GetAbilities());

            OnSpawned?.Invoke(this);
        }

        private void Update()
        {
            if (Health == null || IsDead) return; // pas encore Setup (data manquante)
            if (_networkDriven) return;           // vitals pilotés par les snapshots
            Health.Tick(Time.deltaTime);
            RegenMana(Time.deltaTime);
        }

        /// <summary>Applique les PV/mana reçus du serveur (client multijoueur).</summary>
        public void NetworkSetVitals(float health, float mana)
        {
            Health?.NetworkSet(health);
            float maxMana = Stats != null ? Stats.Value(StatType.MaxMana) : mana;
            CurrentMana = Mathf.Clamp(mana, 0f, maxMana);
        }

        // --- Ressource mana ---
        public bool HasMana(float amount) => CurrentMana >= amount;

        public void SpendMana(float amount) =>
            CurrentMana = Mathf.Max(0f, CurrentMana - amount);

        /// <summary>Recharge le mana au maximum (respawn, debug).</summary>
        public void RefillMana() => CurrentMana = Stats != null ? Stats.Value(StatType.MaxMana) : 0f;

        /// <summary>Restaure une quantité de mana (fontaine de base, régénération).</summary>
        public void RestoreMana(float amount)
        {
            if (Stats == null || amount <= 0f) return;
            CurrentMana = Mathf.Min(Stats.Value(StatType.MaxMana), CurrentMana + amount);
        }

        /// <summary>
        /// Monte d'un niveau : ré-applique les stats de base au nouveau niveau (les
        /// modificateurs d'items/buffs sont préservés car séparés de la BaseValue),
        /// puis répercute les gains de PV/Mana max sur les valeurs courantes.
        /// </summary>
        public bool LevelUp()
        {
            if (_level >= MaxLevel || _data == null) return false;

            float prevMaxHp = Stats.Value(StatType.MaxHealth);
            float prevMaxMana = Stats.Value(StatType.MaxMana);

            _level++;
            _data.ApplyBaseStats(Stats, _level);

            Health.OnMaxHealthIncreased(prevMaxHp);
            CurrentMana += Mathf.Max(0f, Stats.Value(StatType.MaxMana) - prevMaxMana);

            OnLeveledUp?.Invoke(this);
            return true;
        }

        /// <summary>Ressuscite le champion à PV/Mana pleins (appelé par le RespawnController).</summary>
        public void Respawn(Vector3 position)
        {
            if (Motor != null) Motor.Warp(position); // téléport NavMesh-safe
            else transform.position = position;
            Health.Revive();
            RefillMana();
            IsStunned = IsRooted = IsSilenced = false;
            OnRespawned?.Invoke(this);
        }

        private void RegenMana(float dt)
        {
            float max = Stats.Value(StatType.MaxMana);
            float regen = Stats.Value(StatType.ManaRegen);
            CurrentMana = Mathf.Min(max, CurrentMana + regen * dt);
        }

        // --- IDamageable ---
        public void TakeDamage(in DamageInfo info) => Health.TakeDamage(info);
        public void Heal(float amount, object source = null) => Health.Heal(amount, source);

        private void HandleDeath()
        {
            OnDied?.Invoke(this);
            // TODO: timer de respawn (lié à la durée de partie), désactivation visuelle.
        }

        /// <summary>Vrai si l'autre entité est un ennemi (cible valide pour dégâts).</summary>
        public bool IsEnemy(IDamageable other) =>
            other != null && other.Team != Team && other.Team != Team.None;

        /// <summary>Vrai si l'autre entité est un allié (cible valide pour soin/buff).</summary>
        public bool IsAlly(IDamageable other) =>
            other != null && other.Team == Team;
    }
}
