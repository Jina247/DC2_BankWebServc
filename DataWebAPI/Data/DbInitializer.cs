using Microsoft.Data.Sqlite;

namespace DataWebAPI.Data
{
    public static class DbInitializer
    {
        public const string Schema = @"
CREATE TABLE IF NOT EXISTS Users (
    UserId        INTEGER PRIMARY KEY AUTOINCREMENT,
    Username      TEXT    NOT NULL COLLATE NOCASE UNIQUE,
    Name          TEXT    NOT NULL,
    Email         TEXT    NOT NULL COLLATE NOCASE UNIQUE,
    Address       TEXT,
    Phone         TEXT,
    PicturePath   TEXT,
    PasswordHash  TEXT    NOT NULL,
    Role          TEXT    NOT NULL DEFAULT 'Customer' CHECK (Role IN ('Customer', 'Admin')),
    IsActive      INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1)),
    CreatedAt     TEXT    NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS Accounts (
    AccountNumber TEXT    PRIMARY KEY,
    UserId        INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE RESTRICT,
    AccountType   TEXT    NOT NULL,
    Balance       NUMERIC NOT NULL DEFAULT 0 CHECK (Balance >= 0),
    IsActive      INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1)),
    CreatedAt     TEXT    NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS Transactions (
    TransactionId        INTEGER PRIMARY KEY AUTOINCREMENT,
    AccountNumber        TEXT    NOT NULL REFERENCES Accounts(AccountNumber) ON DELETE RESTRICT,
    Type                 TEXT    NOT NULL CHECK (Type IN ('Deposit', 'Withdrawal', 'TransferOut', 'TransferIn')),
    Amount               NUMERIC NOT NULL,
    BalanceAfter         NUMERIC NOT NULL CHECK (BalanceAfter >= 0),
    Description          TEXT,
    RelatedAccountNumber TEXT    REFERENCES Accounts(AccountNumber) ON DELETE RESTRICT,
    Timestamp            TEXT    NOT NULL
);

CREATE TABLE IF NOT EXISTS AuditLogs (
    AuditId     INTEGER PRIMARY KEY AUTOINCREMENT,
    AdminUserId INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE RESTRICT,
    Action      TEXT    NOT NULL,
    TargetType  TEXT,
    TargetId    TEXT,
    Details     TEXT,
    Timestamp   TEXT    NOT NULL
);

-- indexes so the history pages (filter by account + date, sort by date) stay fast
CREATE INDEX IF NOT EXISTS IX_Accounts_UserId            ON Accounts(UserId);
CREATE INDEX IF NOT EXISTS IX_Transactions_Account_Time  ON Transactions(AccountNumber, Timestamp);
CREATE INDEX IF NOT EXISTS IX_Transactions_Time          ON Transactions(Timestamp);
CREATE INDEX IF NOT EXISTS IX_AuditLogs_Time             ON AuditLogs(Timestamp);
";

     
        public static void Initialise(string connStr, ILogger logger){
            var dataSource = new SqliteConnectionStringBuilder(connStr).DataSource;
            var folder = Path.GetDirectoryName(dataSource);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);
           
            using var conn = new SqliteConnection(connStr);
            conn.Open();

            using (var cmd = conn.CreateCommand()){
                cmd.CommandText = Schema;
                cmd.ExecuteNonQuery();
            }

            logger.LogInformation("Database ready at {Path}", dataSource);
        }
    }
}
