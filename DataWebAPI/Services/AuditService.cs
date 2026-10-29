using System.Globalization;
using DataWebAPI.Models;
using Microsoft.Data.Sqlite;

namespace DataWebAPI.Services{
    public class AuditService : IAuditService{
        private readonly string _connStr;
        private const string TimeFmt = "yyyy-MM-dd HH:mm:ss";

        private const string SelectSql = @"
            SELECT l.AuditId, l.AdminUserId, u.Username, l.Action, l.TargetType, l.TargetId, l.Details, l.Timestamp
            FROM AuditLogs l JOIN Users u ON u.UserId = l.AdminUserId";

        public AuditService(IConfiguration config){
            _connStr = config.GetConnectionString("DefaultConnection") ?? "Data Source=bank.db;Foreign Keys=True";
        }

        private SqliteConnection Open(){
            var conn = new SqliteConnection(_connStr);
            conn.Open();
            return conn;
        }

        private static SqliteCommand Cmd(SqliteConnection conn, string sql, params (string Name, object? Value)[] ps){
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (name, value) in ps)
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
            return cmd;
        }

        public Task<AuditEntryDto> CreateAsync(CreateAuditRequest r){
            using var conn = Open();

            string? role;
            using (var check = Cmd(conn, "SELECT Role FROM Users WHERE UserId = @u", ("@u", r.AdminUserId)))
                role = check.ExecuteScalar() as string;

            if (role == null)
                throw new NotFoundException($"User {r.AdminUserId} not found.");
            if (role != "Admin")
                throw new BusinessRuleException("NOT_ADMIN", $"User {r.AdminUserId} is not an administrator.");

            long id;
            using (var cmd = Cmd(conn,
                @"INSERT INTO AuditLogs (AdminUserId, Action, TargetType, TargetId, Details, Timestamp)
                  VALUES (@u, @a, @tt, @ti, @d, @ts);
                  SELECT last_insert_rowid();",
                ("@u", r.AdminUserId), ("@a", r.Action.Trim().ToUpperInvariant()), ("@tt", r.TargetType),
                ("@ti", r.TargetId), ("@d", r.Details), ("@ts", DateTime.UtcNow.ToString(TimeFmt))))
            {
                id = Convert.ToInt64(cmd.ExecuteScalar());
            }

            return GetAsync(id);
        }

        public Task<AuditEntryDto> GetAsync(long auditId){
            using var conn = Open();
            using var cmd = Cmd(conn, SelectSql + " WHERE l.AuditId = @id", ("@id", auditId));
            using var rd = cmd.ExecuteReader();

            if (!rd.Read())
                throw new NotFoundException($"Audit entry {auditId} not found.");

            return Task.FromResult(Read(rd));
        }

        public Task<PagedResult<AuditEntryDto>> SearchAsync(AuditQuery q){
            using var conn = Open();

            var where = new List<string>();
            var ps = new List<(string Name, object? Value)>();

            if (q.AdminUserId.HasValue){
                where.Add("l.AdminUserId = @admin");
                ps.Add(("@admin", q.AdminUserId.Value));
            }
            if (!string.IsNullOrWhiteSpace(q.Action)){
                where.Add("l.Action = @action");
                ps.Add(("@action", q.Action.Trim().ToUpperInvariant()));
            }
            if (!string.IsNullOrWhiteSpace(q.Search)){
                where.Add("(l.Action LIKE @s OR l.TargetId LIKE @s OR l.Details LIKE @s OR u.Username LIKE @s)");
                ps.Add(("@s", "%" + q.Search.Trim() + "%"));
            }
            if (q.From.HasValue){
                where.Add("l.Timestamp >= @from");
                ps.Add(("@from", q.From.Value.ToString(TimeFmt)));
            }
            if (q.To.HasValue){
                var to = q.To.Value.TimeOfDay == TimeSpan.Zero ? q.To.Value.AddDays(1) : q.To.Value.AddSeconds(1);
                where.Add("l.Timestamp < @to");
                ps.Add(("@to", to.ToString(TimeFmt)));
            }
            var whereSql = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";
            var dir = q.SortDir.Equals("asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

            int total;
            using (var cnt = Cmd(conn, "SELECT COUNT(*) FROM AuditLogs l JOIN Users u ON u.UserId = l.AdminUserId" + whereSql, ps.ToArray()))
                total = Convert.ToInt32(cnt.ExecuteScalar());

            ps.Add(("@size", q.PageSize));
            ps.Add(("@off", (q.Page - 1) * q.PageSize));

            var items = new List<AuditEntryDto>();
            using (var cmd = Cmd(conn, SelectSql + whereSql + $" ORDER BY l.Timestamp {dir}, l.AuditId {dir} LIMIT @size OFFSET @off", ps.ToArray()))
            using (var rd = cmd.ExecuteReader()){
                while (rd.Read())
                    items.Add(Read(rd));
            }

            return Task.FromResult(new PagedResult<AuditEntryDto>{
                Items = items,
                Page = q.Page,
                PageSize = q.PageSize,
                TotalCount = total,
                TotalPages = (int)Math.Ceiling(total / (double)q.PageSize)
            });
        }

        private static AuditEntryDto Read(SqliteDataReader rd) => new(){
            AuditId = rd.GetInt64(0),
            AdminUserId = rd.GetInt32(1),
            AdminUsername = rd.GetString(2),
            Action = rd.GetString(3),
            TargetType = rd.IsDBNull(4) ? null : rd.GetString(4),
            TargetId = rd.IsDBNull(5) ? null : rd.GetString(5),
            Details = rd.IsDBNull(6) ? null : rd.GetString(6),
            Timestamp = DateTime.SpecifyKind(DateTime.Parse(rd.GetString(7), CultureInfo.InvariantCulture), DateTimeKind.Utc)
        };
    }
}
