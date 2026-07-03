using System.Collections.Generic;
using UnityEngine;

namespace Twisted3v3.Audio
{
    /// <summary>Identifiants des effets sonores synthétisés.</summary>
    public enum SfxId
    {
        CastLight,      // sort normal : whoosh + sweep
        CastHeavy,      // ultime : rumble grave + souffle
        ProjectileWhoosh, // départ de projectile
        ImpactPhysical, // coup physique : thump sourd
        ImpactMagical,  // impact magique : zap descendant
        Crit,           // critique : thump + ping aigu
        Death,          // mort : chute de ton + souffle
        LevelUp,        // arpège ascendant
        Buy,            // pièces (2 pings)
        Sell,           // pièces inversées
        Deny            // refus : buzz grave
    }

    /// <summary>
    /// SFX 100 % procéduraux : chaque son est synthétisé par code (sinus/scie/carré/
    /// bruit filtré + enveloppes) au premier usage puis mis en cache — aucun asset
    /// audio requis. Lecture spatialisée (<see cref="Play"/>) ou UI (<see cref="Play2D"/>).
    /// Cohérent avec le reste du projet : tout est généré au runtime.
    /// </summary>
    public static class Sfx
    {
        private const int Rate = 44100;
        private static readonly Dictionary<SfxId, AudioClip> _cache = new();

        // ------------------------------------------------------------- LECTURE
        /// <summary>Joue un son au point donné (spatialisé, léger jitter de pitch).</summary>
        public static void Play(SfxId id, Vector3 position, float volume = 1f)
        {
            var clip = GetClip(id);
            if (clip == null) return;

            var go = new GameObject($"SFX_{id}");
            go.transform.position = position;
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.volume = Mathf.Clamp01(volume);
            src.pitch = Random.Range(0.94f, 1.06f);   // variété entre deux coups
            src.spatialBlend = 0.7f;                   // mi-2D pour rester audible en vue MOBA
            src.minDistance = 8f;
            src.maxDistance = 45f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.Play();
            Object.Destroy(go, clip.length / src.pitch + 0.1f);
        }

        /// <summary>Joue un son d'interface (non spatialisé : shop, level-up UI...).</summary>
        public static void Play2D(SfxId id, float volume = 1f)
        {
            var clip = GetClip(id);
            if (clip == null) return;

            var go = new GameObject($"SFX2D_{id}");
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.volume = Mathf.Clamp01(volume);
            src.spatialBlend = 0f;
            src.Play();
            Object.Destroy(go, clip.length + 0.1f);
        }

        // -------------------------------------------------------------- SYNTHÈSE
        private static AudioClip GetClip(SfxId id)
        {
            if (_cache.TryGetValue(id, out var clip)) return clip;

            float[] s = id switch
            {
                SfxId.CastLight => CastLight(),
                SfxId.CastHeavy => CastHeavy(),
                SfxId.ProjectileWhoosh => Whoosh(),
                SfxId.ImpactPhysical => ImpactPhysical(),
                SfxId.ImpactMagical => ImpactMagical(),
                SfxId.Crit => Crit(),
                SfxId.Death => Death(),
                SfxId.LevelUp => LevelUp(),
                SfxId.Buy => Coins(false),
                SfxId.Sell => Coins(true),
                SfxId.Deny => Deny(),
                _ => null
            };
            if (s == null) return null;

            Normalize(s, 0.8f);
            clip = AudioClip.Create($"sfx_{id}", s.Length, 1, Rate, false);
            clip.SetData(s, 0);
            _cache[id] = clip;
            return clip;
        }

        // Recettes ------------------------------------------------------------
        private static float[] CastLight()
        {
            var s = New(0.20f);
            AddNoise(s, 0f, 0.20f, amp: 0.5f, lowpass: 0.25f, decay: 14f);
            AddSweep(s, 0f, 0.18f, 650f, 280f, amp: 0.35f, decay: 16f, Wave.Sine);
            return s;
        }

        private static float[] CastHeavy()
        {
            var s = New(0.55f);
            AddSweep(s, 0f, 0.5f, 220f, 70f, amp: 0.6f, decay: 6f, Wave.Sine);
            AddNoise(s, 0f, 0.45f, amp: 0.4f, lowpass: 0.12f, decay: 7f);
            AddSweep(s, 0.05f, 0.3f, 440f, 110f, amp: 0.25f, decay: 10f, Wave.Saw);
            return s;
        }

        private static float[] Whoosh()
        {
            var s = New(0.15f);
            AddNoise(s, 0f, 0.15f, amp: 0.35f, lowpass: 0.35f, decay: 18f);
            return s;
        }

        private static float[] ImpactPhysical()
        {
            var s = New(0.14f);
            AddSweep(s, 0f, 0.12f, 160f, 55f, amp: 0.8f, decay: 28f, Wave.Sine); // thump
            AddNoise(s, 0f, 0.05f, amp: 0.5f, lowpass: 0.6f, decay: 60f);        // claquement
            return s;
        }

        private static float[] ImpactMagical()
        {
            var s = New(0.18f);
            AddSweep(s, 0f, 0.16f, 950f, 260f, amp: 0.5f, decay: 20f, Wave.Sine, vibrato: 40f);
            AddNoise(s, 0f, 0.06f, amp: 0.3f, lowpass: 0.5f, decay: 50f);
            return s;
        }

        private static float[] Crit()
        {
            var s = New(0.20f);
            AddSweep(s, 0f, 0.12f, 160f, 50f, amp: 0.8f, decay: 26f, Wave.Sine);
            AddNoise(s, 0f, 0.05f, amp: 0.5f, lowpass: 0.7f, decay: 55f);
            AddSweep(s, 0.02f, 0.12f, 1400f, 1200f, amp: 0.3f, decay: 30f, Wave.Sine); // ping
            return s;
        }

        private static float[] Death()
        {
            var s = New(0.65f);
            AddSweep(s, 0f, 0.6f, 320f, 55f, amp: 0.6f, decay: 5f, Wave.Saw);
            AddNoise(s, 0.1f, 0.5f, amp: 0.3f, lowpass: 0.10f, decay: 6f);
            return s;
        }

        private static float[] LevelUp()
        {
            var s = New(0.5f);
            float[] notes = { 523.25f, 659.25f, 783.99f }; // do–mi–sol
            for (int i = 0; i < notes.Length; i++)
                AddSweep(s, i * 0.09f, 0.18f, notes[i], notes[i], amp: 0.4f, decay: 12f, Wave.Sine);
            return s;
        }

        private static float[] Coins(bool reversed)
        {
            var s = New(0.28f);
            float a = reversed ? 1760f : 1318.5f;
            float b = reversed ? 1318.5f : 1760f;
            AddSweep(s, 0f, 0.10f, a, a, amp: 0.4f, decay: 22f, Wave.Sine);
            AddSweep(s, 0.08f, 0.14f, b, b, amp: 0.4f, decay: 18f, Wave.Sine);
            return s;
        }

        private static float[] Deny()
        {
            var s = New(0.18f);
            AddSweep(s, 0f, 0.16f, 110f, 95f, amp: 0.5f, decay: 12f, Wave.Square);
            return s;
        }

        // Briques bas niveau ---------------------------------------------------
        private enum Wave { Sine, Saw, Square }

        private static float[] New(float seconds) => new float[(int)(seconds * Rate)];

        /// <summary>
        /// Oscillateur avec balayage de fréquence (accumulation de phase → pas de
        /// clic), enveloppe attaque courte + décroissance exponentielle, vibrato optionnel.
        /// </summary>
        private static void AddSweep(float[] buffer, float start, float duration,
            float freqFrom, float freqTo, float amp, float decay, Wave wave, float vibrato = 0f)
        {
            int i0 = (int)(start * Rate);
            int n = Mathf.Min((int)(duration * Rate), buffer.Length - i0);
            float phase = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float freq = Mathf.Lerp(freqFrom, freqTo, t);
                if (vibrato > 0f) freq += Mathf.Sin(i * 2f * Mathf.PI * 30f / Rate) * vibrato;
                phase += 2f * Mathf.PI * freq / Rate;

                float raw = wave switch
                {
                    Wave.Saw => 2f * (phase / (2f * Mathf.PI) % 1f) - 1f,
                    Wave.Square => Mathf.Sign(Mathf.Sin(phase)),
                    _ => Mathf.Sin(phase)
                };

                float attack = Mathf.Min(1f, i / (0.006f * Rate));       // 6 ms d'attaque
                float env = attack * Mathf.Exp(-decay * i / (float)Rate); // décroissance
                buffer[i0 + i] += raw * amp * env;
            }
        }

        /// <summary>Bruit blanc filtré passe-bas (un pôle) avec enveloppe décroissante.</summary>
        private static void AddNoise(float[] buffer, float start, float duration,
            float amp, float lowpass, float decay)
        {
            int i0 = (int)(start * Rate);
            int n = Mathf.Min((int)(duration * Rate), buffer.Length - i0);
            float lp = 0f;

            for (int i = 0; i < n; i++)
            {
                float white = Random.value * 2f - 1f;
                lp += lowpass * (white - lp); // passe-bas simple
                float env = Mathf.Exp(-decay * i / (float)Rate);
                buffer[i0 + i] += lp * amp * env;
            }
        }

        /// <summary>Ramène le pic du signal à <paramref name="peak"/> (évite l'écrêtage).</summary>
        private static void Normalize(float[] s, float peak)
        {
            float max = 0f;
            for (int i = 0; i < s.Length; i++) max = Mathf.Max(max, Mathf.Abs(s[i]));
            if (max < 0.0001f) return;
            float k = peak / max;
            for (int i = 0; i < s.Length; i++) s[i] *= k;
        }
    }
}
