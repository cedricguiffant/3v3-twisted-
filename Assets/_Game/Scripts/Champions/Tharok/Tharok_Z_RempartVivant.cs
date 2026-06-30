using UnityEngine;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Champions;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Tharok
{
    /// <summary>
    /// Z de Tharok — Rempart Vivant. Érige une protection : octroie un bouclier
    /// temporisé à Tharok et aux alliés proches (encaisse les dégâts entrants).
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Tharok/Z - Rempart Vivant",
                     fileName = "AB_Tharok_Z")]
    public sealed class Tharok_Z_RempartVivant : AbilityData
    {
        public float Radius = 6f;
        public LayerMask TargetMask = ~0;
        public float[] ShieldByRank = { 80f, 120f, 160f, 200f, 240f };
        public float BonusArmorRatio = 0.6f;
        public float ShieldDuration = 4f;

        private static readonly Collider[] _buffer = new Collider[32];

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            float shield = GetByRank(ShieldByRank, ctx.Rank)
                         + BonusArmorRatio * caster.Stats.Value(StatType.Armor);

            int n = Physics.OverlapSphereNonAlloc(caster.transform.position, Radius, _buffer, TargetMask);
            for (int i = 0; i < n; i++)
            {
                if (!_buffer[i].TryGetComponent<Champion>(out var c)) continue;
                if (c.IsDead || !caster.IsAlly(c)) continue;
                c.Health.AddShield(shield, ShieldDuration);
            }
        }
    }
}
