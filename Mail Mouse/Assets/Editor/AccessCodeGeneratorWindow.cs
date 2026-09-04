using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AccessCodeGeneratorWindow : EditorWindow
{
    private string databaseUrl = "";
    private string generatedCode;
    private string statusMessage = "Ready.";

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

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(databaseUrl)))
        {
            if (GUILayout.Button("Generate and Add One Code", GUILayout.Height(36f)))
                GenerateCode();
        }

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
            Arguments = "manage_codes.py generate --count 1 --length 12 --product-id mail-mouse",
            WorkingDirectory = serverDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.EnvironmentVariables["DATABASE_URL"] = databaseUrl.Trim();

        try
        {
            using (Process process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd().Trim();
                string error = process.StandardError.ReadToEnd().Trim();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    statusMessage = string.IsNullOrEmpty(error) ? "Code generation failed." : error;
                    return;
                }

                generatedCode = output;
                EditorGUIUtility.systemCopyBuffer = generatedCode;
                statusMessage = "Code added to Supabase and copied to the clipboard.";
            }
        }
        catch (Exception exception)
        {
            statusMessage = $"Could not run code generator: {exception.Message}";
        }

        Repaint();
    }
}
