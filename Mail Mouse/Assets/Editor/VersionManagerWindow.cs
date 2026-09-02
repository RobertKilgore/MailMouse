using System.IO;
using UnityEditor;
using UnityEngine;

public class VersionManagerWindow : EditorWindow
{
    private const string VersionFilePath = "Assets/Resources/build-version.json";

    private int major;
    private int minor;
    private int patch;
    private int build;

    [MenuItem("Build/Version Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<VersionManagerWindow>("Version Manager");
        window.LoadVersion();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Version Settings", EditorStyles.boldLabel);

        major = EditorGUILayout.IntField("Major", major);
        minor = EditorGUILayout.IntField("Minor", minor);
        patch = EditorGUILayout.IntField("Patch", patch);
        build = EditorGUILayout.IntField("Build", build);

        EditorGUILayout.Space();

        if (GUILayout.Button("Save Version"))
        {
            SaveVersion();
        }

        if (GUILayout.Button("Increment Build"))
        {
            build += 1;
            SaveVersion();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Reset Build Number"))
        {
            if (EditorUtility.DisplayDialog(
                "Reset build number?",
                "This will reset the build counter to 0. Continue?",
                "Yes, reset it",
                "Cancel"))
            {
                build = 0;
                SaveVersion();
                Debug.Log("Build number reset to 0.");
            }
        }
    }

    private void LoadVersion()
    {
        if (!File.Exists(VersionFilePath))
        {
            major = 1;
            minor = 0;
            patch = 0;
            build = 0;
            SaveVersion();
            return;
        }

        string json = File.ReadAllText(VersionFilePath);
        VersionData data = JsonUtility.FromJson<VersionData>(json);

        if (data == null)
        {
            major = 1;
            minor = 0;
            patch = 0;
            build = 0;
            return;
        }

        major = data.major;
        minor = data.minor;
        patch = data.patch;
        build = data.build;
    }

    private void SaveVersion()
    {
        VersionData data = new VersionData
        {
            major = major,
            minor = minor,
            patch = patch,
            build = build
        };

        string json = JsonUtility.ToJson(data, true);
        string directory = Path.GetDirectoryName(VersionFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(VersionFilePath, json);
        AssetDatabase.Refresh();
    }

    private class VersionData
    {
        public int major;
        public int minor;
        public int patch;
        public int build;
    }
}
