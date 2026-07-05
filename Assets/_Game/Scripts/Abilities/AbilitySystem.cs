using System;
using System.Collections.Generic;
using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Stats;

namespace Twisted3v3.Abilities
{
    /// <summary>
    /// Orchestrateur des capacités d'un champion. Détient une <see cref="AbilityInstance"/>
    /// par slot (Passive/Q/Z/E/R), tick les cooldowns, valide et exécute les casts.
    /// L'input lui fournit le contexte ; il ne lit jamais le clavier lui-même
    /// (séparation des préoccupations).
    /// </summary>
    [RequireComponent(typeof(Champion))]
    public sealed class AbilitySystem : MonoBehaviour
    {
        private Champion _champion;
        private readonly Dictionary<AbilitySlot, AbilityInstance> _slots = new();
        private readonly AbilityContext _ctx = new(); // réutilisé, zéro alloc

        public event Action<AbilitySlot, AbilityInstance> OnAbilityCast;
        public event Action<AbilitySlot, AbilityInstance> OnAbilityFailed;

        /// <summary>
        /// (Réseau) Cast validé, avec son contexte de visée — relayé par le
        /// serveur aux clients pour rejouer le visuel des autres champions.
        /// </summary>
        public event Action<AbilitySlot, int, Vector3, Vector3, Combat.IDamageable> OnCastNetwork;

        public void Initialize(Champion champion, IReadOnlyList<AbilityData> abilities)
        {
            _champion = champion;
            _slots.Clear();
            foreach (var data in abilities)
            {
                if (data == null) continue;
                // Le passif est "appris" d'office au rang 1.
                int startRank = data.Slot == AbilitySlot.Passive ? 1 : 0;
                var inst = new AbilityInstance(data, startRank);
                _slots[data.Slot] = inst;

                // Branche le comportement des capacités déjà apprises (passifs).
                if (inst.IsLearned) data.OnLearned(_champion);
            }
        }

        /// <summary>Monte une capacité d'un rang ; déclenche OnLearned au passage 0→1.</summary>
        public bool LevelUp(AbilitySlot slot)
        {
            var inst = GetSlot(slot);
            if (inst == null) return false;
            bool wasLearned = inst.IsLearned;
            if (!inst.TryLevelUp()) return false;
            if (!wasLearned) inst.Data.OnLearned(_champion);
            return true;
        }

        public AbilityInstance GetSlot(AbilitySlot slot) =>
            _slots.TryGetValue(slot, out var inst) ? inst : null;

        private void Update()
        {
            float cdr = _champion.Stats.Value(StatType.CooldownReduction);
            float dt = Time.deltaTime;
            foreach (var inst in _slots.Values)
                inst.Tick(dt, cdr);
        }

        /// <summary>
        /// Tente de lancer la capacité d'un slot. Renseigne le contexte de visée
        /// avant l'appel via <see cref="BuildContext"/>.
        /// </summary>
        public bool TryCast(AbilitySlot slot, Vector3 aimDirection, Vector3 groundPoint,
                            Combat.IDamageable targetUnit = null)
        {
            var inst = GetSlot(slot);
            if (inst == null || !inst.IsReady) { Fail(slot, inst); return false; }
            if (!_champion.CanCast) { Fail(slot, inst); return false; } // mort, stun ou silence

            float manaCost = inst.Data.GetManaCost(inst.Rank);
            if (!_champion.HasMana(manaCost)) { Fail(slot, inst); return false; }

            BuildContext(inst, aimDirection, groundPoint, targetUnit);
            if (!inst.Data.CanCast(_ctx)) { Fail(slot, inst); return false; }

            // Validation OK → on consomme et on exécute.
            _champion.SpendMana(manaCost);
            inst.Data.Execute(_ctx);
            inst.StartCooldown();
            OnAbilityCast?.Invoke(slot, inst);
            OnCastNetwork?.Invoke(slot, inst.Rank, _ctx.AimDirection, groundPoint, targetUnit);
            return true;
        }

        /// <summary>
        /// (Réseau, client) Rejoue un cast déjà validé par le serveur, sans
        /// vérification ni coût en mana — pour le visuel des champions distants.
        /// Aligne d'abord le rang local sur celui du serveur.
        /// </summary>
        public void ForceCast(AbilitySlot slot, int rank, Vector3 aimDirection,
                              Vector3 groundPoint, Combat.IDamageable targetUnit = null)
        {
            var inst = GetSlot(slot);
            if (inst == null) return;
            NetworkSyncRank(slot, rank);
            BuildContext(inst, aimDirection, groundPoint, targetUnit);
            inst.Data.Execute(_ctx);
            inst.StartCooldown();
            OnAbilityCast?.Invoke(slot, inst);
        }

        /// <summary>(Réseau, client) Monte une capacité au rang validé par le serveur.</summary>
        public void NetworkSyncRank(AbilitySlot slot, int rank)
        {
            var inst = GetSlot(slot);
            if (inst == null) return;
            while (inst.Rank < rank)
            {
                bool wasLearned = inst.IsLearned;
                if (!inst.TryLevelUp()) break;
                if (!wasLearned) inst.Data.OnLearned(_champion);
            }
        }

        private void BuildContext(AbilityInstance inst, Vector3 aim, Vector3 ground,
                                  Combat.IDamageable target)
        {
            _ctx.Reset();
            _ctx.Caster = _champion;
            _ctx.AimDirection = aim.sqrMagnitude > 0.001f ? aim.normalized : _champion.transform.forward;
            _ctx.GroundPoint = ground;
            _ctx.TargetUnit = target;
            _ctx.Rank = inst.Rank;
        }

        private void Fail(AbilitySlot slot, AbilityInstance inst) => OnAbilityFailed?.Invoke(slot, inst);
    }
}
