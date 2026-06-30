using System.Collections.Generic;
using UnityEngine;
using Twisted3v3.Stats;

namespace Twisted3v3.Jungle
{
    /// <summary>Un buff (stat plate) octroyé au clear d'un camp / capture d'objectif.</summary>
    [System.Serializable]
    public struct CampReward
    {
        public StatType Stat;
        public float Value;
    }

    /// <summary>
    /// Définition data-driven d'un camp de jungle (Golem Ancien, Loup Alpha,
    /// Sentinelles Spectrales, Harpie des Ronces) ou de l'objectif majeur
    /// (Autel des Âmes). Auto-suffisant : le camp construit ses monstres par code,
    /// aucun prefab requis. Créer via : Assets > Create > Twisted3v3 > Jungle Camp.
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Jungle Camp", fileName = "JC_NewCamp")]
    public sealed class JungleCampData : ScriptableObject
    {
        [Header("Identité")]
        public string DisplayName = "Nouveau Camp";
        public bool IsMajorObjective = false; // true pour l'Autel des Âmes

        [Header("Monstres")]
        public int MonsterCount = 1;
        public float MonsterMaxHealth = 800f;
        public float MonsterContactDamage = 25f;
        public float MonsterAttackInterval = 1.2f;
        public float MonsterMoveSpeed = 3f;
        [Tooltip("Distance au-delà de laquelle le monstre se désengage et se régénère.")]
        public float LeashRange = 10f;
        public float MonsterScale = 1.6f;
        public Color MonsterColor = new(0.3f, 0.7f, 0.3f);

        [Header("Respawn")]
        public float RespawnTime = 60f;
        public float InitialSpawnDelay = 20f;

        [Header("Récompenses par monstre")]
        public int GoldPerMonster = 45;
        public float XpPerMonster = 70f;

        [Header("Buff de clear / capture")]
        [Tooltip("Buffs appliqués quand le camp est nettoyé (ou l'objectif capturé).")]
        public List<CampReward> ClearBuffs = new();
        [Tooltip("Si vrai, le buff s'applique à toute l'équipe (Autel des Âmes).")]
        public bool TeamWideBuff = false;
        public float BuffDuration = 60f;
    }
}
