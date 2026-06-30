using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Champions;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Sylvara
{
    /// <summary>
    /// E de Sylvara — Transfert Sylvestre. Soigne Sylvara et l'allié le plus blessé à
    /// proximité, et téléporte cet allié auprès d'elle (sauvetage / regroupement).
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Sylvara/E - Transfert Sylvestre",
                     fileName = "AB_Sylvara_E")]
    public sealed class Sylvara_E_TransfertSylvestre : AbilityData
    {
        public float SearchRadius = 14f;
        public LayerMask TargetMask = ~0;
        public float[] HealByRank = { 80f, 120f, 160f, 200f, 240f };
        public float ApRatio = 0.5f;
        public bool TeleportAlly = true;

        private static readonly Collider[] _buffer = new Collider[32];

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            float heal = GetByRank(HealByRank, ctx.Rank)
                       + ApRatio * caster.Stats.Value(StatType.AbilityPower);

            caster.Heal(heal, caster);

            // Allié vivant le plus blessé (hors Sylvara) dans le rayon.
            Champion wounded = null;
            float lowest = 1f;
            int n = Physics.OverlapSphereNonAlloc(caster.transform.position, SearchRadius, _buffer, TargetMask);
            for (int i = 0; i < n; i++)
            {
                if (!_buffer[i].TryGetComponent<Champion>(out var c)) continue;
                if (c == caster || c.IsDead || !caster.IsAlly(c)) continue;
                float pct = c.Health.HealthPercent;
                if (pct < lowest) { lowest = pct; wounded = c; }
            }

            if (wounded != null)
            {
                wounded.Heal(heal, caster);
                if (TeleportAlly) wounded.Motor.Warp(caster.transform.position - caster.transform.forward * 1.5f);
            }
        }
    }
}
