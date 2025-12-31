using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LLama.Common;
using LLama;

namespace Components
{
    public sealed class Neuro : MonoBehaviour
    {
        public TextMeshProUGUI InputComponent;
        public Button Button;
        public TextMeshProUGUI FeedComponent;
        public string ModelPath = "Assets/LLM/Qwen3-1.7B-Q8_0.gguf";

        private readonly ModelParams parameters;
        private CancellationTokenSource cts;
        private readonly ChatHistory chatHistory = new ChatHistory();
        private const string stopSlovo = "User:";

        public Neuro()
        {
            parameters = new ModelParams(ModelPath)
            {
                ContextSize = 1024, // The longest length of chat as memory.
                GpuLayerCount = 5 // How many layers to offload to GPU. Please adjust it according to your GPU memory.
            };

            cts = new CancellationTokenSource();
            chatHistory.AddMessage(AuthorRole.System,
                "Запись диалога, в котором Пользователь взаимодействует с ассистентом по имени Боб. Боб услужлив, добр, честен, умеет писать и всегда отвечает на запросы Пользователя незамедлительно и точно.");
        }

        void Start()
        {
            if (Button == null)
                return;

            cts.Cancel();
            cts.Dispose();
            cts = new CancellationTokenSource();
            Button.onClick.AddListener(() => ButtonClicked(cts.Token));
        }

        private async void ButtonClicked(CancellationToken token = default)
        {
            if (InputComponent == null || FeedComponent == null)
                return;

            var sb = new StringBuilder();
            FeedComponent.text = string.Empty;

            using var model = LLamaWeights.LoadFromFile(parameters);
            using var context = model.CreateContext(parameters);
            var executor = new InteractiveExecutor(context);

            ChatSession session = new(executor, chatHistory);
            InferenceParams inferenceParams = new InferenceParams()
            {
                MaxTokens = 256, // No more than 256 tokens should appear in answer. Remove it if antiprompt is enough for control.
                AntiPrompts = new List<string> { stopSlovo } // Stop generation once antiprompts appear.
            };

            sb.Append("<color=yellow>User: </color>");
            sb.Append("<color=green>");
            sb.Append(InputComponent.text);
            sb.Append("\n</color>");
            FeedComponent.text = sb.ToString();

            await foreach (var text in session.ChatAsync(
                               new ChatHistory.Message(AuthorRole.User, InputComponent.text),
                               inferenceParams, token))
            {
                if (string.Equals(text, stopSlovo, StringComparison.OrdinalIgnoreCase))
                    continue;

                sb.Append($"<color=white>{text}</color>");
                FeedComponent.text = sb.ToString();
            }
        }
    }
}