using UnityEngine;

namespace Twisted3v3.Audio
{
    /// <summary>
    /// Lecteur de musique d'ambiance. Une seule piste → lecture en boucle ;
    /// plusieurs pistes → enchaînement séquentiel (ou aléatoire) en boucle.
    /// Crée son propre AudioSource ; il suffit de poser le composant et de
    /// renseigner la playlist.
    /// </summary>
    public sealed class MusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip[] _playlist;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.6f;
        [SerializeField] private bool _shuffle = false;
        [Tooltip("Garder la musique en passant d'une scène à l'autre.")]
        [SerializeField] private bool _persistAcrossScenes = false;

        private AudioSource _source;
        private int _index;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.volume = _volume;
            _source.spatialBlend = 0f; // 2D
            if (_persistAcrossScenes) DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_playlist == null || _playlist.Length == 0) return;

            // Piste unique → boucle native de l'AudioSource (ex: Moonforge Rift en jeu).
            if (_playlist.Length == 1)
            {
                _source.loop = true;
                _source.clip = _playlist[0];
                _source.Play();
                return;
            }

            _index = _shuffle ? Random.Range(0, _playlist.Length) : 0;
            PlayCurrent();
        }

        private void Update()
        {
            // Enchaînement quand la piste courante se termine (playlist multi-pistes).
            if (!_source.loop && !_source.isPlaying && _source.clip != null)
                Advance();
        }

        private void PlayCurrent()
        {
            _source.clip = _playlist[_index];
            _source.Play();
        }

        private void Advance()
        {
            _index = _shuffle
                ? Random.Range(0, _playlist.Length)
                : (_index + 1) % _playlist.Length;
            PlayCurrent();
        }
    }
}
