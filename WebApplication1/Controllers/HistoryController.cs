using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace WebApplication1;

public class HistoryController : Controller
{
    private readonly DBHelper _db;
    public HistoryController(DBHelper db) { _db = db; }

    // GET /History/List?userId=1
    [HttpGet]
    public IActionResult List(int userId)
    {
        // 1) 主表
        var dtH = _db.Query(
            @"SELECT Id, InputSentence, CorrectedSentence, TipZh, MeaningZh, CreatedAt
              FROM LearningHistory
              WHERE UserId = @uid
              ORDER BY CreatedAt DESC",
            new SqlParameter("@uid", userId)
        );

        // 2) 明細 errors
        var dtE = _db.Query(
            @"SELECT HistoryId, StartPos, EndPos, Original, Suggest, ReasonZh, RuleZh
              FROM LearningHistoryErrors
              WHERE HistoryId IN (
                  SELECT Id FROM LearningHistory WHERE UserId = @uid
              )
              ORDER BY HistoryId DESC, StartPos ASC",
            new SqlParameter("@uid", userId)
        );

        // 3) 組回 JSON
        var map = new Dictionary<int, List<object>>();
        foreach (DataRow r in dtE.Rows)
        {
            int hid = Convert.ToInt32(r["HistoryId"]);
            if (!map.ContainsKey(hid)) map[hid] = new List<object>();

            map[hid].Add(new
            {
                start = Convert.ToInt32(r["StartPos"]),
                end = Convert.ToInt32(r["EndPos"]),
                original = r["Original"]?.ToString(),
                suggest = r["Suggest"]?.ToString(),
                reason_zh = r["ReasonZh"]?.ToString(),
                rule_zh = r["RuleZh"]?.ToString(),
                category = "" // 你表裡沒存就留空，renderGrammarViz 也能跑
            });
        }

        var list = new List<object>();
        foreach (DataRow r in dtH.Rows)
        {
            int hid = Convert.ToInt32(r["Id"]);
            list.Add(new
            {
                id = hid,
                createdAt = Convert.ToDateTime(r["CreatedAt"]).ToString("yyyy-MM-dd HH:mm:ss"),
                input = r["InputSentence"]?.ToString() ?? "",
                corrected = r["CorrectedSentence"]?.ToString() ?? "",
                one_sentence_tip_zh = r["TipZh"]?.ToString() ?? "",
                meaning_zh = r["MeaningZh"]?.ToString() ?? "",
                errors = map.ContainsKey(hid) ? map[hid] : new List<object>()
            });
        }

        return Json(new { ok = true, data = list });
    }
}
