using System.Text.Json.Serialization;

namespace WebApplication1.Models
{
    public class AnalyzeResponse
    {
        public string Task { get; set; } = "";
        public string Prompt_Version { get; set; } = ""; // 也可用 JsonPropertyName("prompt_version")
        public string Input { get; set; } = "";
        public string Language { get; set; } = "unknown";
        public List<AnalyzeUnit> Units { get; set; } = new();
        public List<string> Notes_Zh { get; set; } = new();

        public AnalyzeCompose Compose { get; set; } = new();
    }

    public class AnalyzeUnit
    {
        public string Text { get; set; } = "";
        public string Type { get; set; } = "unknown";
        public string Gloss_Zh { get; set; } = "";
        public string Pos { get; set; } = "unknown";
        public string? Pattern { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public string? Ko { get; set; }
    }
    public class AnalyzeCompose
    {
        public string? Ko_Natural { get; set; }
        public string? Pattern_Ko { get; set; }
        public List<AnalyzeAlignment> Alignment { get; set; } = new();
        public List<string> Why_Zh { get; set; } = new();
    }

    public class AnalyzeAlignment
    {
        public List<int> From_Units { get; set; } = new();
        public string? To { get; set; }
    }



    public class GrammarCheckResponse
    {
        [JsonPropertyName("task")] public string Task { get; set; } = "";
        [JsonPropertyName("input")] public string Input { get; set; } = "";
        [JsonPropertyName("is_korean")] public bool IsKorean { get; set; }
        [JsonPropertyName("has_error")] public bool HasError { get; set; }
        [JsonPropertyName("corrected")] public string Corrected { get; set; } = "";
        [JsonPropertyName("meaning_zh")] public string meaning_zh { get; set; } = "";
        [JsonPropertyName("errors")] public List<GrammarError> Errors { get; set; } = new();
        [JsonPropertyName("one_sentence_tip_zh")] public string OneSentenceTipZh { get; set; } = "";
        [JsonPropertyName("feedback_level")] public string FeedbackLevel { get; set; } = "normal";
    }

    public class GrammarError
    {
        [JsonPropertyName("start")] public int Start { get; set; }
        [JsonPropertyName("end")] public int End { get; set; }
        [JsonPropertyName("original")] public string Original { get; set; } = "";
        [JsonPropertyName("suggest")] public string Suggest { get; set; } = "";
        [JsonPropertyName("category")] public string Category { get; set; } = "other";
        [JsonPropertyName("reason_zh")] public string ReasonZh { get; set; } = "";
        [JsonPropertyName("rule_zh")] public string? RuleZh { get; set; }
    }
}
