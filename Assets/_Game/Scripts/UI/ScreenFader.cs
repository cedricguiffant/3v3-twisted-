using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Twisted3v3.UI
{
    /// <summary>
    /// Fondu plein écran entre les scènes. Singleton persistant : <c>FadeAndLoad</c>
    /// fond au noir puis charge la scène ; à chaque chargement, fond depuis le noir.
    /// Auto-construit son Canvas (au-dessus de tout).
    /// </summary>
    public sealed class ScreenFader : MonoBehaviour
    {
        private static ScreenFader _instance;

        public static ScreenFader Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GameObject("ScreenFader").AddComponent<ScreenFader>();
                return _instance;
            }
        }

        [SerializeField] private float _fadeDuration = 0.6f;
        private CanvasGroup _group;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // au-dessus de tout
            _group = gameObject.AddComponent<CanvasGroup>();

            var imgGO = new GameObject("Black", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)imgGO.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            imgGO.GetComponent<Image>().color = Color.black;

            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => StartCoroutine(Fade(1f, 0f));

        public void FadeAndLoad(string sceneName) => StartCoroutine(FadeOutLoad(sceneName));

        private IEnumerator FadeOutLoad(string sceneName)
        {
            _group.blocksRaycasts = true;
            yield return Fade(0f, 1f);
            SceneManager.LoadScene(sceneName); // déclenche OnSceneLoaded → fondu d'entrée
        }

        private IEnumerator Fade(float from, float to)
        {
            float t = 0f;
            _group.alpha = from;
            while (t < _fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(from, to, t / _fadeDuration);
                yield return null;
            }
            _group.alpha = to;
            _group.blocksRaycasts = to > 0.5f;
        }
    }
}
