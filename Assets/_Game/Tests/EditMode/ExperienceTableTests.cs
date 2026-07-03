using NUnit.Framework;
using Twisted3v3.Progression;

namespace Twisted3v3.Tests
{
    /// <summary>Vérifie la courbe d'XP : palier linéaire, cumul, et plafond au niveau max.</summary>
    public sealed class ExperienceTableTests
    {
        [Test]
        public void XpToNext_Level1_IsBaseXp()
        {
            Assert.AreEqual(180f, ExperienceTable.XpToNext(1), 0.001f);
        }

        [Test]
        public void XpToNext_IncreasesLinearly()
        {
            Assert.AreEqual(280f, ExperienceTable.XpToNext(2), 0.001f); // 180 + 100
            Assert.AreEqual(380f, ExperienceTable.XpToNext(3), 0.001f); // 180 + 200
        }

        [Test]
        public void XpToNext_AtMaxLevel_IsInfinity()
        {
            Assert.IsTrue(float.IsPositiveInfinity(ExperienceTable.XpToNext(ExperienceTable.MaxLevel)));
        }

        [Test]
        public void TotalXpForLevel_1_IsZero()
        {
            Assert.AreEqual(0f, ExperienceTable.TotalXpForLevel(1), 0.001f);
        }

        [Test]
        public void TotalXpForLevel_3_SumsFirstTwoSteps()
        {
            // Niveau 1→2 (180) + 2→3 (280) = 460
            Assert.AreEqual(460f, ExperienceTable.TotalXpForLevel(3), 0.001f);
        }

        [Test]
        public void TotalXpForLevel_ClampsBelowOne()
        {
            Assert.AreEqual(0f, ExperienceTable.TotalXpForLevel(0), 0.001f);
        }
    }
}
