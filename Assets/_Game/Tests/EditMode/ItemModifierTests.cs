using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Twisted3v3.Items;
using Twisted3v3.Stats;

namespace Twisted3v3.Tests
{
    /// <summary>Vérifie le cœur pur de l'inventaire (<see cref="ItemModifiers"/>) :
    /// l'achat applique les stats de l'item, la revente les retire par source, et
    /// deux items empilent leurs bonus sans interférence.</summary>
    public sealed class ItemModifierTests
    {
        private static ItemData MakeItem(params ItemStat[] stats)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.Stats = new List<ItemStat>(stats);
            return item;
        }

        private static ItemStat S(StatType type, float value) =>
            new ItemStat { Stat = type, Value = value, Modifier = ModifierType.Flat };

        [Test]
        public void Apply_AddsItemStats()
        {
            var sheet = new StatSheet();
            sheet.SetBase(StatType.AttackDamage, 60f);
            var sword = MakeItem(S(StatType.AttackDamage, 55f));

            ItemModifiers.Apply(sheet, sword);

            Assert.AreEqual(115f, sheet.Value(StatType.AttackDamage), 0.001f);
            Object.DestroyImmediate(sword);
        }

        [Test]
        public void Remove_RevertsItemStats()
        {
            var sheet = new StatSheet();
            sheet.SetBase(StatType.MaxHealth, 1000f);
            var armor = MakeItem(S(StatType.MaxHealth, 300f), S(StatType.Armor, 45f));

            ItemModifiers.Apply(sheet, armor);
            Assert.AreEqual(1300f, sheet.Value(StatType.MaxHealth), 0.001f);
            Assert.AreEqual(45f, sheet.Value(StatType.Armor), 0.001f);

            ItemModifiers.Remove(sheet, armor);
            Assert.AreEqual(1000f, sheet.Value(StatType.MaxHealth), 0.001f);
            Assert.AreEqual(0f, sheet.Value(StatType.Armor), 0.001f);
            Object.DestroyImmediate(armor);
        }

        [Test]
        public void TwoItems_Stack_AndRemoveIsIndependent()
        {
            var sheet = new StatSheet();
            sheet.SetBase(StatType.AbilityPower, 0f);
            var staff = MakeItem(S(StatType.AbilityPower, 90f));
            var crown = MakeItem(S(StatType.AbilityPower, 100f));

            ItemModifiers.Apply(sheet, staff);
            ItemModifiers.Apply(sheet, crown);
            Assert.AreEqual(190f, sheet.Value(StatType.AbilityPower), 0.001f);

            ItemModifiers.Remove(sheet, staff); // on ne retire que la source « staff »
            Assert.AreEqual(100f, sheet.Value(StatType.AbilityPower), 0.001f);

            Object.DestroyImmediate(staff);
            Object.DestroyImmediate(crown);
        }
    }
}
