using UnityEngine;

namespace Twisted3v3.VFX
{
    /// <summary>
    /// Anti-fuite mémoire : détruit les instances de matériaux créées par un accès à
    /// <c>renderer.material</c> (suffixe « (Instance) ») quand l'objet est détruit.
    /// Unity ne libère PAS ces copies automatiquement — sur un match entier,
    /// projectiles/zones/anneaux de cast en créent des centaines.
    /// À poser sur tout GameObject éphémère dont on teinte le matériau.
    /// </summary>
    public sealed class MaterialCleanup : MonoBehaviour
    {
        private void OnDestroy()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                foreach (var m in r.sharedMaterials)
                {
                    // Seules les copies runtime portent le suffixe — jamais les assets.
                    if (m != null && m.name.EndsWith(" (Instance)"))
                        Destroy(m);
                }
            }
        }
    }
}
