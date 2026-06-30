using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

namespace Twisted3v3.UI
{
    /// <summary>
    /// Joue la cinématique d'intro (VideoClip) au lancement, plein écran via la
    /// caméra, puis charge la scène suivante à la fin de la vidéo. Skippable
    /// (clic / touche). Crée son propre VideoPlayer.
    /// </summary>
    public sealed class IntroCinematicPlayer : MonoBehaviour
    {
        [SerializeField] private VideoClip _clip;
        [SerializeField] private string _nextScene = "01_MainMenu";
        [SerializeField] private bool _skippable = true;

        private VideoPlayer _video;
        private bool _transitioning;

        private void Start()
        {
            var cam = Camera.main;
            _video = gameObject.AddComponent<VideoPlayer>();
            _video.playOnAwake = false;
            _video.isLooping = false;
            _video.renderMode = VideoRenderMode.CameraNearPlane;
            _video.targetCamera = cam;
            _video.targetCameraAlpha = 1f;
            _video.audioOutputMode = VideoAudioOutputMode.Direct; // son de la vidéo
            _video.clip = _clip;
            _video.loopPointReached += OnVideoEnd;

            if (_clip != null) _video.Play();
            else LoadNext(); // pas de clip → on saute directement
        }

        private void Update()
        {
            if (_skippable && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
                LoadNext();
        }

        private void OnVideoEnd(VideoPlayer vp) => LoadNext();

        private void LoadNext()
        {
            if (_transitioning) return;
            _transitioning = true;
            ScreenFader.Instance.FadeAndLoad(_nextScene); // transition en fondu
        }
    }
}
