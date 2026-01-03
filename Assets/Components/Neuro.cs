using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LLama.Common;
using LLama;
using LLama.Native;
using UnityEditor;

namespace Components;

public sealed class Neuro : MonoBehaviour
{
    public TextMeshProUGUI? InputComponent;
    public Button? Button;
    public TextMeshProUGUI? FeedComponent;
    public string ModelPath = "Assets/LLM/Qwen3-1.7B-Q8_0.gguf";
    public string Instruction = string.Empty;

    private readonly ModelParams parameters;
    private CancellationTokenSource cts = new CancellationTokenSource();
    private readonly ChatHistory chatHistory = new ChatHistory();
    private const string stopSlovo = "User:";

    private readonly InferenceParams inferenceParams = new InferenceParams()
    {
        MaxTokens = 256, // No more than 256 tokens should appear in answer. Remove it if antiprompt is enough for control.
        AntiPrompts = new List<string> { stopSlovo } // Stop generation once antiprompts appear.
    };

    public Neuro()
    {
        parameters = new ModelParams(ModelPath)
        {
            ContextSize = 1024,
            GpuLayerCount = 128,
            MainGpu = 0,
        };

        NativeLibraryConfig.All.DryRun(out var a, out var b);
        // NativeLibraryConfig.All.WithLogCallback(LLamaLogCallback);
        // NativeLibraryConfig.LLama.WithLogCallback(LLamaLogCallback);
        // NativeLibraryConfig.LLava.WithLogCallback(LLamaLogCallback);
    }

    void LLamaLogCallback(LLamaLogLevel level, string message)
    {
        Console.WriteLine(message);
    }

    void Start()
    {
        if (Button == null)
            return;

        if (!string.IsNullOrEmpty(Instruction))
            chatHistory.AddMessage(AuthorRole.System, Instruction);

        cts.Cancel();
        cts.Dispose();
        cts = new CancellationTokenSource();
        Button.onClick.AddListener(async () => await ButtonClicked(cts.Token));
        EditorApplication.playModeStateChanged += HandleOnPlayModeChanged;
    }

    private async Task ButtonClicked(CancellationToken token = default)
    {
        if (InputComponent == null || FeedComponent == null)
            return;

        var sb = new StringBuilder();
        sb.Append("<color=yellow>User: </color>");
        sb.Append("<color=green>");
        sb.Append(InputComponent.text);
        sb.Append("</color>");
        sb.Append(Environment.NewLine);
        FeedComponent.text = sb.ToString();

        using var model = LLamaWeights.LoadFromFile(parameters);
        using var context = model.CreateContext(parameters);
        var executor = new InteractiveExecutor(context);
        ChatSession session = new(executor, chatHistory);
        await foreach (var text in session.ChatAsync(
                           new ChatHistory.Message(AuthorRole.User, InputComponent.text),
                           inferenceParams, token))
        {
            sb.Append("<color=white>");
            sb.Append(text);
            sb.Append("</color>");
            FeedComponent.text = sb.ToString();
        }
    }

    void HandleOnPlayModeChanged(PlayModeStateChange mode)
    {
        // This method is run whenever the playmode state is changed.

        if (mode == PlayModeStateChange.ExitingPlayMode)
        {
            cts.Cancel();
        }
    }
}