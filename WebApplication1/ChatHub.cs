using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using System.Data;
using WebApplication1;
using WebApplication1.Models;

public class ChatHub : Hub
{
    private readonly OpenAIService _openAI;
    private readonly DBHelper _db;

    public ChatHub(OpenAIService openAI, DBHelper db)
    {
        _openAI = openAI;
        _db = db;
    }

    // 前端呼叫：connection.invoke("SendMessage", userId, userName, message)
    public async Task SendMessage(int userId, string userName, string message)
    {
        // 先把原訊息廣播出去
        await Clients.All.SendAsync("ReceiveMessage", userName, message);

        // 針對這句做 grammar check
        GrammarCheckResponse result;
        try
        {
            result = await _openAI.GrammarCheckAsync(message);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("ReceiveSystem", $"OpenAI 錯誤：{ex.Message}");
            return;
        }

        // 回傳給發送者
        await Clients.Caller.SendAsync("ReceiveGrammarCheck", userName, result);

        // =========================
        // ✅ 存學習歷程（貼在這裡）
        // =========================
        try
        {
            // 1) 插入主表 + 直接拿新 Id（不用 SCOPE_IDENTITY 分開查）
            var hidObj = _db.Scalar(
"INSERT INTO LearningHistory (UserId, InputSentence, CorrectedSentence, TipZh, MeaningZh) " +
"OUTPUT INSERTED.Id " +
"VALUES (@uid, @input, @corr, @tip, @meaning)",
new SqlParameter("@uid", userId),
new SqlParameter("@input", result.Input ?? message),
new SqlParameter("@corr", (object?)result.Corrected ?? DBNull.Value),
new SqlParameter("@tip", (object?)result.OneSentenceTipZh ?? DBNull.Value),
new SqlParameter("@meaning", (object?)result.meaning_zh ?? DBNull.Value)
);

            int historyId = Convert.ToInt32(hidObj);

            // 2) 插入錯誤明細
            if (result.Errors != null)
            {
                foreach (var e in result.Errors)
                {
                    string sql = "INSERT INTO LearningHistoryErrors " +
                                 "(HistoryId, StartPos, EndPos, Original, Suggest, ReasonZh, RuleZh) " +
                                 "VALUES (@hid, @s, @en, @o, @sg, @rz, @rl)";

                    _db.Execute(sql,
                        new SqlParameter("@hid", historyId),
                        new SqlParameter("@s", e.Start),
                        new SqlParameter("@en", e.End),
                        new SqlParameter("@o", e.Original ?? ""),
                        new SqlParameter("@sg", e.Suggest ?? ""),
                        new SqlParameter("@rz", (object?)e.ReasonZh ?? DBNull.Value),
                        new SqlParameter("@rl", (object?)e.RuleZh ?? DBNull.Value)
                    );
                }
            }

        }
        catch (Exception ex)
        {
            // 不要影響聊天/批改流程，只回報給自己看
            //await Clients.Caller.SendAsync("ReceiveSystem", $"DB 存檔失敗：{ex.Message}");
            //await Clients.Caller.SendAsync("ReceiveSystem", "DB 存檔失敗(完整)：\n" + ex.ToString());
            await Clients.Caller.SendAsync("ReceiveSystem", "DB 存檔失敗(完整)：\n" + ex.ToString());
        }
    }

    // 分析：connection.invoke("AnalyzeText", text)
    public async Task AnalyzeText(string input)
    {
        AnalyzeResponse result;
        try
        {
            result = await _openAI.AnalyzeAsync(input);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("ReceiveSystem", $"OpenAI 錯誤：{ex.Message}");
            return;
        }

        await Clients.Caller.SendAsync("ReceiveAnalyze", result);
    }
}
