using UnityEngine;
using Twisted3v3.Core;

namespace Twisted3v3.Combat
{
    /// <summary>
    /// Toute entité pouvant recevoir des dégâts / soins (champion, monstre, tourelle).
    /// Abstrait le ciblage des capacités : elles ne connaissent que cette interface.
    /// </summary>
    public interface IDamageable
    {
        Team Team { get; }
        Transform Transform { get; }
        bool IsDead { get; }

        void TakeDamage(in DamageInfo info);
        void Heal(float amount, object source = null);
    }
}
