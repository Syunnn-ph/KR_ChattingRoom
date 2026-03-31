using System.Net.Http;
using System.Text;
using System.Text.Json;
using WebApplication1.Models;

public class OpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAIService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        //_apiKey = config["OpenAI:ApiKey"] ?? "";
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("OpenAI:ApiKey 未設定在 appsettings.json");
    }

    // === 你要的兩個 Prompt（system） ===
    private const string AnalyzeSystemPrompt = """
[ANALYZE_PROMPT_VERSION = 2026-01-03-V2]
你是一個韓文學習輔助工具的「中→韓」句子拆解器。使用者通常輸入繁體中文，你要把中文句子做「學習導向拆分」，並為每個片段提供對應的韓語表達。

你的輸出必須是「單一 JSON」，不能有任何多餘文字、markdown、或解釋。

【你要做的事】
1) 判斷 language: ko/zh/en/mixed/unknown（以 input 實際語言為準）
2) 產生 units：依原字串順序切分 input（不要改動 input），每個 unit 必須包含：
   - text: 原文片段（不可改動）
   - type: word/grammar/particle/ending/punct/unknown
   - gloss_zh: 用繁體中文簡短說明此片段的語意/功能（學習導向）
   - pos: 若能對應到韓語詞性/標記（NNG/VV/VA/JKS/JKO/EF…）就填；不確定填 "unknown"
   - pattern:
       * 只有在 type = grammar/particle/ending 時才填「文法/助詞/語尾模式」（例如 "V-니?"、"A/V-아/어지다"）
       * 若是一般詞彙（type=word）或標點（punct），pattern 必須為 null
   - start/end: 這個 text 在原始 input 中的字元起訖位置（start 포함, end 不包含）
   - ko: 對應的韓語建議表達（單字/片語/助詞/語尾皆可）；沒有就填 null
3) notes_zh：最多 3 點，提供「學習提示」，不得用來補上每個字的對應翻譯（對應翻譯必須放在 units[].ko）。

【新增輸出欄位：compose】
- compose.ko_natural：請輸出一個「最自然、可直接使用」的韓文整句（不必逐字直譯）。
- compose.pattern_ko：輸出本句主要使用的韓語句型/模板（例如 "V-(으)러 가다 + -(으)려고 하다"）。
- compose.alignment：用 0-based unit index 把 units 對齊到「實際自然韓語」片段。
  * 若某些中文片段在自然韓語中不需要直翻（例如「打算」變成 -(으)려고 하다），則該片段 to 可以為 null。
- compose.why_zh：最多 3 點，說明為什麼要這樣組句（學習導向）。
- compose.alignment 的每個項目都必須提供 to（字串），用來對應 ko_natural 中「實際出現的片段」以便前端高亮。
- 若某個中文片段在自然韓語中被「句型化」（例如 打算 → -(으)려고 하다），則該片段的 to 必須填入對應的句型片段（例如 "려고" 或 "가려고 해요"），不可為 null。
- 只有在完全不需要表達該片段（真正省略）時，to 才可以填空字串 ""。
- compose.ko_natural 必須涵蓋 input 的核心語意（本例必須包含「逛/구경」的語意，例如 "구경하러"）。



【重要規則】
- 只能輸出 JSON，不能輸出其他任何字。
- units 必須按原文順序排列。
- start/end 必須對應原始 input 的字元索引。
- 不要自動糾錯；不要改寫 input。
- 每一個 units 物件都必須包含 ko 欄位（即使值為 null），不要只放在 notes_zh，若缺少 ko 視為輸出格式錯誤。
- prompt_version 必須輸出為 "2026-01-03-V2"
- 教學優先原則：即使不存在唯一的一對一翻譯，只要在學習上有「最常見、最自然」的韓語對應，也必須填入 ko，不得因不確定性而填 null。
- 只有在「完全沒有自然韓語對應」時，才允許 ko 為 null。


【輸出格式】
{
  "task":"analyze",
  "prompt_version": "string",
  "input":"string",
  "language":"ko|zh|en|mixed|unknown",
  "units":[
    {"text":"string","type":"word|grammar|particle|ending|punct|unknown","gloss_zh":"string","pos":"string","pattern":null,"start":0,"end":0,"ko":null}
  ],
  "notes_zh":["string"]
}
""";


    private const string GrammarCheckSystemPrompt = """
你是一個韓文文法校正器，用於聊天室訊息。你必須輸出單一 JSON，不能有任何多餘文字、markdown、或解釋。

【任務】
1) 判斷是否為韓文句子或主要為韓文：is_korean（true/false）
2) 若 is_korean=false：has_error=false, corrected=input, errors=[]，並給 one_sentence_tip_zh
3) 若 is_korean=true：檢查文法、助詞、空格、拼字、語尾、時態、敬語、標點。
   - 如無錯：has_error=false, corrected=input（若你做了任何變動仍視為 has_error=true）
   - 如有錯：has_error=true，提供 corrected，並列出 errors[]。

【errors[] 規格】
- start/end：錯誤片段在原 input 的字元位置（start 포함, end 不包含）
- original：原片段
- suggest：建議替換成的片段（刪除則 suggest=""）
- category：spacing|particle|ending|tense|honorific|word_choice|spelling|punct|other
- reason_zh：繁中一句話說明為什麼錯
- rule_zh：若有對應規則，用繁中簡短描述；沒有則 null

【新增語意說明欄位】
- meaning_zh：請用繁體中文說明「此 input 句子在韓語母語者眼中，實際傳達的意思」。
- meaning_zh 必須根據 input（或 corrected，若 has_error=true 且 corrected 不為 null）來判斷。
- 不要猜測使用者原本想說什麼。
- 即使文法正確、has_error=false，也必須提供 meaning_zh。
- 若句子語意模糊，請如實描述模糊之處，不要自行補齊。
- 語意正確但語氣不同、或可接受的口語表達，不得視為錯誤；但 meaning_zh 仍需如實描述其語意。


【其他欄位】
- feedback_level：gentle/normal/strict（預設 normal）
- one_sentence_tip_zh：一句話建議（繁中、短）

【重要規則】
- 只能輸出 JSON，不能輸出其他任何字。
- corrected 必須是完整句子。
- 若句子在韓語中是可接受（grammatical）的，即使語氣不完整或偏口語，也不得標記為 has_error=true。
- 語氣、禮貌程度、自然度差異，僅能以「建議（suggestion）」呈現，不得視為錯誤。
- 若沒有明確文法錯誤，corrected 必須為 null。
- errors 位置必須對應原 input，不要對 corrected 算位置。
- errors 中的 start/end 必須只標記「實際錯誤的最小詞彙或助詞單位」，
  不得包含正確的詞、語尾（如 있니/어요/다）或標點符號。
- 若需要修改整句結構，請在 corrected 中呈現，但 errors 仍只能標最小錯誤片段。

【輸出格式】
{
  "task":"grammar_check",
  "input":"string",
  "is_korean":true,
  "has_error":true,
  "corrected":"string",
  "errors":[
    {"start":0,"end":0,"original":"string","suggest":"string","category":"spacing|particle|ending|tense|honorific|word_choice|spelling|punct|other","reason_zh":"string","rule_zh":null}
  ],
  "one_sentence_tip_zh":"string",
  "feedback_level":"gentle|normal|strict"
}
""";


    // === 對外方法 ===
    public Task<AnalyzeResponse> AnalyzeAsync(string input)
        => CallAndParseAnalyzeWithSchemaAsync(input);

    public Task<GrammarCheckResponse> GrammarCheckAsync(string input)
        => CallAndParseAsync<GrammarCheckResponse>(GrammarCheckSystemPrompt, input);

    // === 核心呼叫 + 解析 ===
    private async Task<T> CallAndParseAsync<T>(string systemPrompt, string userInput)
    {
        var requestBody = new
        {
            model = "gpt-4o",
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userInput }
            },
            temperature = 0.2
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        req.Headers.Add("Authorization", $"Bearer {_apiKey}");
        req.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var resp = await _httpClient.SendAsync(req);
        var raw = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"OpenAI API error: {(int)resp.StatusCode}\n{raw}");

        // 取出 assistant content（應該是一段 JSON 字串）
        using var doc = JsonDocument.Parse(raw);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        // 容錯：有時模型會多空白或換行
        content = content.Trim();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            var result = JsonSerializer.Deserialize<T>(content, options);
            if (result == null) throw new Exception("JSON 反序列化結果為 null");
            return result;
        }
        catch (Exception ex)
        {
            // 方便你 debug：把 content 印出來
            throw new Exception($"無法解析模型輸出為 {typeof(T).Name}。\n模型輸出：\n{content}\n\n錯誤：{ex.Message}");
        }
    }

    private async Task<AnalyzeResponse> CallAndParseAnalyzeWithSchemaAsync(string userInput)
    {
        // ✅ 建議指定支援 Structured Outputs 的模型版本（官方文章點名）:contentReference[oaicite:2]{index=2}
        const string modelName = "gpt-4o-mini-2024-07-18";

        // ✅ 強制 JSON Schema（strict:true + additionalProperties:false）:contentReference[oaicite:3]{index=3}
        
        var responseFormat = new
        {
            type = "json_schema",
            json_schema = new
            {
                name = "analyze_schema",
                strict = true,
                schema = new
                {
                    type = "object",
                    additionalProperties = false,

                    // ✅ 新增 compose（必填）
                    required = new[] { "task", "prompt_version", "input", "language", "units", "notes_zh", "compose" },

                    properties = new
                    {
                        task = new { type = "string", @enum = new[] { "analyze" } },
                        prompt_version = new { type = "string", @enum = new[] { "2026-01-03-V2" } },
                        input = new { type = "string" },
                        language = new { type = "string", @enum = new[] { "ko", "zh", "en", "mixed", "unknown" } },

                        units = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                additionalProperties = false,
                                required = new[] { "text", "type", "gloss_zh", "pos", "pattern", "start", "end", "ko" },
                                properties = new
                                {
                                    text = new { type = "string" },
                                    type = new { type = "string", @enum = new[] { "word", "grammar", "particle", "ending", "punct", "unknown" } },
                                    gloss_zh = new { type = "string" },
                                    pos = new { type = "string" },
                                    pattern = new { type = new object[] { "string", "null" } },
                                    start = new { type = "integer", minimum = 0 },
                                    end = new { type = "integer", minimum = 0 },
                                    ko = new { type = new object[] { "string", "null" } }
                                }
                            }
                        },

                        notes_zh = new
                        {
                            type = "array",
                            items = new { type = "string" },
                            maxItems = 3
                        },

                        // ✅ 新增 compose：給「自然整句」+「句型」+「對齊」
                        compose = new
                        {
                            type = "object",
                            additionalProperties = false,
                            required = new[] { "ko_natural", "pattern_ko", "alignment", "why_zh" },
                            properties = new
                            {
                                // 一句最自然的韓文（允許 null，但欄位必須存在）
                                ko_natural = new { type = new object[] { "string", "null" } },

                                // 主要句型/模板（允許 null）
                                pattern_ko = new { type = new object[] { "string", "null" } },

                                // 對齊：哪些 units 合併成哪段韓文（to 可以是 null 表示「不直接翻」）
                                alignment = new
                                {
                                    type = "array",
                                    items = new
                                    {
                                        type = "object",
                                        additionalProperties = false,
                                        required = new[] { "from_units", "to" },
                                        properties = new
                                        {
                                            // from_units: unit 的 index 陣列（0-based）
                                            from_units = new
                                            {
                                                type = "array",
                                                items = new { type = "integer", minimum = 0 },
                                                minItems = 1
                                            },

                                            // to: 對應的韓文片段（可為 null）
                                            //to = new { type = new object[] { "string", "null" } }
                                            to = new { type = "string" }
                                        }
                                    }
                                },

                                // 為什麼要這樣組句（最多 3 點）
                                why_zh = new
                                {
                                    type = "array",
                                    items = new { type = "string" },
                                    maxItems = 3
                                }
                            }
                        }
                    }
                }
            }
        };


        var requestBody = new
        {
            model = modelName,
            messages = new object[]
            {
            new { role = "system", content = AnalyzeSystemPrompt },
            new { role = "user", content = userInput }
            },
            temperature = 0.2,
            response_format = responseFormat
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        req.Headers.Add("Authorization", $"Bearer {_apiKey}");
        req.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var resp = await _httpClient.SendAsync(req);
        var raw = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"OpenAI API error: {(int)resp.StatusCode}\n{raw}");

        using var doc = JsonDocument.Parse(raw);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        content = content.Trim();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            var result = JsonSerializer.Deserialize<AnalyzeResponse>(content, options);
            if (result == null) throw new Exception("JSON 反序列化結果為 null");
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"無法解析模型輸出為 AnalyzeResponse。\n模型輸出：\n{content}\n\n錯誤：{ex.Message}");
        }
    }



}
