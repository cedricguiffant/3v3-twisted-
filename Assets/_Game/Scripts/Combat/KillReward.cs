using UnityEngine;
using Twisted3v3.Champions;
using Twisted3v3.Economy;
using Twisted3v3.Progression;

namespace Twisted3v3.Combat
{
    /// <summary>
    /// Récompense accordée au tueur de cette entité (champion, monstre de jungle,
    /// mannequin). Donnée brute, lue par <see cref="KillRewardService"/>.
    /// </summary>
    public sealed class KillReward : MonoBehaviour
    {
        public int Gold = 300;
        public float Experience = 200f;
    }

    /// <summary>Attribue or + XP au tueur s'il possède les composants adéquats.</summary>
    public static class KillRewardService
    {
        public static void Award(object killer, KillReward reward)
        {
            if (reward == null) return;
            var champion = killer as Champion;
            if (champion == null) return;

            if (champion.TryGetComponent<GoldWallet>(out var wallet))
                wallet.AddGold(reward.Gold);
            if (champion.TryGetComponent<LevelSystem>(out var level))
                level.GainExperience(reward.Experience);

            Debug.Log($"[Kill] {champion.name} +{reward.Gold} or, +{reward.Experience:0} XP");
        }
    }
}
