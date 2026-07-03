using NUnit.Framework;
using Twisted3v3.Stats;

namespace Twisted3v3.Tests
{
    /// <summary>Vérifie le conteneur <see cref="StatSheet"/> : valeurs par défaut,
    /// modificateurs et retrait par source sur l'ensemble des stats.</summary>
    public sealed class StatSheetTests
    {
        [Test]
        public void UnknownStat_DefaultsToZero()
        {
            var sheet = new StatSheet();
            Assert.AreEqual(0f, sheet.Value(StatType.AttackDamage), 0.001f);
        }

        [Test]
        public void SetBase_ThenValue_ReturnsBase()
        {
            var sheet = new StatSheet();
            sheet.SetBase(StatType.MaxHealth, 1000f);
            Assert.AreEqual(1000f, sheet.Value(StatType.MaxHealth), 0.001f);
        }

        [Test]
        public void AddModifier_AffectsValue()
        {
            var sheet = new StatSheet();
            sheet.SetBase(StatType.AttackDamage, 60f);
            sheet.AddModifier(StatType.AttackDamage, new StatModifier(40f, ModifierType.Flat));
            Assert.AreEqual(100f, sheet.Value(StatType.AttackDamage), 0.001f);
        }

        [Test]
        public void RemoveAllFromSource_ClearsAcrossAllStats()
        {
            var sheet = new StatSheet();
            sheet.SetBase(StatType.AttackDamage, 60f);
            sheet.SetBase(StatType.MaxHealth, 1000f);
            object item = new object();
            sheet.AddModifier(StatType.AttackDamage, new StatModifier(40f, ModifierType.Flat, item));
            sheet.AddModifier(StatType.MaxHealth, new StatModifier(300f, ModifierType.Flat, item));

            sheet.RemoveAllFromSource(item);

            Assert.AreEqual(60f, sheet.Value(StatType.AttackDamage), 0.001f);
            Assert.AreEqual(1000f, sheet.Value(StatType.MaxHealth), 0.001f);
        }
    }
}
