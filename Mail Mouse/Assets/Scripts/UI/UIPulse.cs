using UnityEngine;

namespace MailMouse.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class UIPulse : MonoBehaviour
    {
        [Header("Pulse Settings")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float minScale = 0.92f;
        [SerializeField] private float maxScale = 1.08f;

        [Header("Optional Alpha Pulse")]
        [SerializeField] private bool pulseAlpha = false;
        [SerializeField] private float minAlpha = 0.5f;
        [SerializeField] private float maxAlpha = 1f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector3 originalScale;
        private float originalAlpha;
        private bool isPulsing = true;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            originalScale = rectTransform.localScale;

            if (canvasGroup != null)
            {
                originalAlpha = canvasGroup.alpha;
            }
        }

        private void Update()
        {
            if (!isPulsing)
            {
                return;
            }

            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float scaleValue = Mathf.Lerp(minScale, maxScale, t);

            rectTransform.localScale = originalScale * scaleValue;

            if (pulseAlpha && canvasGroup != null)
            {
                float alphaValue = Mathf.Lerp(minAlpha, maxAlpha, t);
                canvasGroup.alpha = alphaValue;
            }
        }

        public void StopPulse()
        {
            isPulsing = false;
            rectTransform.localScale = originalScale;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = originalAlpha;
            }
        }

        public void StartPulse()
        {
            isPulsing = true;
        }
    }
}
