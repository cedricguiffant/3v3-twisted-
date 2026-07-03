using UnityEngine;

namespace Twisted3v3.VFX
{
    /// <summary>
    /// Herbe procédurale : un mesh de touffe PARTAGÉ (brins triangulaires effilés,
    /// dégradé de couleur cuit en vertex colors) + une factory. La teinte par touffe
    /// passe par MaterialPropertyBlock — un seul matériau partagé, aucune fuite.
    /// Shader Sprites/Default : double face (Cull Off) et sans éclairage, idéal
    /// pour des brins fins lisibles en vue MOBA.
    /// </summary>
    public static class Grass
    {
        private const int Blades = 8;

        private static Mesh _tuftMesh;
        private static Material _material;

        /// <summary>Crée une touffe d'herbe (hauteur en unités monde, teinte multipliée).</summary>
        public static GameObject CreateTuft(Vector3 position, float height, Color tint,
            Transform parent = null, float seed = 0f)
        {
            var go = new GameObject("Grass");
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, seed * 360f, 0f);
            go.transform.localScale = new Vector3(0.8f + seed * 0.4f, height, 0.8f + (1f - seed) * 0.4f);

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = TuftMesh();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = SharedMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", tint);
            renderer.SetPropertyBlock(mpb);
            return go;
        }

        /// <summary>Mesh partagé : brins en éventail, base sombre → pointe claire.</summary>
        private static Mesh TuftMesh()
        {
            if (_tuftMesh != null) return _tuftMesh;

            var verts = new Vector3[Blades * 3];
            var colors = new Color[Blades * 3];
            var tris = new int[Blades * 3];

            Color baseColor = new(0.10f, 0.22f, 0.10f);
            Color tipColor = new(0.42f, 0.68f, 0.32f);

            for (int i = 0; i < Blades; i++)
            {
                float angle = (i / (float)Blades) * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var side = Vector3.Cross(Vector3.up, dir) * 0.05f;
                Vector3 root = dir * 0.10f;

                int v = i * 3;
                verts[v] = root - side;                       // pied gauche
                verts[v + 1] = root + side;                   // pied droit
                verts[v + 2] = root + dir * 0.28f + Vector3.up; // pointe penchée vers l'extérieur
                colors[v] = baseColor;
                colors[v + 1] = baseColor;
                colors[v + 2] = tipColor;
                tris[v] = v; tris[v + 1] = v + 2; tris[v + 2] = v + 1;
            }

            _tuftMesh = new Mesh { name = "GrassTuft", vertices = verts, colors = colors, triangles = tris };
            _tuftMesh.RecalculateBounds();
            return _tuftMesh;
        }

        private static Material SharedMaterial()
        {
            if (_material == null)
                _material = new Material(Shader.Find("Sprites/Default"));
            return _material;
        }
    }
}
