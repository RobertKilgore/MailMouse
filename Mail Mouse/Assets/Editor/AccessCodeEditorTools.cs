using System.IO;
using UnityEditor;
using UnityEngine;

public static class AccessCodeEditorTools
{
    private const string AccessCodeFileName = "access-code.json";

    [MenuItem("Access Control/Clear Saved Access Code", priority = 1)]
    private static void ClearSavedAccessCode()
    {
        if (!EditorUtility.DisplayDialog(
                "Clear saved access code?",
                "The next game boot will require access-code validation again.",
                "Clear code",
                "Cancel"))
            return;

        string filePath = Path.Combine(Application.persistentDataPath, AccessCodeFileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"Access control: cleared saved access code at '{filePath}'.");
        }
        else
        {
            Debug.Log("Access control: no saved access code was found.");
        }
    }

    [MenuItem("Access Control/Clear Saved Access Code", true)]
    private static bool ValidateClearSavedAccessCode()
    {
        return !EditorApplication.isPlaying;
    }
}
