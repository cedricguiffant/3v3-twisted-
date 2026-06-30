using UnityEngine;
using Twisted3v3.Champions;
using Twisted3v3.Combat;

namespace Twisted3v3.Abilities
{
    /// <summary>
    /// Toutes les infos nécessaires à l'exécution d'une capacité.
    /// Construit par l'<see cref="AbilitySystem"/> à partir de l'input, puis passé
    /// à <c>AbilityData.Execute</c>. Garde les capacités découplées de l'input.
    /// </summary>
    public sealed class AbilityContext
    {
        /// <summary>Le champion qui lance la capacité.</summary>
        public Champion Caster;

        /// <summary>Direction visée (skillshots). Normalisée, sur le plan horizontal.</summary>
        public Vector3 AimDirection;

        /// <summary>Point au sol visé (zones, murs, blinks).</summary>
        public Vector3 GroundPoint;

        /// <summary>Cible unité (sorts ciblés : marque, soin allié...). Peut être null.</summary>
        public IDamageable TargetUnit;

        /// <summary>Le rang actuel de la capacité (1–5), pour scaler dégâts/cooldown.</summary>
        public int Rank;

        public void Reset()
        {
            Caster = null;
            AimDirection = Vector3.zero;
            GroundPoint = Vector3.zero;
            TargetUnit = null;
            Rank = 1;
        }
    }
}
