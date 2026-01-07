using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LLMUnity;
using UnityEditor;

namespace Components;

public sealed class Neuro : MonoBehaviour
{
    public TextMeshProUGUI? InputComponent;
    public Button? Button;
    public TextMeshProUGUI? FeedComponent;
    public LLMCharacter? llmCharacter;

    void Start()
    {
        if (Button == null)
            return;


        Button.onClick.AddListener(ButtonClicked);
        EditorApplication.playModeStateChanged += HandleOnPlayModeChanged;
    }

    private async void ButtonClicked()
    {
        try
        {
            if (InputComponent == null || FeedComponent == null || llmCharacter == null)
                return;
        
            llmCharacter.CancelRequests();
            var sb = new StringBuilder();
            sb.Append("<color=green>");
            sb.Append(InputComponent.text);
            sb.Append("</color>");
            sb.Append(Environment.NewLine);
            FeedComponent.text = sb.ToString();
            
            await llmCharacter.Chat(InputComponent.text, reply =>
            {
                FeedComponent.text = reply;
            },
            () =>
            {
                Debug.LogError("complete");
            });
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    void HandleOnPlayModeChanged(PlayModeStateChange mode)
    {
        // This method is run whenever the playmode state is changed.

        if (mode == PlayModeStateChange.ExitingPlayMode)
        {
            if (llmCharacter != null)
                llmCharacter.CancelRequests();
        }
    }
}