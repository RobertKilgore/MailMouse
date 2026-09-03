using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AccessCodePrompt : MonoBehaviour
{
    [Header("Scene UI")]
    [SerializeField] private TMP_InputField accessCodeInput;
    [SerializeField] private TMP_Text messageText;

    [Header("Messages")]
    [SerializeField] private string codeRequiredMessage = "Enter an access code.";
    [SerializeField] private string checkingMessage = "Checking access code...";
    [SerializeField] private string codeNotFoundMessage = "That access code does not exist.";
    [SerializeField] private string codeDeactivatedMessage = "That access code has expired.";
    [SerializeField] private string invalidCodeMessage = "That access code is not valid.";
    [SerializeField] private string connectionErrorMessage = "Could not verify the access code. Check your connection and try again.";

    public void SubmitAccessCode()
    {
        if (AccessControlManager.Instance == null)
        {
            ShowMessage(connectionErrorMessage);
            return;
        }

        string code = accessCodeInput == null ? string.Empty : accessCodeInput.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            ShowMessage(codeRequiredMessage);
            return;
        }

        ShowMessage(checkingMessage);
        AccessControlManager.Instance.SubmitAccessCode(code, HandleValidationResult);
    }

    private void HandleValidationResult(AccessCodeValidationResult result)
    {

        switch (result)
        {
            case AccessCodeValidationResult.Valid:
                SceneFlowManager.GetOrCreateInstance().LoadStartScene();
                return;
            case AccessCodeValidationResult.NotFound:
                ShowMessage(codeNotFoundMessage);
                return;
            case AccessCodeValidationResult.Deactivated:
                ShowMessage(codeDeactivatedMessage);
                return;
            case AccessCodeValidationResult.ConnectionError:
                ShowMessage(connectionErrorMessage);
                return;
            default:
                ShowMessage(invalidCodeMessage);
                return;
        }
    }


    private void ShowMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }
}
