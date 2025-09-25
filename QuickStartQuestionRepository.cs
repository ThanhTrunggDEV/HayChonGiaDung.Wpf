using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace HayChonGiaDung.Wpf
{
    public static class QuickStartQuestionRepository
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static List<QuickQuestion> LoadQuestions()
        {
            foreach (var path in EnumerateCandidateFiles())
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(path));
                    if (!doc.RootElement.TryGetProperty("questions", out var arr))
                    {
                        continue;
                    }

                    var list = new List<QuickQuestion>();
                    foreach (var item in arr.EnumerateArray())
                    {
                        var text = item.TryGetProperty("text", out var textProp)
                            ? textProp.GetString() ?? string.Empty
                            : string.Empty;

                        var options = new List<string>();
                        if (item.TryGetProperty("options", out var optionsProp))
                        {
                            options.AddRange(optionsProp
                                .EnumerateArray()
                                .Select(o => o.GetString() ?? string.Empty));
                        }

                        while (options.Count < 4)
                        {
                            options.Add(string.Empty);
                        }

                        var correctIndex = item.TryGetProperty("correctIndex", out var correctProp)
                            ? correctProp.GetInt32()
                            : 0;

                        if (correctIndex < 0 || correctIndex >= options.Count)
                        {
                            correctIndex = 0;
                        }

                        var explanation = item.TryGetProperty("explanation", out var explanationProp)
                            ? explanationProp.GetString() ?? string.Empty
                            : string.Empty;

                        list.Add(new QuickQuestion
                        {
                            Text = text,
                            Options = options,
                            CorrectIndex = correctIndex,
                            Explanation = explanation
                        });
                    }

                    if (list.Count > 0)
                    {
                        return list;
                    }
                }
                catch
                {
                    // ignored
                }
            }

            return new List<QuickQuestion>();
        }

        public static void SaveQuestions(IEnumerable<QuickQuestion> questions)
        {
            var payload = questions
                .Select(q => Normalize(q ?? new QuickQuestion()))
                .ToArray();

            var json = JsonSerializer.Serialize(new { questions = payload }, SerializerOptions);

            var written = false;
            foreach (var path in EnumerateCandidateFiles())
            {
                try
                {
                    var directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.WriteAllText(path, json);
                    written = true;
                }
                catch
                {
                    // ignored
                }
            }

            if (!written)
            {
                var fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "quickstart_questions.json");
                var directory = Path.GetDirectoryName(fallback);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(fallback, json);
            }
        }

        private static object Normalize(QuickQuestion question)
        {
            var options = (question.Options ?? new List<string>()).ToList();
            while (options.Count < 4)
            {
                options.Add(string.Empty);
            }

            var correctIndex = Math.Clamp(question.CorrectIndex, 0, Math.Max(0, options.Count - 1));

            return new
            {
                text = question.Text ?? string.Empty,
                options = options.Select(o => o ?? string.Empty).ToArray(),
                correctIndex,
                explanation = question.Explanation ?? string.Empty
            };
        }

        private static IEnumerable<string> EnumerateCandidateFiles()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var start in new[] { Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.BaseDirectory, AppContext.BaseDirectory })
            {
                if (string.IsNullOrEmpty(start))
                {
                    continue;
                }

                var current = Path.GetFullPath(start);
                for (var i = 0; i < 6 && !string.IsNullOrEmpty(current); i++)
                {
                    var candidate = Path.Combine(current, "Data", "quickstart_questions.json");
                    if (seen.Add(candidate))
                    {
                        yield return candidate;
                    }

                    var parent = Directory.GetParent(current);
                    if (parent == null)
                    {
                        break;
                    }

                    current = parent.FullName;
                }
            }
        }
    }
}
