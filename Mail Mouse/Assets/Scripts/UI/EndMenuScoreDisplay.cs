using System;
using System.IO;
using TMPro;
using UnityEngine;

public class EndMenuScoreDisplay : MonoBehaviour
{
    [Header("Score Text Fields")]
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI lastScoreText;

    [Header("Validator Reference")]
    [SerializeField] private InventoryValidationController validationController;

    private void Awake()
    {
        RefreshScores();
    }

    public void RefreshScores()
    {
        var saveData = LoadScoresFromDisk();

        if (highScoreText != null)
            highScoreText.text = FormatScore(saveData.highestScore);

        if (lastScoreText != null)
            lastScoreText.text = FormatScore(saveData.mostRecentScore);

        Debug.Log($"EndMenuScoreDisplay loaded scores: highest={saveData.highestScore}, recent={saveData.mostRecentScore}");
    }

    private InventoryValidationController.ScoreSaveData LoadScoresFromDisk()
    {
        string path = Path.Combine(Application.persistentDataPath, "inventory-validation-scores.json");

        if (!File.Exists(path))
        {
            Debug.LogWarning($"EndMenuScoreDisplay could not find save file at {path}");
            return new InventoryValidationController.ScoreSaveData();
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<InventoryValidationController.ScoreSaveData>(json)
                   ?? new InventoryValidationController.ScoreSaveData();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"EndMenuScoreDisplay failed to read save file at {path}: {ex.Message}");
            return new InventoryValidationController.ScoreSaveData();
        }
    }

    private string FormatScore(float score)
    {
        return score.ToString("0");
    }
}
