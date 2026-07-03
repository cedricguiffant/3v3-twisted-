using NUnit.Framework;
using Twisted3v3.Stats;

namespace Twisted3v3.Tests
{
    /// <summary>Vérifie l'ordre d'application des modificateurs (Flat → %Add → %Mult)
    /// et le cache dirty-flag de <see cref="Stat"/>.</summary>
    public sealed class StatTests
    {
        [Test]
        public void BaseValue_WithoutModifiers_ReturnsBase()
        {
            var stat = new Stat(100f);
            Assert.AreEqual(100f, stat.Value, 0.001f);
        }

        [Test]
        public void FlatModifiers_AreSummed()
        {
            var stat = new Stat(100f);
            stat.AddModifier(new StatModifier(20f, ModifierType.Flat));
            stat.AddModifier(new StatModifier(30f, ModifierType.Flat));
            Assert.AreEqual(150f, stat.Value, 0.001f);
        }

        [Test]
        public void PercentAdditive_AppliesToFlatTotal()
        {
            var stat = new Stat(100f);
            stat.AddModifier(new StatModifier(50f, ModifierType.Flat));   // → 150
            stat.AddModifier(new StatModifier(0.10f, ModifierType.PercentAdditive));
            stat.AddModifier(new StatModifier(0.10f, ModifierType.PercentAdditive)); // +20% cumulés
            Assert.AreEqual(180f, stat.Value, 0.001f); // 150 * 1.20
        }

        [Test]
        public void PercentMultiplicative_AppliesSequentially()
        {
            var stat = new Stat(100f);
            stat.AddModifier(new StatModifier(0.10f, ModifierType.PercentMultiplicative));
            stat.AddModifier(new StatModifier(0.10f, ModifierType.PercentMultiplicative));
            Assert.AreEqual(121f, stat.Value, 0.001f); // 100 * 1.1 * 1.1
        }

        [Test]
        public void Order_FlatThenAddThenMult()
        {
            var stat = new Stat(100f);
            stat.AddModifier(new StatModifier(100f, ModifierType.Flat));           // 200
            stat.AddModifier(new StatModifier(0.50f, ModifierType.PercentAdditive)); // 300
            stat.AddModifier(new StatModifier(0.10f, ModifierType.PercentMultiplicative)); // 330
            Assert.AreEqual(330f, stat.Value, 0.001f);
        }

        [Test]
        public void RemoveModifier_RecalculatesValue()
        {
            var stat = new Stat(100f);
            var mod = new StatModifier(50f, ModifierType.Flat);
            stat.AddModifier(mod);
            Assert.AreEqual(150f, stat.Value, 0.001f);
            Assert.IsTrue(stat.RemoveModifier(mod));
            Assert.AreEqual(100f, stat.Value, 0.001f);
        }

        [Test]
        public void RemoveAllFromSource_RemovesOnlyThatSource()
        {
            var stat = new Stat(100f);
            object itemA = new object();
            object itemB = new object();
            stat.AddModifier(new StatModifier(50f, ModifierType.Flat, itemA));
            stat.AddModifier(new StatModifier(30f, ModifierType.Flat, itemB));
            stat.RemoveAllFromSource(itemA);
            Assert.AreEqual(130f, stat.Value, 0.001f); // reste itemB
        }

        [Test]
        public void OnValueChanged_FiresOnModifierChange()
        {
            var stat = new Stat(100f);
            int calls = 0;
            stat.OnValueChanged += _ => calls++;
            stat.AddModifier(new StatModifier(10f, ModifierType.Flat));
            Assert.AreEqual(1, calls);
        }
    }
}
