using System;
using System.Text;
using Microsoft.Scs.Test.RiskTools.RulePerf.DAL;
using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;

namespace Microsoft.Scs.Test.RiskTools.RulePerf.BLL
{
    public class BackupDatabaseBLL
    {
        public static bool BackupDatabases(out string logText, string sqlServer, params string[] databaseNames)
        {
            StringBuilder log = new StringBuilder();
            int success = 0, failed = 0;
            if (databaseNames.Length > 0)
            {
                string originalServer = SqlServerHelper.ConnectionString.Server;
                SqlServerHelper.ConnectionString.Server = sqlServer;
                for (int i = 0; i < databaseNames.Length; i++)
                {
                    try
                    {
                        if (SqlServerHelper.BackupDatabase(databaseNames[i]))
                        {
                            log.AppendLine(@"Backup database {0}\{1} successfully.".FormatWith(sqlServer, databaseNames[i]));
                            success++;
                        }
                        else
                        {
                            log.AppendLine(@"Backup database {0}\{1} failed!".FormatWith(sqlServer, databaseNames[i]));
                            failed++;
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.CentralProcess(ex);
                        log.AppendLine(@"Backup database {0}\{1} failed!".FormatWith(sqlServer, databaseNames[i]));
                        log.AppendLine(ExceptionHelper.ExceptionLog(ex));
                        continue;
                    }
                }
                SqlServerHelper.ConnectionString.Server = originalServer;
            }

            log.AppendLine("Total databases to be backed up on server '{4}': {0}, Succeeded: {1}, Failed: {2}, Aborted: {3}".FormatWith(databaseNames.Length, success, failed, databaseNames.Length - success - failed, sqlServer));

            logText = log.ToString();
            return success == databaseNames.Length;
        }

        public static string BackupDatabases(string sqlServer, params string[] databaseNames)
        {
            string log = "";
            BackupDatabases(out log, sqlServer, databaseNames);
            return log;
        }
    }
}
