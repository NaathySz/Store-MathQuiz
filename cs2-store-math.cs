using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using StoreApi;
using System.Text.Json.Serialization;

namespace Store_MathQuiz
{
    public class Store_MathQuizConfig : BasePluginConfig
    {
        [JsonPropertyName("question_interval_seconds")]
        public int QuestionIntervalSeconds { get; set; } = 120;

        [JsonPropertyName("answer_timeout_seconds")]
        public int AnswerTimeSeconds { get; set; } = 90;

        [JsonPropertyName("max_credits")]
        public int MaxCredits { get; set; } = 100;

        [JsonPropertyName("min_credits")]
        public int MinCredits { get; set; } = 10;

        [JsonPropertyName("max_number")]
        public int MaxNumber { get; set; } = 100;

        [JsonPropertyName("min_number")]
        public int MinNumber { get; set; } = 1;

        [JsonPropertyName("operator_chances")]
        public Dictionary<string, int> OperatorChances { get; set; } = new()
        {
            { "+", 40 },
            { "-", 20 },
            { "*", 25 },
            { "/", 15 }
        };

        [JsonPropertyName("operator_quantity_chances")]
        public Dictionary<int, int> OperatorQuantityChances { get; set; } = new()
        {
            { 1, 10 },
            { 2, 20 },
            { 3, 40 },
            { 4, 15 }
        };

        [JsonPropertyName("reward_type")]
        public int RewardType { get; set; } = 2; // 1 = Difficulty-based | 2 = Random
    }

    public class Store_MathQuiz : BasePlugin, IPluginConfig<Store_MathQuizConfig>
    {
        public override string ModuleName { get; } = "Store Module [Math Quiz]";
        public override string ModuleVersion { get; } = "0.0.1";
        public override string ModuleAuthor => "Nathy";

        public Store_MathQuizConfig Config { get; set; } = new();
        private string currentQuestion = string.Empty;
        private double currentAnswer = 0;
        private int currentCredits = 0;
        private bool questionAnswered = false;
        private IStoreApi? storeApi;
        private readonly object lockObject = new();
        private readonly Random rand = new();

        public void OnConfigParsed(Store_MathQuizConfig config)
        {
            Config = config;
        }

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            storeApi = IStoreApi.Capability.Get();

            AddTimer(Config.QuestionIntervalSeconds, () =>
            {
                AskQuestion();
            });

            AddCommandListener("say", OnPlayerChatAll);
            AddCommandListener("say_team", OnPlayerChatTeam);
        }

        private void AskQuestion()
        {
            lock (lockObject)
            {
                if (!questionAnswered)
                {
                    GenerateNewQuestion();
                    questionAnswered = false;

                    Server.NextFrame(() =>
                    {
                        Server.PrintToChatAll(Localizer["Prefix"] + Localizer["MathQuiz.Question", currentQuestion, currentCredits]);
                    });

                    AddTimer(Config.AnswerTimeSeconds, () =>
                    {
                        OnAnswerTimeout();
                    });
                }
            }
        }

        private void OnAnswerTimeout()
        {
            lock (lockObject)
            {
                if (!questionAnswered)
                {
                    Server.NextFrame(() =>
                    {
                        int nextQuestionInterval = Config.QuestionIntervalSeconds - Config.AnswerTimeSeconds;
                        Server.PrintToChatAll(Localizer["Prefix"] + Localizer["MathQuiz.NoAnswer", currentAnswer, nextQuestionInterval]);

                        AddTimer(nextQuestionInterval, () =>
                        {
                            AskQuestion();
                        });
                    });
                }
            }
        }

        private void GenerateNewQuestion()
        {
            int operatorCount = DetermineOperatorCount();
            double[] numbers = new double[operatorCount + 1];
            string[] operations = new string[operatorCount];
            string question = string.Empty;

            for (int i = 0; i <= operatorCount; i++)
            {
                numbers[i] = rand.Next(Config.MinNumber, Config.MaxNumber + 1);
                if (i < operatorCount)
                {
                    operations[i] = GetRandomOperation();
                }
            }

            string expression = numbers[0].ToString();
            for (int i = 0; i < operatorCount; i++)
            {
                expression += $" {operations[i]} {numbers[i + 1]}";
            }

            double result = EvaluateExpression(expression);
            result = Math.Round(result, 2);

            question = $"{expression} = ?";
            currentQuestion = question;
            currentAnswer = result;

            currentCredits = Config.RewardType == 1 ? CalculateCredits(operatorCount) : rand.Next(Config.MinCredits, Config.MaxCredits + 1);
        }


        private string GetRandomOperation()
        {
            int op = rand.Next(0, 100);
            int cumulative = 0;

            foreach (var kvp in Config.OperatorChances)
            {
                cumulative += kvp.Value;
                if (op < cumulative)
                {
                    return kvp.Key;
                }
            }

            return "+";
        }

        private double ApplyOperation(double left, double right, string operation)
        {
            return operation switch
            {
                "+" => left + right,
                "-" => left - right,
                "*" => left * right,
                "/" => right != 0 ? left / right : left,
                _ => left,
            };
        }

        private int DetermineOperatorCount()
        {
            int roll = rand.Next(0, 100);
            int cumulative = 0;

            foreach (var kvp in Config.OperatorQuantityChances)
            {
                cumulative += kvp.Value;
                if (roll < cumulative)
                {
                    return kvp.Key;
                }
            }

            return 1;
        }

        private double EvaluateExpression(string expression)
        {
            var dataTable = new System.Data.DataTable();
            return Convert.ToDouble(dataTable.Compute(expression, string.Empty));
        }

        private int CalculateCredits(int operatorCount)
        {
            int creditsRange = Config.MaxCredits - Config.MinCredits;
            int baseCredits = Config.MinCredits;

            int additionalCredits = (int)(creditsRange * (operatorCount / 4.0));

            return Math.Min(baseCredits + additionalCredits, Config.MaxCredits);
        }

        private HookResult OnPlayerChatAll(CCSPlayerController? player, CommandInfo message)
        {
            if (player == null)
            {
                return HookResult.Handled;
            }

            if (!questionAnswered)
            {
                string answer = message.GetArg(1);

                if (double.TryParse(answer, out double playerAnswer) && Math.Abs(playerAnswer - currentAnswer) < 0.01)
                {
                    questionAnswered = true;

                    if (storeApi != null && currentCredits > 0)
                    {
                        storeApi.GivePlayerCredits(player, currentCredits);
                        Server.PrintToChatAll(Localizer["Prefix"] + Localizer["MathQuiz.Awarded", player.PlayerName, currentCredits]);
                    }

                    AskQuestion();
                }
            }
            return HookResult.Continue;
        }

        private HookResult OnPlayerChatTeam(CCSPlayerController? player, CommandInfo message)
        {
            if (player == null)
            {
                return HookResult.Handled;
            }

            if (!questionAnswered)
            {
                string answer = message.GetArg(1);

                if (double.TryParse(answer, out double playerAnswer) && Math.Abs(playerAnswer - currentAnswer) < 0.01)
                {
                    questionAnswered = true;

                    if (storeApi != null && currentCredits > 0)
                    {
                        storeApi.GivePlayerCredits(player, currentCredits);
                        Server.PrintToChatAll(Localizer["Prefix"] + Localizer["MathQuiz.Awarded", player.PlayerName, currentCredits]);
                    }

                    AskQuestion();
                }
            }
            return HookResult.Continue;
        }
    }
}
