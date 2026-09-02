using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class VersionedBuildBuilder
{
    [MenuItem("Build/Build Versioned Game")]
    public static void BuildVersionedGame()
    {
        string buildsRoot = Path.Combine(Application.dataPath, "..", "Builds");
        Directory.CreateDirectory(buildsRoot);

        BuildVersionAutoIncrement.VersionData nextVersion = BuildVersionAutoIncrement.GetNextVersion();
        string versionLabel = $"v{nextVersion.major}.{nextVersion.minor}.{nextVersion.patch}-b{nextVersion.build}";
        string buildRoot = Path.Combine(buildsRoot, versionLabel);
        Directory.CreateDirectory(buildRoot);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray(),
            locationPathName = GetLocationPathName(buildRoot),
            target = EditorUserBuildSettings.activeBuildTarget,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded. Output: {buildRoot}");
        }
        else
        {
            Debug.LogError($"Build failed: {report.summary.result}");
        }
    }

    private static string GetLocationPathName(string buildRoot)
    {
        switch (EditorUserBuildSettings.activeBuildTarget)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return Path.Combine(buildRoot, "MailMouse.exe");
            case BuildTarget.StandaloneOSX:
                return Path.Combine(buildRoot, "MailMouse.app");
            case BuildTarget.StandaloneLinux64:
                return Path.Combine(buildRoot, "MailMouse.x86_64");
            default:
                return Path.Combine(buildRoot, "MailMouse");
        }
    }
}
