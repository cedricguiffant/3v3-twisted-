using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.UI;

namespace Twisted3v3.Combat
{
    /// <summary>
    /// Retour visuel de combat sur un champion (« juice ») : texte de dégâts flottant
    /// à chaque coup reçu et bref flash blanc du modèle. Purement cosmétique — s'abonne
    /// à <c>Champion.Health.OnDamaged</c> et ne modifie aucun état de jeu.
    /// </summary>
    [RequireComponent(typeof(Champion))]
    public sealed class CombatFeedback : MonoBehaviour
    {
        [SerializeField] private float _flashDuration = 0.12f;
        [SerializeField] private Color _flashColor = Color.white;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Champion _champion;
        private Renderer[] _renderers;
        private Color[] _baseColors;
        private MaterialPropertyBlock _mpb;
        private float _flash; // 1 → 0

        private void Start()
        {
            _champion = GetComponent<Champion>();
            _renderers = GetComponentsInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                var mat = _renderers[i].sharedMaterial;
                _baseColors[i] = mat != null && mat.HasProperty(ColorId) ? mat.color : Color.white;
            }

            if (_champion.Health != null) _champion.Health.OnDamaged += HandleDamaged;
            _champion.OnDied += HandleDied;
            _champion.OnLeveledUp += HandleLeveledUp;
        }

        private void OnDestroy()
        {
            if (_champion != null && _champion.Health != null) _champion.Health.OnDamaged -= HandleDamaged;
            if (_champion != null)
            {
                _champion.OnDied -= HandleDied;
                _champion.OnLeveledUp -= HandleLeveledUp;
            }
        }

        private void HandleDied(Champion c) =>
            Twisted3v3.Audio.Sfx.Play(Twisted3v3.Audio.SfxId.Death, transform.position);

        private void HandleLeveledUp(Champion c) =>
            Twisted3v3.Audio.Sfx.Play(Twisted3v3.Audio.SfxId.LevelUp, transform.position, 0.7f);

        private void HandleDamaged(float amount, DamageInfo info)
        {
            if (amount <= 0f) return;

            bool crit = info.IsCrit;
            Color c = info.Type switch
            {
                DamageType.Magical => new Color(0.55f, 0.75f, 1f),
                DamageType.True => Color.white,
                _ => crit ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.55f, 0.35f)
            };
            string txt = crit ? $"{Mathf.RoundToInt(amount)}!" : Mathf.RoundToInt(amount).ToString();
            FloatingText.Spawn(transform.position, txt, c, crit ? 1.35f : 1f);

            _flash = 1f;

            // Son d'impact (throttlé : les DoT tickent 4×/s, on évite la mitraille).
            if (Time.time - _lastImpactSfx >= 0.08f)
            {
                _lastImpactSfx = Time.time;
                var id = crit ? Twisted3v3.Audio.SfxId.Crit
                    : info.Type == DamageType.Magical ? Twisted3v3.Audio.SfxId.ImpactMagical
                    : Twisted3v3.Audio.SfxId.ImpactPhysical;
                Twisted3v3.Audio.Sfx.Play(id, transform.position, 0.75f);
            }
        }

        private float _lastImpactSfx;

        private void Update()
        {
            if (_flash <= 0f) return;
            _flash = Mathf.Max(0f, _flash - Time.deltaTime / Mathf.Max(0.01f, _flashDuration));

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                _renderers[i].GetPropertyBlock(_mpb);
                Color c = Color.Lerp(_baseColors[i], _flashColor, _flash);
                _mpb.SetColor(ColorId, c);
                _mpb.SetColor(BaseColorId, c);
                _renderers[i].SetPropertyBlock(_mpb);
            }
        }
    }
}
