using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public enum AccessCodeValidationResult
{
    Invalid,
    Valid,
    NotFound,
    Deactivated,
    ConnectionError
}

public class AccessControlManager : MonoBehaviour
{
    public static AccessControlManager Instance { get; private set; }

    [Header("Server Configuration")]
    [Tooltip("POST endpoint that accepts { accessCode, productId, buildVersion } and returns { valid: true }.")]
    [SerializeField] private string validationUrl = "";
    [SerializeField] private string productId = "mail-mouse";
    [SerializeField] private float requestTimeoutSeconds = 10f;

    private const string AccessCodeFileName = "access-code.json";
    private bool isChecking;
    private bool accessGranted;

    private string AccessCodeFilePath => Path.Combine(Application.persistentDataPath, AccessCodeFileName);

    public static AccessControlManager GetOrCreateInstance()
    {
        if (Instance != null)
            return Instance;

        Instance = FindFirstObjectByType<AccessControlManager>(FindObjectsInactive.Include);
        if (Instance != null)
            return Instance;

        GameObject managerObject = new GameObject("AccessControlManager");
        Instance = managerObject.AddComponent<AccessControlManager>();
        DontDestroyOnLoad(managerObject);
        return Instance;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        TextAsset configurationAsset = Resources.Load<TextAsset>("access-control-config");
        if (configurationAsset == null || string.IsNullOrWhiteSpace(configurationAsset.text))
        {
            Debug.LogError("AccessControlManager: Resources/access-control-config.json was not found or is empty.");
            return;
        }

        try
        {
            AccessControlConfiguration configuration = JsonUtility.FromJson<AccessControlConfiguration>(configurationAsset.text);
            if (configuration != null)
            {
                validationUrl = configuration.validationUrl;
                productId = string.IsNullOrWhiteSpace(configuration.productId) ? productId : configuration.productId;
                Debug.Log($"AccessControlManager: loaded validation endpoint '{validationUrl}'.");
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"AccessControlManager: could not read access-control-config.json: {exception.Message}");
        }
    }

    public void ValidateAtBoot(Action<bool> result)
    {
        if (accessGranted)
        {
            result?.Invoke(true);
            return;
        }

        StartCoroutine(ValidateSavedAccessCode(result));
    }

    public void SubmitAccessCode(string code, Action<AccessCodeValidationResult> result = null)
    {
        if (isChecking)
            return;

        string normalizedCode = code == null ? string.Empty : code.Trim();
        if (normalizedCode.Length == 0)
        {
            result?.Invoke(AccessCodeValidationResult.Invalid);
            return;
        }

        StartCoroutine(ValidateCode(normalizedCode, null, result));
    }

    private IEnumerator ValidateSavedAccessCode(Action<bool> result = null)
    {
        yield return ValidateCode(ReadSavedAccessCode(), result);
    }

    private IEnumerator ValidateCode(string code, Action<bool> bootResult = null, Action<AccessCodeValidationResult> submitResult = null)
    {
        if (isChecking)
            yield break;

        isChecking = true;

        AccessCodeValidationResult validationResult = AccessCodeValidationResult.Invalid;
        string error = string.Empty;

        if (string.IsNullOrWhiteSpace(code))
        {
            error = "An access code is required to continue.";
            validationResult = AccessCodeValidationResult.NotFound;
        }
        else if (string.IsNullOrWhiteSpace(validationUrl))
        {
            error = "Access validation is not configured.";
            Debug.LogError("AccessControlManager: validation URL is empty; access is blocked.");
            validationResult = AccessCodeValidationResult.ConnectionError;
        }
        else
        {
            string requestBody = $"{{\"accessCode\":\"{EscapeJson(code)}\",\"productId\":\"{EscapeJson(productId)}\",\"buildVersion\":\"{EscapeJson(Application.version)}\"}}";
            using (UnityWebRequest request = new UnityWebRequest(validationUrl, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestBody));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = Mathf.CeilToInt(Mathf.Max(1f, requestTimeoutSeconds));

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    ValidationResponse response = ParseValidationResponse(request.downloadHandler.text);
                    validationResult = response.valid ? AccessCodeValidationResult.Valid : ParseStatus(response.status);
                    if (validationResult != AccessCodeValidationResult.Valid)
                        error = "That access code is not valid.";
                }
                else
                {
                    validationResult = request.responseCode == 404
                        ? AccessCodeValidationResult.NotFound
                        : request.responseCode == 410
                            ? AccessCodeValidationResult.Deactivated
                            : AccessCodeValidationResult.ConnectionError;
                    error = validationResult == AccessCodeValidationResult.NotFound
                        ? "That access code does not exist."
                        : validationResult == AccessCodeValidationResult.Deactivated
                            ? "That access code has expired."
                            : "Could not verify the access code. Check your connection and try again.";
                    Debug.LogWarning($"AccessControlManager: validation request failed: {request.error}");
                }
            }
        }

        bool valid = validationResult == AccessCodeValidationResult.Valid;
        accessGranted = valid;
        isChecking = false;

        if (valid)
        {
            SaveAccessCode(code.Trim());
        }

        if (!valid && !string.IsNullOrEmpty(error))
            Debug.LogWarning($"AccessControlManager: {error}");

        bootResult?.Invoke(valid);
        submitResult?.Invoke(validationResult);
    }

    private string ReadSavedAccessCode()
    {
        if (!File.Exists(AccessCodeFilePath))
            return string.Empty;

        try
        {
            SavedAccessCode savedAccessCode = JsonUtility.FromJson<SavedAccessCode>(File.ReadAllText(AccessCodeFilePath));
            return savedAccessCode?.accessCode?.Trim() ?? string.Empty;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"AccessControlManager: could not read {AccessCodeFileName}: {exception.Message}");
            return string.Empty;
        }
    }

    private void SaveAccessCode(string code)
    {
        try
        {
            string directory = Path.GetDirectoryName(AccessCodeFilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            SavedAccessCode savedAccessCode = new SavedAccessCode { accessCode = code };
            File.WriteAllText(AccessCodeFilePath, JsonUtility.ToJson(savedAccessCode));
        }
        catch (Exception exception)
        {
            Debug.LogError($"AccessControlManager: could not save {AccessCodeFileName}: {exception.Message}");
        }
    }

    private static ValidationResponse ParseValidationResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return new ValidationResponse();

        try
        {
            return JsonUtility.FromJson<ValidationResponse>(response);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"AccessControlManager: invalid validation response: {exception.Message}");
            return new ValidationResponse();
        }
    }

    private static AccessCodeValidationResult ParseStatus(string status)
    {
        if (string.Equals(status, "not_found", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "missing", StringComparison.OrdinalIgnoreCase))
            return AccessCodeValidationResult.NotFound;

        if (string.Equals(status, "deactivated", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "expired", StringComparison.OrdinalIgnoreCase))
            return AccessCodeValidationResult.Deactivated;

        return AccessCodeValidationResult.Invalid;
    }

    private static string EscapeJson(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    [Serializable]
    private struct ValidationResponse
    {
        public bool valid;
        public string status;
    }

    [Serializable]
    private class SavedAccessCode
    {
        public string accessCode;
    }

    [Serializable]
    private class AccessControlConfiguration
    {
        public string validationUrl;
        public string productId;
    }
}