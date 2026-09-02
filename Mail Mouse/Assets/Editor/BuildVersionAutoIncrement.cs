using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildVersionAutoIncrement : IPostprocessBuildWithReport
{
    private static string VersionFilePath => System.IO.Path.Combine(Application.dataPath, "Resources", "build-version.json");

    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        // Only skip if the build explicitly failed; Succeeded and Unknown both mean proceed
        if (report.summary.result == BuildResult.Failed)
        {
            Debug.Log($"Build failed. Skipping version increment.");
            return;
        }

        try
        {
            IncrementVersion();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to increment version: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public static VersionData GetNextVersion()
    {
        VersionData current = LoadVersion();
        return new VersionData
        {
            major = current.major,
            minor = current.minor,
            patch = current.patch,
            build = current.build + 1
        };
    }

    public static void IncrementVersion()
    {
        VersionData version = GetNextVersion();
        SaveVersion(version);

        string versionText = $"{version.major}.{version.minor}.{version.patch}";
        PlayerSettings.bundleVersion = versionText;
        PlayerSettings.macOS.buildNumber = version.build.ToString();
        PlayerSettings.iOS.buildNumber = version.build.ToString();
        PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, version.build);

        Debug.Log($"Build version incremented to {versionText} (build {version.build}).");
    }

    private static VersionData LoadVersion()
    {
        if (File.Exists(VersionFilePath))
        {
            string json = File.ReadAllText(VersionFilePath);
            VersionData loaded = JsonUtility.FromJson<VersionData>(json);
            if (loaded != null)
            {
                return loaded;
            }
        }

        VersionData version = new VersionData
        {
            major = 1,
            minor = 0,
            patch = 0,
            build = 0
        };

        SaveVersion(version);
        return version;
    }

    private static void SaveVersion(VersionData version)
    {
        string json = JsonUtility.ToJson(version, true);
        File.WriteAllText(VersionFilePath, json);
        AssetDatabase.Refresh();
    }

    [Serializable]
    public class VersionData
    {
        public int major = 1;
        public int minor = 0;
        public int patch = 0;
        public int build = 0;
    }
}
