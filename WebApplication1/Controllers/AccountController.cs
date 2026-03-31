using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace WebApplication1;

public class AccountController : Controller
{
    private readonly DBHelper _db;
    public AccountController(DBHelper db) { _db = db; }

    [HttpPost]
    public IActionResult Login(string name, string password)
    {
        // 先查 user 存不存在 + 拿密碼
        var dt = _db.Query(
            "SELECT Id, Password FROM UserID WHERE Name = @name",
            new SqlParameter("@name", name)
        );

        if (dt.Rows.Count == 0)
            return Json(new { ok = false, code = "not_found" });

        var dbPwd = dt.Rows[0]["Password"]?.ToString() ?? "";
        if (dbPwd != password)
            return Json(new { ok = false, code = "wrong_password" });

        int userId = Convert.ToInt32(dt.Rows[0]["Id"]);
        return Json(new { ok = true, userId });
    }


    [HttpPost]
    public IActionResult Register(string name, string password)
    {
        try
        {
            _db.Execute(
                "INSERT INTO UserID (Name, Password) VALUES (@name, @pwd)",
                new SqlParameter("@name", name),
                new SqlParameter("@pwd", password)
            );

            // 回傳新增後的 Id
            var dt = _db.Query(
                "SELECT Id FROM UserID WHERE Name = @name",
                new SqlParameter("@name", name)
            );

            return Json(new { ok = true, userId = (int)dt.Rows[0]["Id"] });
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            return Json(new { ok = false, msg = "帳號已存在" });
        }
    }
}
