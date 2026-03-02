using UnityEngine;
using System.Collections;
using TMPro;

namespace CubeTower.View
{
    public class MessageView : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;

        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private float fadeDuration = 0.5f;

        private CanvasGroup _canvasGroup;
        private Coroutine _currentCoroutine;

        private void Awake()
        {
            _canvasGroup = messageText.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = messageText.gameObject.AddComponent<CanvasGroup>();
            }
            _canvasGroup.alpha = 0f;
        }

        public void Show(string message)
        {
            if (_currentCoroutine != null)
                StopCoroutine(_currentCoroutine);

            messageText.text = message;
            _currentCoroutine = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                yield return null;
            }
            _canvasGroup.alpha = 1f;

            // Wait
            yield return new WaitForSeconds(displayDuration);

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }
            _canvasGroup.alpha = 0f;
        }
    }
}
