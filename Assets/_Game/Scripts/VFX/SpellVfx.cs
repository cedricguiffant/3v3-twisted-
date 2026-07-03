using UnityEngine;

namespace Twisted3v3.VFX
{
    /// <summary>
    /// Fabrique d'effets visuels de sorts, 100 % générés par code (aucun prefab requis) :
    /// onde de cast au sol, gerbe d'étincelles à l'impact, traînée + halo sur les
    /// projectiles. Tous les effets sont éphémères, colorés par champion et purement
    /// cosmétiques (aucun impact gameplay).
    /// </summary>
    public static class SpellVfx
    {
        private static Material _ringMaterial;   // partagé, teinté par vertex color
        private static Material _sparkMaterial;  // additive pour les étincelles

        // ------------------------------------------------------------------ CAST
        /// <summary>Onde circulaire + flash lumineux au lancement d'un sort.</summary>
        public static void CastBurst(Vector3 position, Color color)
        {
            SpawnRing(position, color, startRadius: 0.4f, endRadius: 2.6f, life: 0.35f);
            SpawnLightFlash(position + Vector3.up * 1.2f, color, intensity: 2.6f, range: 7f, life: 0.28f);
            SpawnSparks(position + Vector3.up * 0.8f, color, count: 14, speed: 5f, life: 0.45f);
        }

        // ---------------------------------------------------------------- IMPACT
        /// <summary>Gerbe d'étincelles + flash à l'impact d'un projectile.</summary>
        public static void ImpactBurst(Vector3 position, Color color)
        {
            SpawnLightFlash(position, color, intensity: 2.2f, range: 5f, life: 0.2f);
            SpawnSparks(position, color, count: 18, speed: 6.5f, life: 0.35f);
        }

        // ------------------------------------------------------------ PROJECTILE
        /// <summary>Ajoute traînée, halo lumineux et émission au visuel d'un projectile.</summary>
        public static void AddProjectileJuice(GameObject projectile, Color color, float radius)
        {
            // Traînée dégradée couleur → transparent.
            var trail = projectile.AddComponent<TrailRenderer>();
            trail.time = 0.28f;
            trail.startWidth = Mathf.Max(0.15f, radius * 1.6f);
            trail.endWidth = 0f;
            trail.material = RingMaterial();
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = grad;

            // Halo lumineux embarqué.
            var light = projectile.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 1.8f;
            light.range = 5f;
            light.shadows = LightShadows.None;

            // Émission sur la sphère du visuel (matériau déjà instancié par le Projectile).
            var rend = projectile.GetComponentInChildren<Renderer>();
            if (rend != null && rend.material.HasProperty("_EmissionColor"))
            {
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", color * 1.6f);
            }
        }

        // ------------------------------------------------------------- INTERNES
        private static void SpawnRing(Vector3 position, Color color,
            float startRadius, float endRadius, float life)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "VFX_CastRing";
            if (ring.TryGetComponent<Collider>(out var col)) Object.Destroy(col);
            ring.transform.position = position + Vector3.up * 0.06f;
            var rend = ring.GetComponent<Renderer>();
            rend.sharedMaterial = RingMaterial();
            rend.material.color = color; // copie « (Instance) », libérée par MaterialCleanup
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.AddComponent<MaterialCleanup>();

            var anim = ring.AddComponent<ExpandAndFade>();
            anim.Configure(startRadius, endRadius, life, color);
        }

        private static void SpawnLightFlash(Vector3 position, Color color,
            float intensity, float range, float life)
        {
            var go = new GameObject("VFX_LightFlash");
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            go.AddComponent<LightFade>().Configure(life);
        }

        private static void SpawnSparks(Vector3 position, Color color,
            int count, float speed, float life)
        {
            var go = new GameObject("VFX_Sparks");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();

            // IMPORTANT : le ParticleSystem démarre en lecture dès l'AddComponent —
            // configurer duration/bursts pendant la lecture lève une exception.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = life;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = color;
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = SparkMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            ps.Play();
            Object.Destroy(go, life + 0.5f);
        }

        private static Material RingMaterial()
        {
            if (_ringMaterial == null)
                _ringMaterial = new Material(Shader.Find("Sprites/Default"));
            return _ringMaterial;
        }

        private static Material SparkMaterial()
        {
            if (_sparkMaterial == null)
            {
                var shader = Shader.Find("Legacy Shaders/Particles/Additive")
                             ?? Shader.Find("Sprites/Default");
                _sparkMaterial = new Material(shader);
            }
            return _sparkMaterial;
        }
    }

    /// <summary>Anneau qui s'étend et s'estompe puis se détruit (onde de cast).</summary>
    public sealed class ExpandAndFade : MonoBehaviour
    {
        private float _start, _end, _life, _elapsed;
        private Color _color;
        private Renderer _renderer;

        public void Configure(float startRadius, float endRadius, float life, Color color)
        {
            _start = startRadius; _end = endRadius; _life = life; _color = color;
            _renderer = GetComponent<Renderer>();
            Apply(0f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / _life;
            if (t >= 1f) { Destroy(gameObject); return; }
            Apply(t);
        }

        private void Apply(float t)
        {
            float r = Mathf.Lerp(_start, _end, Mathf.Sin(t * Mathf.PI * 0.5f)); // ease-out
            transform.localScale = new Vector3(r * 2f, 0.02f, r * 2f);
            if (_renderer != null)
            {
                var c = _color; c.a = Mathf.Lerp(0.7f, 0f, t);
                _renderer.material.color = c;
            }
        }
    }

    /// <summary>Lumière qui décroît linéairement puis se détruit (flash).</summary>
    public sealed class LightFade : MonoBehaviour
    {
        private float _life, _elapsed, _baseIntensity;
        private Light _light;

        public void Configure(float life)
        {
            _life = life;
            _light = GetComponent<Light>();
            _baseIntensity = _light != null ? _light.intensity : 0f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / _life;
            if (t >= 1f) { Destroy(gameObject); return; }
            if (_light != null) _light.intensity = _baseIntensity * (1f - t);
        }
    }
}
