using UnityEngine;
using Twisted3v3.Combat;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Lirael
{
    /// <summary>
    /// État runtime du passif Tir Précis : écoute les auto-attaques et accumule des
    /// charges de vitesse d'attaque + portée (modificateurs additifs cumulés).
    /// </summary>
    public sealed class TirPrecisRuntime : MonoBehaviour
    {
        private Champion _champion;
        private Lirael_Passive_TirPrecis _config;
        private AutoAttack _autoAttack;
        private StatModifier _asMod, _rangeMod;
        private int _stacks;
        private bool _subscribed;

        public void Initialize(Champion champion, Lirael_Passive_TirPrecis config)
        {
            _champion = champion;
            _config = config;
            _autoAttack = champion.GetComponent<AutoAttack>();
            if (_autoAttack != null && !_subscribed)
            {
                _autoAttack.OnAttack += HandleAttack;
                _subscribed = true;
            }
        }

        public void Teardown()
        {
            if (_subscribed && _autoAttack != null) _autoAttack.OnAttack -= HandleAttack;
            _subscribed = false;
            RemoveModifiers();
        }

        private void OnDestroy() => Teardown();

        private void HandleAttack()
        {
            if (_stacks >= _config.MaxStacks) return;
            _stacks++;
            RemoveModifiers();
            _asMod = new StatModifier(_stacks * _config.AttackSpeedPerStack, ModifierType.PercentAdditive, this);
            _rangeMod = new StatModifier(_stacks * _config.RangePerStack, ModifierType.Flat, this);
            _champion.Stats.AddModifier(StatType.AttackSpeed, _asMod);
            _champion.Stats.AddModifier(StatType.AttackRange, _rangeMod);
        }

        private void RemoveModifiers()
        {
            if (_asMod != null) { _champion.Stats.Get(StatType.AttackSpeed).RemoveModifier(_asMod); _asMod = null; }
            if (_rangeMod != null) { _champion.Stats.Get(StatType.AttackRange).RemoveModifier(_rangeMod); _rangeMod = null; }
        }
    }
}
