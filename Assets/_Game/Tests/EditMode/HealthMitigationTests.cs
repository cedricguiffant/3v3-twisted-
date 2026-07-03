using NUnit.Framework;
using Twisted3v3.Combat;
using Twisted3v3.Core;
using Twisted3v3.Stats;

namespace Twisted3v3.Tests
{
    /// <summary>Vérifie la formule de mitigation (100/(100+résistance)), les dégâts
    /// bruts et l'absorption par bouclier de <see cref="HealthSystem"/>.</summary>
    public sealed class HealthMitigationTests
    {
        private static HealthSystem MakeHealth(float maxHp, float armor = 0f, float mr = 0f)
        {
            var sheet = new StatSheet();
            sheet.SetBase(StatType.MaxHealth, maxHp);
            sheet.SetBase(StatType.Armor, armor);
            sheet.SetBase(StatType.MagicResist, mr);
            return new HealthSystem(sheet);
        }

        [Test]
        public void NewHealth_StartsFull()
        {
            var hp = MakeHealth(1000f);
            Assert.AreEqual(1000f, hp.CurrentHealth, 0.001f);
        }

        [Test]
        public void ZeroArmor_TakesFullPhysicalDamage()
        {
            var hp = MakeHealth(1000f, armor: 0f);
            hp.TakeDamage(new DamageInfo(200f, DamageType.Physical, null));
            Assert.AreEqual(800f, hp.CurrentHealth, 0.001f);
        }

        [Test]
        public void Armor100_Halves_PhysicalDamage()
        {
            var hp = MakeHealth(1000f, armor: 100f); // mult = 100/200 = 0.5
            hp.TakeDamage(new DamageInfo(200f, DamageType.Physical, null));
            Assert.AreEqual(900f, hp.CurrentHealth, 0.001f); // 100 réels
        }

        [Test]
        public void MagicResist_MitigatesMagicalOnly()
        {
            var hp = MakeHealth(1000f, armor: 0f, mr: 100f);
            hp.TakeDamage(new DamageInfo(200f, DamageType.Magical, null)); // 100 réels
            Assert.AreEqual(900f, hp.CurrentHealth, 0.001f);
        }

        [Test]
        public void TrueDamage_IgnoresResistances()
        {
            var hp = MakeHealth(1000f, armor: 100f, mr: 100f);
            hp.TakeDamage(new DamageInfo(200f, DamageType.True, null));
            Assert.AreEqual(800f, hp.CurrentHealth, 0.001f);
        }

        [Test]
        public void Shield_AbsorbsBeforeHealth()
        {
            var hp = MakeHealth(1000f);
            hp.AddShield(50f);
            hp.TakeDamage(new DamageInfo(100f, DamageType.True, null));
            Assert.AreEqual(950f, hp.CurrentHealth, 0.001f); // 50 absorbés, 50 passent
        }

        [Test]
        public void LethalDamage_MarksDead()
        {
            var hp = MakeHealth(500f);
            hp.TakeDamage(new DamageInfo(500f, DamageType.True, null));
            Assert.IsTrue(hp.IsDead);
            Assert.AreEqual(0f, hp.CurrentHealth, 0.001f);
        }
    }
}
