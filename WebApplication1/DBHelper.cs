using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;


namespace WebApplication1;

public class DBHelper
{
    private readonly string _connStr;

    public DBHelper(IConfiguration config)
    {
        _connStr = config.GetConnectionString("Default")
                  ?? throw new InvalidOperationException("Missing connection string: Default");
    }

    // SELECT
    public DataTable Query(string sql, params SqlParameter[] parameters)
    {
        var dt = new DataTable();

        using var conn = new SqlConnection(_connStr);
        using var cmd = new SqlCommand(sql, conn);

        if (parameters?.Length > 0)
            cmd.Parameters.AddRange(parameters);

        conn.Open();
        using var da = new SqlDataAdapter(cmd);
        da.Fill(dt);

        return dt;
    }

    // INSERT / UPDATE / DELETE
    public int Execute(string sql, params SqlParameter[] parameters)
    {
        SqlCommand? cmd = null;

        try
        {
            using var conn = new SqlConnection(_connStr);
            cmd = new SqlCommand(sql, conn);

            if (parameters?.Length > 0)
                cmd.Parameters.AddRange(parameters);

            conn.Open();
            int affected = cmd.ExecuteNonQuery();
            return affected;
        }
        catch (SqlException ex)
        {
            var dump = "[SQL] " + sql + "\n" +
                       (cmd == null ? "(cmd is null)" :
                       string.Join("\n", cmd.Parameters.Cast<SqlParameter>()
                           .Select(p => $"{p.ParameterName}={p.Value}")));

            throw new Exception(dump, ex);
        }
    }



    // SCALAR
    public object? Scalar(string sql, params SqlParameter[] parameters)
    {
        SqlCommand? cmd = null;

        try
        {
            using var conn = new SqlConnection(_connStr);
            cmd = new SqlCommand(sql, conn);

            if (parameters?.Length > 0)
                cmd.Parameters.AddRange(parameters);

            conn.Open();
            return cmd.ExecuteScalar();
        }
        catch (SqlException ex)
        {
            var dump = "[SQL] " + sql + "\n" +
                       (cmd == null ? "(cmd is null)" :
                       string.Join("\n", cmd.Parameters.Cast<SqlParameter>()
                           .Select(p => $"{p.ParameterName}={p.Value}")));

            throw new Exception(dump, ex);
        }
    }

}
