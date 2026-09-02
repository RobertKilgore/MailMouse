using UnityEngine;

namespace MailMouse.UI
{
    public class OpenExternalLink : MonoBehaviour
    {
        [SerializeField] private string url = "https://example.com";

        public void Open()
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogWarning("OpenExternalLink URL is empty.");
                return;
            }

            Application.OpenURL(url);
        }

        public void SetUrl(string newUrl)
        {
            url = newUrl;
        }
    }
}
