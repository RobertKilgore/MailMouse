using System.IO;
using System.Linq;
using DiagnosticsProcess = System.Diagnostics.Process;
using DiagnosticsProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class VersionedBuildBuilder
{
    [MenuItem("Build/Build Versioned Game")]
    public static void BuildVersionedGame()
    {
        BuildVersionedGame(false);
    }

    [MenuItem("Build/Build Versioned Game and Push")]
    public static void BuildVersionedGameAndPush()
    {
        if (!EditorUtility.DisplayDialog(
                "Build and push?",
                "This will build the game, commit the updated build version, and push the commit to the current Git branch.",
                "Build and push",
                "Cancel"))
            return;

        BuildVersionedGame(true);
    }

    private static void BuildVersionedGame(bool pushAfterBuild)
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
            if (pushAfterBuild)
                CommitAndPushVersion(versionLabel);
        }
        else
        {
            Debug.LogError($"Build failed: {report.summary.result}");
        }
    }

    private static void CommitAndPushVersion(string versionLabel)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string versionFile = "Assets/Resources/build-version.json";

        if (!RunGit(projectRoot, $"add -- \"{versionFile}\"", out string addError))
        {
            Debug.LogError($"Build succeeded, but Git staging failed: {addError}");
            return;
        }

        if (!RunGit(projectRoot, $"commit -m \"Build {versionLabel}\"", out string commitError))
        {
            Debug.LogError($"Build succeeded, but Git commit failed: {commitError}");
            return;
        }

        if (!RunGit(projectRoot, "push", out string pushError))
        {
            Debug.LogError($"Build and commit succeeded, but Git push failed: {pushError}");
            return;
        }

        Debug.Log($"Git commit and push completed for {versionLabel}.");
    }

    private static bool RunGit(string workingDirectory, string arguments, out string error)
    {
        DiagnosticsProcessStartInfo startInfo = new DiagnosticsProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (DiagnosticsProcess process = DiagnosticsProcess.Start(startInfo))
        {
            string output = process.StandardOutput.ReadToEnd().Trim();
            error = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit();

            if (process.ExitCode == 0)
                return true;

            if (string.IsNullOrEmpty(error))
                error = output;
            return false;
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
