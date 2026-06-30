using UnityEngine;

namespace Twisted3v3.Player
{
    /// <summary>
    /// Gère le curseur de la souris. Bascule entre le curseur système par défaut et
    /// un curseur d'attaque (réticule rouge) généré par code — aucun asset requis.
    /// N'applique le changement que lorsque l'état change (pas de churn par frame).
    /// </summary>
    public static class CursorService
    {
        private static Texture2D _attackTexture;
        private static bool _attackActive;
        private static readonly Vector2 _hotspot = new(16f, 16f);

        public static void ShowAttack(bool on)
        {
            if (on == _attackActive) return;
            _attackActive = on;

            if (on)
            {
                if (_attackTexture == null) _attackTexture = BuildAttackCursor();
                Cursor.SetCursor(_attackTexture, _hotspot, CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // curseur système
            }
        }

        /// <summary>Réticule rouge 32×32 : anneau + petites croix cardinales.</summary>
        private static Texture2D BuildAttackCursor()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);
            var red = new Color(0.95f, 0.15f, 0.15f, 1f);
            var center = new Vector2(15.5f, 15.5f);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                bool ring = dist >= 9f && dist <= 11.5f;       // anneau
                bool tick = (dist >= 12f && dist <= 15f) &&     // ticks cardinaux
                            (Mathf.Abs(x - 15.5f) < 1.2f || Mathf.Abs(y - 15.5f) < 1.2f);
                tex.SetPixel(x, y, ring || tick ? red : clear);
            }
            tex.Apply();
            return tex;
        }
    }
}
