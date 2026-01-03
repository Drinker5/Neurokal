using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;

namespace Components;

public class Input : MonoBehaviour
{
    public Button? SubmitButton;
    public TMP_InputField? InputComponent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (InputComponent != null)
            InputComponent.onSubmit.AddListener(OnSubmit);
    }

    private void OnSubmit(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        if (SubmitButton == null || InputComponent == null)
            return;

        SubmitButton.onClick.Invoke();
        InputComponent.text = string.Empty;
    }
}