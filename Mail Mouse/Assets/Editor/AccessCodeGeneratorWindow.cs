using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class AccessCodeGeneratorWindow : EditorWindow
{
    private string databaseUrl = "";
    private string generatedCode;
    private string statusMessage = "Ready.";
    private int codeCount = 1;
    private Process activeProcess;
    private readonly StringBuilder outputBuffer = new StringBuilder();
    private readonly StringBuilder errorBuffer = new StringBuilder();

    [MenuItem("Access Control/Generate One Access Code")]
    private static void OpenWindow()
    {
        GetWindow<AccessCodeGeneratorWindow>("Access Code Generator");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Access Code Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Enter the private Supabase database URL. It is kept only in this editor window and is never saved in the Unity project.", MessageType.Info);

        databaseUrl = EditorGUILayout.PasswordField("Supabase Database URL", databaseUrl);
        codeCount = Mathf.Clamp(EditorGUILayout.IntField("Number of Codes", codeCount), 1, 1000);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(databaseUrl) || activeProcess != null))
        {
            if (GUILayout.Button("Generate and Add One Code", GUILayout.Height(36f)))
                GenerateCode();
        }

        if (activeProcess != null)
            EditorGUILayout.HelpBox("Generating code and contacting Supabase...", MessageType.Info);

        if (!string.IsNullOrEmpty(generatedCode))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generated Code", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(generatedCode, EditorStyles.textField, GUILayout.Height(20f));
            if (GUILayout.Button("Copy Code"))
                EditorGUIUtility.systemCopyBuffer = generatedCode;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(statusMessage, EditorStyles.wordWrappedLabel);
    }

    private void GenerateCode()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string serverDirectory = Path.Combine(projectRoot, "server");
        string scriptPath = Path.Combine(serverDirectory, "manage_codes.py");
        string pythonPath = Path.Combine(projectRoot, ".venv", "Scripts", "python.exe");

        if (!File.Exists(pythonPath))
        {
            statusMessage = "Could not find .venv\\Scripts\\python.exe.";
            return;
        }

        if (!File.Exists(scriptPath))
        {
            statusMessage = "Could not find server\\manage_codes.py.";
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = $"manage_codes.py generate --count {codeCount} --length 12 --product-id mail-mouse",
            WorkingDirectory = serverDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.EnvironmentVariables["DATABASE_URL"] = databaseUrl.Trim();

        try
        {
            outputBuffer.Clear();
            errorBuffer.Clear();
            activeProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            activeProcess.OutputDataReceived += HandleOutputData;
            activeProcess.ErrorDataReceived += HandleErrorData;
            activeProcess.Exited += HandleProcessExited;
            activeProcess.Start();
            activeProcess.BeginOutputReadLine();
            activeProcess.BeginErrorReadLine();
            statusMessage = "Contacting Supabase...";
        }
        catch (Exception exception)
        {
            activeProcess?.Dispose();
            activeProcess = null;
            statusMessage = $"Could not run code generator: {exception.Message}";
            UnityEngine.Debug.LogError($"Access code generation failed: {exception}");
        }

        Repaint();
    }

    private void HandleOutputData(object sender, DataReceivedEventArgs arguments)
    {
        if (!string.IsNullOrEmpty(arguments.Data))
            outputBuffer.AppendLine(arguments.Data);
    }

    private void HandleErrorData(object sender, DataReceivedEventArgs arguments)
    {
        if (!string.IsNullOrEmpty(arguments.Data))
            errorBuffer.AppendLine(arguments.Data);
    }

    private void HandleProcessExited(object sender, EventArgs arguments)
    {
        EditorApplication.delayCall += CompleteProcess;
    }

    private void CompleteProcess()
    {
        if (activeProcess == null)
            return;

        int exitCode = activeProcess.ExitCode;
        string output = outputBuffer.ToString().Trim();
        string error = errorBuffer.ToString().Trim();
        activeProcess.Dispose();
        activeProcess = null;

        if (exitCode != 0)
        {
            statusMessage = string.IsNullOrEmpty(error) ? "Code generation failed." : error;
            UnityEngine.Debug.LogError($"Access code generation failed (exit code {exitCode}): {statusMessage}");
        }
        else
        {
            generatedCode = output;
            EditorGUIUtility.systemCopyBuffer = generatedCode;
            statusMessage = "Code added to Supabase and copied to the clipboard.";
        }

        Repaint();
    }

    private void OnDisable()
    {
        if (activeProcess == null)
            return;

        activeProcess.Exited -= HandleProcessExited;
        activeProcess.OutputDataReceived -= HandleOutputData;
        activeProcess.ErrorDataReceived -= HandleErrorData;
        if (!activeProcess.HasExited)
            activeProcess.Kill();
        activeProcess.Dispose();
        activeProcess = null;
    }
}
