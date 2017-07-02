using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
using Microsoft.Scs.Test.RiskTools.RulePerf.Properties;

namespace Microsoft.Scs.Test.RiskTools.RulePerf.DAL
{
    public class SqlServerHelper
    {
        private static List<SqlError> sqlGenuineErrors;

        public static ConnectionStringHelper ConnectionString = new ConnectionStringHelper();

        #region Connection String Helpers
        public static string GetSQLServerBackupDir()
        {
            string sql = @"DECLARE @path NVARCHAR(4000); EXEC master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE', N'Software\Microsoft\MSSQLServer\MSSQLServer', N'BackupDirectory', @path OUTPUT, 'no_output'; SELECT @path;";
            return (string)QueryScaler(sql);
        }
        #endregion Connection String Helpers

        #region Database controlling operations
        /// <summary>
        /// Backups the database.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="tag">A tag that would be appended after the database name in the backup file name.</param>
        /// <returns>True if success else false.</returns>
        /// <exception cref="RulePerf.DAL.SqlServerHelperException"></exception>
        public static bool BackupDatabase(string databaseName, string tag = "")
        {
            //string backupFileName = Path.Combine(Directory.GetCurrentDirectory(), databaseName + " " + DateTime.Now.ToString("yyyy-MM-ddThh-mm-ssZ") + ".bak");
            string bakFileName;
            return BackupDatabase(databaseName, out bakFileName, tag);
        }

        public static bool BackupDatabase(string databaseName, out string bakFileName, string tag = "")
        {
            bakFileName = Path.Combine(GetSQLServerBackupDir(), databaseName + "." + tag + "." + DateTime.Now.ToString("yyyy-MM-ddThh-mm-ssZ") + ".bak");
            return BackupDatabaseAs(databaseName, bakFileName);
        }

        public static bool BackupDatabaseAs(string databaseName, string bakFileName)
        {
            sqlGenuineErrors = new List<SqlError>();
            string backupCommand = string.Format("backup database {0} to disk = {1} with description = {2}, name = {3}, stats = 1",
                        QuoteIdentifier(databaseName), QuoteString(bakFileName), QuoteString("Auto backup"), QuoteString(bakFileName));
            try
            {
                Log.Info("Trying to back up database {0} to {1} on server {2}...", databaseName, bakFileName, ConnectionString.Server);
                ConnectionString.ConnectionTimeout = Settings.Default.SQLConnectionTimeout;
                using (SqlConnection conn = new SqlConnection(ConnectionString.ConnectionString))
                {
                    conn.FireInfoMessageEventOnUserErrors = true;
                    conn.InfoMessage += OnInfoMessage;
                    conn.Open();
                    using (var cmd = new SqlCommand(backupCommand, conn))
                    {
                        cmd.CommandTimeout = Settings.Default.SQLCommandTimeout;
                        cmd.ExecuteNonQuery();
                    }
                    conn.Close();
                    conn.InfoMessage -= OnInfoMessage;
                    conn.FireInfoMessageEventOnUserErrors = false;
                }
                Log.Info("Backed up database {0} to {1} on server {2}.", databaseName, bakFileName, ConnectionString.Server);
                return sqlGenuineErrors == null || sqlGenuineErrors.Count <= 0;
            }
            catch (Exception ex)
            {
                throw new SqlServerHelperException(ex, ConnectionString.ConnectionString, backupCommand);
            }
        }

        public static bool RestoreDatabase(string databaseName, string fileName)
        {
            sqlGenuineErrors = new List<SqlError>();
            string restoreCommand = string.Format("RESTORE {0} FROM DISK = '{1}'", databaseName, fileName);
            try
            {
                Log.Info("Trying to restore database {0} to {1} on server {2}", fileName, databaseName, ConnectionString.Server);
                ConnectionString.ConnectionTimeout = Settings.Default.SQLCommandTimeout;
                using (SqlConnection conn = new SqlConnection(ConnectionString.ConnectionString))
                {
                    conn.FireInfoMessageEventOnUserErrors = true;
                    conn.InfoMessage += OnInfoMessage;
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(restoreCommand, conn))
                    {
                        cmd.CommandTimeout = Settings.Default.SQLCommandTimeout;
                        cmd.ExecuteNonQuery();
                    }
                    conn.Close();
                    conn.InfoMessage -= OnInfoMessage;
                    conn.FireInfoMessageEventOnUserErrors = false;
                }

                return sqlGenuineErrors == null || sqlGenuineErrors.Count <= 0;
            }
            catch (Exception ex)
            {
                throw new SqlServerHelperException(ex, ConnectionString.ConnectionString, restoreCommand);
            }
        }

        public static bool DoesTableExist(string databaseName, string tableName)
        {
            string cmdText = "USE {0}; SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{1}'".FormatWith(databaseName, tableName);
            try
            {
                DataTable dt = Query(cmdText);
                return dt.Rows.Count > 0;
            }
            catch (Exception ex)
            {
                throw new SqlServerHelperException(ex, ConnectionString.ConnectionString, cmdText);
            }
        }

        public static bool DoesDatabaseExist(string databaseName)
        {
            string cmdText = "SELECT name FROM master.dbo.sysdatabases WHERE name= '{0}'".FormatWith(databaseName);
            try
            {
                DataTable dt = Query(cmdText);
                return dt.Rows.Count > 0;
            }
            catch (Exception ex)
            {
                throw new SqlServerHelperException(ex, ConnectionString.ConnectionString, cmdText);
            }
        }

        public static DataTable GetAllUserTableNamesOfDatabase(string databaseName)
        {
            string cmdText = "USE {0}; SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE';".FormatWith(databaseName);
            try
            {
                return Query(cmdText);
            }
            catch (Exception ex)
            {
                throw new SqlServerHelperException(ex, ConnectionString.ConnectionString, cmdText);
            }
        }

        public static void DisableAllIndexesOnTable(string database, string table)
        {
            string cmdText = "USE {0}; SELECT i.name FROM sys.indexes i INNER JOIN sys.objects o ON o.object_id = i.object_id WHERE o.is_ms_shipped = 0 AND i.index_id >= 1 AND i.type <> 1 AND o.name = @tableName".FormatWith(database);
            SqlParameter[] sqlParameters = new SqlParameter[] {
                new SqlParameter("tableName", table)
            };

            try
            {
                //DataTable dt = EnhancedQuery(cmdText, sqlParameters);
                DataTable dt = Query(cmdText, sqlParameters);
                if (dt.Rows.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("USE {0}; ".FormatWith(database));
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        sb.AppendFormat("ALTER INDEX [{0}] ON [{1}] DISABLE; ".FormatWith(dt.Rows[i][0].ToString(), table));
                    }

                    //EnhancedExecute(sb.ToString(), sqlParameters);
                    Execute(sb.ToString(), sqlParameters);
                }
            }
            catch (Exception ex)
            {
                Exception innerMostException = ex.InnerMostException();
                if (innerMostException != null && innerMostException is System.Data.SqlClient.SqlException)
                {
                    ExceptionHelper.CentralProcess(ex);
                }
                else
                    throw new SqlServerHelperException(ex, ConnectionString.ConnectionString, cmdText, sqlParameters);
            }
        }

        public static void RebuildAllIndexesOnTable(string database, string table)
        {
            string cmdText = "USE {0}; SELECT i.name FROM sys.indexes i INNER JOIN sys.objects o ON o.object_id = i.object_id WHERE o.is_ms_shipped = 0 AND i.index_id >= 1 AND i.type <> 1 AND o.name = @tableName".FormatWith(database);
            SqlParameter[] sqlParameters = new SqlParameter[] {
                new SqlParameter("tableName", table)
            };

            try
            {
                DataTable dt = Query(cmdText, sqlParameters);
                if (dt.Rows.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("USE {0};".FormatWith(database));
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        sb.AppendFormat("ALTER INDEX [{0}] ON [{1}] REBUILD; ".FormatWith(dt.Rows[i][0].ToString(), table));
                    }

                    Execute(sb.ToString(), sqlParameters);
                }
            }
            catch (Exception ex)
            {
                throw new SqlServerHelperException(ex, ConnectionString.ConnectionString, cmdText, sqlParameters);
            }
        }
        #endregion Database controlling operations

        #region SQL CRUD operations
        public static DataTable EnhancedQuery(string cmdText, params SqlParameter[] parameters)
        {
            int index = cmdText.IndexOf(';');
            int start = 0;
            string s = "";
            DataTable dt = new DataTable("EnhancedQueryResultTable");
            try
            {
                ConnectionString.ConnectionTimeout = Settings.Default.SQLConnectionTimeout;
                using (SqlConnection conn = new SqlConnection(ConnectionString.ConnectionString))
                {
                    while (index > 0)
                    {
                        s = cmdText.Substring(start, index - start + 1).Trim();
                        start = index + 1;
                        index = cmdText.IndexOf(';', start);

                        if (s.StartsWith("USE ", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string databaseName = s.Substring("USE ", ";", StringComparison.InvariantCultureIgnoreCase);
                            if (conn.State != ConnectionState.Open) conn.Open();
                            conn.ChangeDatabase(databaseName);
                        }
                        else
                        {
                            dt = Query(conn, s, parameters);
                        }
                    }
                    if (start < cmdText.Length)
                    {
                        s = cmdText.Substring(start).Trim();

                        if (s.StartsWith("USE ", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string databaseName = s.Substring("USE ", ";", StringComparison.InvariantCultureIgnoreCase);
                            if (conn.State != ConnectionState.Open) conn.Open();
                            conn.ChangeDatabase(databaseName);
                        }
                        else
                        {
                            dt = Query(conn, s, parameters);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SqlServerHelperException(ex, ConnectionString.ConnectionString, cmdText, parameters);
            }
            return dt;
        }

        public static DataTable Query(SqlConnection conn, string cmdText, params SqlParameter[] parameters)
        {
            DataTable dt = new DataTable("QueryResultTable");
            try
            {
                if (!(conn.State == ConnectionState.Open)) conn.Open();
                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.CommandTimeout = Settings.Default.SQLCommandTimeout;
                    foreach (SqlParameter parameter in parameters)
                    {
                        cmd.Parameters.Add(parameter);
                    }
                    using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            dt.Columns.Add(reader.GetName(i));
                        }
                        dt.Load(reader);
                        reader.Close();
                    }
                    cmd.Parameters.Clear();
                }
            }
            catch (Exception ex)
            {
                throw new SqlServerHelperException(ex, ConnectionString.ConnectionString, cmdText, parameters);
            }
            return dt;
        }

        public static DataTable Query(string cmdText, params SqlParameter[] parameters)
        {
            DataTable dt = new DataTable("QueryResultTable");
            ConnectionString.ConnectionTimeout = Settings.Default.SQLConnectionTimeout;
            using (SqlConnection conn = new SqlConnection(ConnectionString.ConnectionString))
            {
                dt = Query(conn, cmdText, parameters);
                conn.Close();
            }
            return dt;
        }

        public static int EnhancedExecute(string cmdText, params SqlParameter[] parameters)
        {
            int index = cmdText.IndexOf(';');
            int start = 0;
            string s = "";
            int rowsAffected = 0;
            try
            {
                ConnectionString.ConnectionTimeout = Settings.Default.SQLConnectionTimeout;
                using (SqlConnection conn = new SqlConnection(ConnectionString.ConnectionString))
                {
                    while (index > 0)
                    {
                        s = cmdText.Substring(start, index - start + 1).Trim();
                        start = index + 1;
                        index = cmdText.IndexOf(';', start);

                        if (s.StartsWith("USE ", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string databaseName = s.Substring("USE ", ";", StringComparison.InvariantCultureIgnoreCase);
                            if (conn.State != ConnectionState.Open) conn.Open();
                            conn.ChangeDatabase(databaseName);
                        }
                        else
                        {
                            rowsAffected = Execute(conn, s, parameters);
                        }
                    }

                    if (start < cmdText.Length)
                    {
                        s = cmdText.Substring(start).Trim();

                        if (s.StartsWith("USE ", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string databaseName = s.Substring("USE ", ";", StringComparison.InvariantCultureIgnoreCase);
                            if (conn.State != ConnectionState.Open) conn.Open();
                            conn.ChangeDatabase(databaseName);
                        }
                        else
                        {
                            rowsAffected = Execute(conn, s, parameters);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Exception innerMostException = ex.InnerMostException();
                if (innerMostException != null && innerMostException is System.Data.SqlClient.SqlException)
                {
                    ExceptionHelper.CentralProcess(ex);
                }
                else
                    throw new SqlServerHelperException(ex, ConnectionString.ConnectionString, cmdText, parameters);
            }

            return rowsAffected;
        }

        public static int Execute(SqlConnection conn, string cmdText, params SqlParameter[] parameters)
        {
            int rowsAffected = 0;
            try
            {
                ConnectionString.ConnectionTimeout = Settings.Default.SQLConnectionTimeout;
                if (conn.State != ConnectionState.Open) conn.Open();
                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.CommandTimeout = Settings.Default.SQLCommandTimeout;
                    foreach (SqlParameter parameter in parameters)
                    {
                        cmd.Parameters.Add(parameter);
                    }
                    rowsAffected = cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                }
            }
            catch (Exception ex)
            {
                throw new SqlServerHelperException(ex, ConnectionString.ConnectionString, cmdText, parameters);
            }

            Log.Info("cmdText: {1}; Rows Affected: {0}".FormatWith(rowsAffected, cmdText));
            return rowsAffected;
        }

        public static int Execute(string cmdText, params SqlParameter[] parameters)
        {
            int rowsAffected = 0;
            ConnectionString.ConnectionTimeout = Settings.Default.SQLConnectionTimeout;
            using (SqlConnection conn = new SqlConnection(ConnectionString.ConnectionString))
            {
                rowsAffected = Execute(conn, cmdText, parameters);
                conn.Close();
            }

            return rowsAffected;
        }

        public static object QueryScaler(SqlConnection conn, string cmdText, params SqlParameter[] parameters)
        {
            try
            {
                if (!(conn.State == ConnectionState.Open)) conn.Open();
                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.CommandTimeout = Settings.Default.SQLCommandTimeout;
                    foreach (SqlParameter parameter in parameters)
                    {
                        cmd.Parameters.Add(parameter);
                    }
                    return cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                throw new SqlServerHelperException(ex, ConnectionString.ConnectionString, cmdText, parameters);
            }
        }

        public static object QueryScaler(string connectionString, string cmdText, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                return QueryScaler(conn, cmdText, parameters);
            }
        }

        public static object QueryScaler(string cmdText, params SqlParameter[] parameters)
        {
            ConnectionString.ConnectionTimeout = Settings.Default.SQLConnectionTimeout;
            return QueryScaler(ConnectionString.ConnectionString, cmdText, parameters);
        }
        #endregion SQL CRUD operations

        #region Other helpers
        private static void OnInfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            StringBuilder sbTrace = new StringBuilder();
            foreach (SqlError info in e.Errors)
            {
                if (info.Class > 10)
                {
                    if (sqlGenuineErrors == null) sqlGenuineErrors = new List<SqlError>();
                    sqlGenuineErrors.Add(info);
                    // TODO: treat this as a genuine error
                    sbTrace.AppendLine("Error occurred!");
                    sbTrace.AppendLine("\tLine Number: {0}".FormatWith(info.LineNumber));
                    sbTrace.AppendLine("\tMessage: {0}".FormatWith(info.Message));
                    sbTrace.AppendLine("\tNumber: {0}".FormatWith(info.Number));
                    sbTrace.AppendLine("\tProcedure: {0}".FormatWith(info.Procedure));
                    sbTrace.AppendLine("\tServer: {0}".FormatWith(info.Server));
                    sbTrace.AppendLine("\tSource: {0}".FormatWith(info.Source));
                    sbTrace.AppendLine("\tState: {0}".FormatWith(info.State));

                    Log.Error(sbTrace.ToString());
                }
                else
                {
                    // TODO: treat this as a progress message
                    //sbTrace.AppendLine("Progress Info:");
                    //sbTrace.AppendLine("\tLine Number: {0}".Format2(info.LineNumber));
                    //sbTrace.AppendLine("\tMessage: {0}".Format2(info.Message));
                    //sbTrace.AppendLine("\tNumber: {0}".Format2(info.Number));
                    //sbTrace.AppendLine("\tProcedure: {0}".Format2(info.Procedure));
                    //sbTrace.AppendLine("\tServer: {0}".Format2(info.Server));
                    //sbTrace.AppendLine("\tSource: {0}".Format2(info.Source));
                    //sbTrace.AppendLine("\tState: {0}".Format2(info.State));

                    //Log.Info(sbTrace.ToString());
                }
            }

            //File.WriteAllText(Path.Combine(Settings.Default.DataImporter_DataDirectory, "SqlServerHelperLog {0}.txt".Format2(DateTime.Now.ToString("yyyy-MM-ddThh-mm-ssZ"))), sbTrace.ToString());
        }

        private static string QuoteIdentifier(string name)
        {
            return "[" + name.Replace("]", "]]") + "]";
        }

        private static string QuoteString(string text)
        {
            return "'" + text.Replace("'", "''") + "'";
        }
        #endregion Other helpers
    }

    public class ConnectionStringHelper
    {
        public static string DefaultConnectionString { get { return "Server=(local);Database=master;Trusted_Connection=True;Connection Timeout=15"; } }

        #region Properties
        private string connectionString;
        public string ConnectionString
        {
            get
            {
                return this.keyValuePairs.ToString(";", "=");
            }
        }

        private Dictionary<string, string> keyValuePairs;
        public Dictionary<string, string> KeyValuePairs { get { return keyValuePairs; } set { keyValuePairs = value; } }

        public string Server
        {
            get
            {
                return this.keyValuePairs.GetValueOrDefault<string>("Server");
            }
            set
            {
                this.keyValuePairs.Upsert<string, string>("Server", value);
            }
        }

        public string Database
        {
            get
            {
                return this.keyValuePairs.GetValueOrDefault<string>("Database");
            }
            set
            {
                this.keyValuePairs.Upsert<string, string>("Database", value);
            }
        }

        public int ConnectionTimeout
        {
            get
            {
                return int.Parse(this.keyValuePairs.GetValueOrDefault<string>("Connection Timeout"));
            }
            set
            {
                this.keyValuePairs.Upsert<string, string>("Connection Timeout", value.ToString());
            }
        }
        #endregion Properties

        public ConnectionStringHelper(string connectionString)
        {
            this.connectionString = connectionString;

            ProcessConnectionString();
        }

        public ConnectionStringHelper()
        {
            this.connectionString = ConnectionStringHelper.DefaultConnectionString;
            ProcessConnectionString();
        }

        #region Methods
        public Dictionary<string, string> ProcessConnectionString()
        {
            return ProcessConnectionString(this.connectionString);
        }

        public Dictionary<string, string> ProcessConnectionString(string connectionString)
        {
            if (this.keyValuePairs == null)
                this.keyValuePairs = new Dictionary<string, string>();
            else
                this.keyValuePairs.Clear();

            string[] lines = connectionString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                int index = line.IndexOf('=');
                if (index >= 0)
                {
                    string key = line.Substring(0, index);
                    string value = line.Substring(index + 1);

                    this.keyValuePairs.Upsert(key, value);
                }
                else
                {
                    this.keyValuePairs.Add(line, "");
                }
            }

            return this.keyValuePairs;
        }
        #endregion Methods

        #region Helper methods
        public static string GetValueByKey(string input, string key)
        {
            Regex regex = new Regex(".*{0}=([^;]*);?.*".FormatWith(key), RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            Match match = regex.Match(input);

            if (match.Success && match.Groups.Count > 0 && match.Groups[1].Success)
                return match.Groups[1].Captures[0].Value.ToString();
            else
                return "";
        }

        public static string SetValueByKey(string input, string key, string value)
        {
            Regex regex = new Regex("(.*{0}=)([^;]*)(;?.*)".FormatWith(key), RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (regex.IsMatch(input))
            {
                string output = regex.Replace(input, (m) =>
                    {
                        if (m.Success && m.Groups.Count > 3)
                        {
                            return m.Groups[1].Value + value + m.Groups[3].Value;
                        }
                        else
                        {
                            return "";
                        }
                    });

                return output;
            }
            else
            {
                input = input.TrimEnd('\r', '\n', ' ', ';');
                input += ";{0}={1};".FormatWith(key, value);

                return input;
            }
        }
        #endregion Helper methods
    }

    public class SqlServerHelperException : Exception {
        private Exception innerException;
        private string commandText;
        private string connectionString;
        private SqlParameter[] parameters;

        public SqlServerHelperException(Exception innerException, string connectionString, string commandText, params SqlParameter[] parameters):base(commandText, innerException)
        {
            this.innerException = innerException;
            this.connectionString = connectionString;
            this.commandText = commandText;
            this.parameters = parameters;
        }

        public SqlServerHelperException(Exception innerException, string connectionString, SqlCommand cmd)
            : base(cmd.CommandText, innerException)
        {
            this.innerException = innerException;
            this.connectionString = connectionString;
            this.commandText = cmd.CommandText;
            List<SqlParameter> list = new List<SqlParameter>();
            foreach (SqlParameter p in cmd.Parameters)
            {
                list.Add(p);
            }

            this.parameters = list.ToArray();
        }

        public override string Message
        {
            get
            {
                if(this.parameters !=null)
                {
                    foreach (SqlParameter p in this.parameters)
                    {
                        this.commandText = this.commandText.Replace("@" + p.ParameterName, p.ParameterValueForSQL());
                    }
                }
                return "ConnectionString: {0}; Command Text: {1}.".FormatWith(this.connectionString, this.commandText);
            }
        }
    }
}
