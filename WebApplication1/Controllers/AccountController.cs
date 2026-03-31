using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace WebApplication1;

public class AccountController : Controller
{
    private readonly DBHelper _db;
    public AccountController(DBHelper db) { _db = db; }

    [HttpPost]
    public IActionResult Login(string name, string password)
    {
        var dt = _db.Query(
            @"SELECT ""Id"", ""Password"" FROM ""UserID"" WHERE ""Name"" = @name",
            new NpgsqlParameter("@name", name)
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
                @"INSERT INTO ""UserID"" (""Name"", ""Password"") VALUES (@name, @pwd)",
                new NpgsqlParameter("@name", name),
                new NpgsqlParameter("@pwd", password)
            );

            var dt = _db.Query(
                @"SELECT ""Id"" FROM ""UserID"" WHERE ""Name"" = @name",
                new NpgsqlParameter("@name", name)
            );

            return Json(new { ok = true, userId = (int)dt.Rows[0]["Id"] });
        }
        catch (PostgresException ex) when (ex.SqlState == "23505") // 唯一鍵衝突
        {
            return Json(new { ok = false, msg = "帳號已存在" });
        }
    }
}
