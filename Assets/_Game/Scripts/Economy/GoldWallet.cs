using System;
using UnityEngine;

namespace Twisted3v3.Economy
{
    /// <summary>
    /// Or d'un champion : montant de départ, revenu passif par seconde, gains de kills,
    /// et dépenses (achats au shop). Source de vérité pour l'économie d'un joueur.
    /// </summary>
    public sealed class GoldWallet : MonoBehaviour
    {
        [SerializeField] private int _startingGold = 500;
        [Tooltip("Or généré passivement par seconde.")]
        [SerializeField] private float _passiveIncomePerSecond = 2f;

        public int Gold { get; private set; }

        public event Action<GoldWallet> OnGoldChanged;

        private float _incomeAccumulator;

        private void Start()
        {
            Gold = _startingGold;
            OnGoldChanged?.Invoke(this);
        }

        private void Update()
        {
            if (_passiveIncomePerSecond <= 0f) return;
            _incomeAccumulator += _passiveIncomePerSecond * Time.deltaTime;
            if (_incomeAccumulator >= 1f)
            {
                int whole = Mathf.FloorToInt(_incomeAccumulator);
                _incomeAccumulator -= whole;
                AddGold(whole);
            }
        }

        public void AddGold(int amount)
        {
            if (amount == 0) return;
            Gold = Mathf.Max(0, Gold + amount);
            OnGoldChanged?.Invoke(this);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Gold < amount) return false;
            Gold -= amount;
            OnGoldChanged?.Invoke(this);
            return true;
        }
    }
}
