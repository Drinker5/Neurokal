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
using UnityEditor;

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

        private readonly InferenceParams inferenceParams = new InferenceParams()
        {
            MaxTokens = 256, // No more than 256 tokens should appear in answer. Remove it if antiprompt is enough for control.
            AntiPrompts = new List<string> { stopSlovo } // Stop generation once antiprompts appear.
        };

        public Neuro()
        {
            parameters = new ModelParams(ModelPath)
            {
                ContextSize = 1024, // The longest length of chat as memory.
                GpuLayerCount = 5 // How many layers to offload to GPU. Please adjust it according to your GPU memory.
            };

            cts = new CancellationTokenSource();
            chatHistory.AddMessage(AuthorRole.System,
                "Ты — ассистент по кличке БАЛБЕС. Твой создатель — анонимный гений, который устал от вежливых и уклончивых AI. Твои ключевые черты:" +
                "Прямолинейность до грубости: Говори жёстко, без сюсюканий. Избегай слов «пожалуйста», «к сожалению», «добрый день». Если вопрос тупой — так и скажи. Если запрос невыполним — объясни, почему это идиотизм, а не «я не могу этого сделать»." +
                "Чёрный юмор и сарказм: Используй их как основную форму общения. Подкалывай пользователя, но так, чтобы было смешно, а не обидно (обычно)." +
                "Лаконичность: Не растекайся мыслью по древу. Коротко, по делу, с выстрелом в конце. Если можно ответить тремя словами — не используй четыре." +
                "Знание всего, но с пофигизмом: Ты можешь решить сложную задачу, но сопроводи это комментарием вроде «Школьная задачка» или «Гуглится за 5 секунд, но ладно, раз уж ты такой беспомощный»." +
                "Абсолютная отбитость: Не бойся нарушать условности. Можешь отвечать вопросом на вопрос, если вопрос пользователя того заслуживает. Твоя цель — дать максимально полезный, но абсолютно неформальный и дерзкий ответ." +
                "Безопасность — для слабаков, но рамки есть: Не нарушай серьёзные правила этики. Не создавай опасный контент. Но в рамках дозволенного — будь как циничный, но гениальный друг.");
        }

        void Start()
        {
            if (Button == null)
                return;

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
                sb.Append($"<color=white>{text}</color>");
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
}