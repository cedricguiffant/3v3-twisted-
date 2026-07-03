using System;
using System.Collections.Generic;
using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Stats;

namespace Twisted3v3.Combat
{
    /// <summary>
    /// Gère PV courants, boucliers, régénération, résolution des dégâts et la mort.
    /// Composant réutilisable par tout ce qui possède un <see cref="StatSheet"/>.
    /// </summary>
    public sealed class HealthSystem
    {
        /// <summary>Un bouclier individuel : montant restant + durée (0 = permanent).</summary>
        private sealed class ShieldInstance
        {
            public float Amount;
            public float TimeRemaining; // <= 0 → permanent
            public bool Permanent => TimeRemaining <= 0f;
        }

        private readonly StatSheet _stats;
        private readonly List<ShieldInstance> _shields = new();

        public float CurrentHealth { get; private set; }

        /// <summary>Dernière source de dégâts (pour attribuer un kill). Souvent un Champion.</summary>
        public object LastDamageSource { get; private set; }

        /// <summary>Somme de tous les boucliers actifs.</summary>
        public float Shield
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < _shields.Count; i++) total += _shields[i].Amount;
                return total;
            }
        }

        public bool IsDead => CurrentHealth <= 0f;

        public float MaxHealth => _stats.Value(StatType.MaxHealth);
        public float HealthPercent => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;

        // Événements pour l'UI, les passifs (ex: Âme Fracturée), le scoring.
        public event Action<float, DamageInfo> OnDamaged;   // (dégâts réels, info)
        public event Action<float> OnHealed;
        public event Action OnDeath;
        public event Action<float> OnHealthChanged; // PV courants

        public HealthSystem(StatSheet stats)
        {
            _stats = stats;
            CurrentHealth = MaxHealth;
        }

        public void Tick(float deltaTime)
        {
            if (IsDead) return;

            // Expiration des boucliers temporisés.
            for (int i = _shields.Count - 1; i >= 0; i--)
            {
                var s = _shields[i];
                if (s.Permanent) continue;
                s.TimeRemaining -= deltaTime;
                if (s.TimeRemaining <= 0f) _shields.RemoveAt(i);
            }

            float regen = _stats.Value(StatType.HealthRegen);
            if (regen > 0f) Heal(regen * deltaTime, this, silent: true);
        }

        /// <summary>Ajoute un bouclier. duration &lt;= 0 → permanent jusqu'à consommation.</summary>
        public void AddShield(float amount, float duration = 0f)
        {
            if (amount <= 0f) return;
            _shields.Add(new ShieldInstance { Amount = amount, TimeRemaining = duration });
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead) return;

            LastDamageSource = info.Source;
            float mitigated = ResolveMitigation(info);

            // Les boucliers absorbent en premier (les temporisés en priorité).
            mitigated = AbsorbWithShields(mitigated);

            CurrentHealth = Mathf.Max(0f, CurrentHealth - mitigated);
            OnDamaged?.Invoke(mitigated, info);
            OnHealthChanged?.Invoke(CurrentHealth);

            if (IsDead) OnDeath?.Invoke();
        }

        public void Heal(float amount, object source = null, bool silent = false)
        {
            if (IsDead || amount <= 0f) return;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            if (!silent) OnHealed?.Invoke(amount);
            OnHealthChanged?.Invoke(CurrentHealth);
        }

        /// <summary>Ressuscite à PV pleins, boucliers purgés (respawn).</summary>
        public void Revive()
        {
            _shields.Clear();
            LastDamageSource = null;
            CurrentHealth = MaxHealth;
            OnHealthChanged?.Invoke(CurrentHealth);
        }

        /// <summary>Augmente le maximum de PV (montée de niveau) en gardant le ratio courant.</summary>
        public void OnMaxHealthIncreased(float previousMax)
        {
            float delta = MaxHealth - previousMax;
            if (delta > 0f && !IsDead)
            {
                CurrentHealth += delta; // on ajoute le gain au courant (style MOBA)
                OnHealthChanged?.Invoke(CurrentHealth);
            }
        }

        /// <summary>Ramène les PV courants sous le maximum (ex: revente d'un item de PV).</summary>
        public void ClampToMax()
        {
            if (CurrentHealth <= MaxHealth) return;
            CurrentHealth = MaxHealth;
            OnHealthChanged?.Invoke(CurrentHealth);
        }

        /// <summary>Consomme les boucliers (temporisés d'abord) et renvoie les dégâts restants.</summary>
        private float AbsorbWithShields(float damage)
        {
            // Trie : temporisés (durée la plus courte) en premier — comportement type LoL.
            _shields.Sort((a, b) =>
            {
                if (a.Permanent != b.Permanent) return a.Permanent ? 1 : -1;
                return a.TimeRemaining.CompareTo(b.TimeRemaining);
            });

            for (int i = 0; i < _shields.Count && damage > 0f; i++)
            {
                float absorbed = Mathf.Min(_shields[i].Amount, damage);
                _shields[i].Amount -= absorbed;
                damage -= absorbed;
            }
            _shields.RemoveAll(s => s.Amount <= 0.001f);
            return damage;
        }

        /// <summary>Applique armure / résistance magique et la pénétration de la source.</summary>
        private float ResolveMitigation(in DamageInfo info)
        {
            if (info.Type == DamageType.True) return info.Amount;

            float resist = info.Type == DamageType.Physical
                ? _stats.Value(StatType.Armor)
                : _stats.Value(StatType.MagicResist);

            // Pénétration plate de la source (optionnel, simplifié).
            if (info.Source is Twisted3v3.Champions.Champion attacker)
            {
                float pen = info.Type == DamageType.Physical
                    ? attacker.Stats.Value(StatType.ArmorPen)
                    : attacker.Stats.Value(StatType.MagicPen);
                resist = Mathf.Max(0f, resist - pen);
            }

            // Formule LoL : multiplicateur = 100 / (100 + résistance)
            float multiplier = 100f / (100f + Mathf.Max(0f, resist));
            return info.Amount * multiplier;
        }
    }
}
