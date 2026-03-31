using Microsoft.AspNetCore.SignalR;
using Npgsql;
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

    public async Task SendMessage(int userId, string userName, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", userName, message);

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

        await Clients.Caller.SendAsync("ReceiveGrammarCheck", userName, result);

        try
        {
            // PostgreSQL 用 RETURNING 取得新 Id
            var hidObj = _db.Scalar(
                @"INSERT INTO ""LearningHistory"" (""UserId"", ""InputSentence"", ""CorrectedSentence"", ""TipZh"", ""MeaningZh"")
                  VALUES (@uid, @input, @corr, @tip, @meaning)
                  RETURNING ""Id""",
                new NpgsqlParameter("@uid", userId),
                new NpgsqlParameter("@input", result.Input ?? message),
                new NpgsqlParameter("@corr", (object?)result.Corrected ?? DBNull.Value),
                new NpgsqlParameter("@tip", (object?)result.OneSentenceTipZh ?? DBNull.Value),
                new NpgsqlParameter("@meaning", (object?)result.meaning_zh ?? DBNull.Value)
            );

            int historyId = Convert.ToInt32(hidObj);

            if (result.Errors != null)
            {
                foreach (var e in result.Errors)
                {
                    _db.Execute(
                        @"INSERT INTO ""LearningHistoryErrors""
                          (""HistoryId"", ""StartPos"", ""EndPos"", ""Original"", ""Suggest"", ""ReasonZh"", ""RuleZh"")
                          VALUES (@hid, @s, @en, @o, @sg, @rz, @rl)",
                        new NpgsqlParameter("@hid", historyId),
                        new NpgsqlParameter("@s", e.Start),
                        new NpgsqlParameter("@en", e.End),
                        new NpgsqlParameter("@o", e.Original ?? ""),
                        new NpgsqlParameter("@sg", e.Suggest ?? ""),
                        new NpgsqlParameter("@rz", (object?)e.ReasonZh ?? DBNull.Value),
                        new NpgsqlParameter("@rl", (object?)e.RuleZh ?? DBNull.Value)
                    );
                }
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("ReceiveSystem", "DB 存檔失敗(完整)：\n" + ex.ToString());
        }
    }

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
