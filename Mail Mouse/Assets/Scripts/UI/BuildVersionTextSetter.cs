using TMPro;
using UnityEngine;

namespace MailMouse.UI
{
    public class BuildVersionTextSetter : MonoBehaviour
    {
        [SerializeField] private TMP_Text targetText;
        [SerializeField] private string prefix = "Version ";

        private void Awake()
        {
            if (targetText == null)
            {
                targetText = GetComponent<TMP_Text>();
            }

            if (targetText == null)
            {
                Debug.LogWarning("BuildVersionTextSetter requires a TMP_Text component.");
                return;
            }

            TextAsset versionAsset = Resources.Load<TextAsset>("build-version");
            if (versionAsset == null)
            {
                targetText.text = prefix + "Unavailable";
                return;
            }

            VersionData versionData = JsonUtility.FromJson<VersionData>(versionAsset.text);
            if (versionData == null)
            {
                targetText.text = prefix + "Unavailable";
                return;
            }

            targetText.text = prefix + $"{versionData.major}.{versionData.minor}.{versionData.patch} (Build {versionData.build})";
        }

        [System.Serializable]
        private class VersionData
        {
            public int major = 0;
            public int minor = 0;
            public int patch = 0;
            public int build = 0;
        }
    }
}
