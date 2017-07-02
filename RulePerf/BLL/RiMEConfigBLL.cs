using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Scs.Test.RiskTools.RulePerf.DAL;
using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
using Microsoft.Scs.Test.RiskTools.RulePerf.Model;
using Microsoft.Scs.Test.RiskTools.RulePerf.Properties;

namespace Microsoft.Scs.Test.RiskTools.RulePerf.BLL
{
    /// <summary>
    /// RiME Config modifications wrapper
    /// </summary>
    public class RiMEConfigBLL
    {
        #region Change Group prop bypassing
        /// <summary>
        /// Gets the new value to store in DB.
        /// </summary>
        /// <param name="oldValueExample">The old value example.</param>
        /// <param name="newValue">The new value.</param>
        /// <returns>New value text</returns>
        public static string GetNewValueToStoreInDB(string oldValueExample, string newValue)
        {
            return oldValueExample.Substring(0, oldValueExample.IndexOf(">") + 1) + newValue + oldValueExample.Substring(oldValueExample.LastIndexOf("</"));
        }

        /// <summary>
        /// Props the change group.
        /// </summary>
        /// <param name="xmlFilePath">The Change Group XML file path.</param>
        /// <param name="xmlns">The xml namespace.</param>
        /// <returns>How many changes have been processed by this method.</returns>
        public static int PropChangeGroup(string xmlFilePath, string xmlns = "urn:schemas.microsoft.com/Commerce/Types/RiAS/2011/04")
        {
            XDocument xdoc = XDocument.Load(xmlFilePath);
            XNamespace ns = xmlns;
            var configChanges = from change in xdoc.Descendants(ns + "ConfigChange")
                                select new
                                {
                                    ConfigObjectType = change.Element(ns + "ConfigObjectType").Value,
                                    ConfigVerb = change.Element(ns + "ConfigVerb").Value,
                                    Key = change.Element(ns + "Key").Value,
                                    NewConfig = change.Element(ns + "NewConfig") != null ? change.Element(ns+"NewConfig").InnerXmlString() : ""
                                };
            int count = 0;
            foreach (var configChange in configChanges)
            {
                Log.Info("Propping this config object:\r\n{0}\r\n", configChange.ToXml());
                count += RiMEConfigBLL.PropChange(configChange.CastToConfigChangeModel());
                Log.Info("Propped count: {0}".FormatWith(count));
            }

            return count;
        }

        /// <summary>
        /// Props a specified change.
        /// </summary>
        /// <param name="configChange">The config change object.</param>
        /// <returns>How many changes have been processed by this method.</returns>
        /// <exception cref="System.NotImplementedException">Unrecognized Change Config Verb encountered.</exception>
        public static int PropChange(ConfigChangeModel configChange)
        {
            int count = 0;
            if (null != configChange)
            {
                switch (configChange.ConfigVerb)
                {
                    case ConfigVerbEnum.Add:
                        count = RiMEConfigBLL.AddConfig(configChange);
                        break;
                    case ConfigVerbEnum.Update:
                        count = RiMEConfigBLL.UpdateConfig(configChange);
                        break;
                    case ConfigVerbEnum.Remove:
                        count = RiMEConfigBLL.RemoveConfig(configChange);
                        break;
                    default:
                        throw new NotImplementedException("Unrecognized Change Config Verb: {0}".FormatWith(configChange.ConfigVerb));
                }
            }
            else
            {
                throw new ArgumentNullException("configChange");
            }

            return count;
        }

        /// <summary>
        /// Updates a specified config.
        /// </summary>
        /// <param name="configChange">The config change object.</param>
        /// <returns>How many changes have been processed by this method.</returns>
        /// <exception cref="System.Exception">Trying to update a config that does not exist in RiME Config.</exception>
        public static int UpdateConfig(ConfigChangeModel configChange)
        {
            RiMEConfigModel rimeConfig = RiMEConfigDAL.GetConfig((int)configChange.ConfigObjectType, configChange.Key.ToString());
            if (rimeConfig != null)
            {
                string oldValue = rimeConfig.Value;
                return RiMEConfigDAL.UpdateConfig(configChange, RiMEConfigBLL.GetNewValueToStoreInDB(oldValue, configChange.NewConfig));
            }
            else
            {
                throw new Exception("Trying to update a config that does not exist in RiME Config. \r\nThis Config Info:\r\n{0}".FormatWith(configChange.ToSerializedXmlString()));
            }
        }

        /// <summary>
        /// Adds a specified config.
        /// </summary>
        /// <param name="configChange">The config change object.</param>
        /// <returns>How many changes have been processed by this method.</returns>
        /// <exception cref="System.Exception">Can't find a proper value format for this new Config Change to store in the RiMEConfig.dbo.Config table. This is because there seems no existed configs that are in the same Config Object Type as this one (has the same value on iConfigObjectType field), so I don't know what should the nvcValue be like for this config.</exception>
        public static int AddConfig(ConfigChangeModel configChange)
        {
            if (configChange.Key <= 0 || RiMEConfigDAL.IsKeyConflicted((int)configChange.ConfigObjectType, configChange.Key.ToString()))
            {
                configChange.Key = RiMEConfigDAL.GetMaxKeyOfConfig((int)configChange.ConfigObjectType).ToInt() + 1;
                configChange.NewConfig = configChange.NewConfig.Replace("<Id>0</Id>", "<Id>{0}</Id>".FormatWith(configChange.Key));
            }

            if (!RiMEConfigDAL.IsKeyConflicted((int)configChange.ConfigObjectType, configChange.Key.ToString()))
            {
                Log.Info("Adding new config with key {0}".FormatWith(configChange.Key.ToString()));
                List<RiMEConfigModel> configs = RiMEConfigDAL.GetConfig((int)configChange.ConfigObjectType);
                if (configs.Count > 0)
                {
                    return RiMEConfigDAL.AddConfig(configChange, RiMEConfigBLL.GetNewValueToStoreInDB(configs[0].Value, configChange.NewConfig));
                }
                else
                {
                    Exception ex = new Exception("Can't find a proper value format for this new Config Change to store in the RiMEConfig.dbo.Config table. This is because there seems no existed configs that are in the same Config Object Type as this one (has the same value on iConfigObjectType field), so I don't know what should the nvcValue be like for this config. \r\nThis Config Info:\r\n{0}".FormatWith(configChange.ToSerializedXmlString()));
                    ExceptionHelper.CentralProcess(ex);
                    throw ex;
                }
            }
            else
            {
                Exception ex = new Exception("Can't find a proper key for this new Config Change. \r\nThis Config Info:\r\n{0}".FormatWith(configChange.ToSerializedXmlString()));
                ExceptionHelper.CentralProcess(ex);
                throw ex;
            }
        }

        /// <summary>
        /// Removes a specified config.
        /// </summary>
        /// <param name="configChange">The config change object.</param>
        /// <returns>
        /// How many changes have been processed by this method.
        /// </returns>
        /// <exception cref="System.Exception">Trying to delete a config that does not exist in RiME Config.</exception>
        public static int RemoveConfig(ConfigChangeModel configChange)
        {
            RiMEConfigModel rimeConfig = RiMEConfigDAL.GetConfig((int)configChange.ConfigObjectType, configChange.Key.ToString());
            if (rimeConfig != null)
            {
                return RiMEConfigDAL.RemoveConfig(configChange);
            }
            else
            {
                throw new Exception("Trying to delete a config that does not exist in RiME Config. \r\nThis Config Info:\r\n{0}".FormatWith(configChange.ToSerializedXmlString()));
            }
        }
        #endregion Change Group prop bypassing

        #region Azure write enable / disable
        /// <summary>
        /// Gets the azure write settings.
        /// </summary>
        /// <returns>Data table contains the azure write settings</returns>
        public static DataTable GetAzureWriteSettings()
        {
            string sql = @"IF EXISTS (SELECT * FROM tempdb.dbo.sysobjects WHERE id = OBJECT_ID('tempdb.dbo.#Keys'))
	                        DROP TABLE dbo.#Keys;
                        CREATE TABLE #Keys (vcKey varchar(900));
                        INSERT INTO #Keys (vcKey) VALUES {0};
                        SELECT DISTINCT C.vcKey, C.nvcValue, C.iConfigObjectType FROM RiMEConfig.dbo.Config C JOIN #Keys K ON C.vcKey LIKE '%.%.' + K.vcKey OR C.vcKey LIKE '%.%' + K.vcKey + '%.%';";
            string values = string.Join(",", Settings.Default.AzureWriteKeysAndValues_Disable.Cast<string>().Select(value => "('" + value.Split(new char[] { '\t', '=' }, StringSplitOptions.RemoveEmptyEntries)[0].Replace("'", "''") + "')").ToArray<string>());
            sql = sql.FormatWith(values);

            string originalServer = SqlServerHelper.ConnectionString.Server;

            ServerAssignmentModel serverAssignment = ServerAssignmentModel.GetInstance();
            if (serverAssignment.CpRkRiskConfigServers.Length > 0)
            {
                DataTable dt = new DataTable();
                for (int i = 0; i < serverAssignment.CpRkRiskConfigServers.Length; i++)
                {
                    SqlServerHelper.ConnectionString.Server = serverAssignment.CpRkRiskConfigServers[i];
                    try
                    {
                        dt = SqlServerHelper.Query(sql);
                        break;
                    }
                    catch (SqlServerHelperException ex)
                    {
                        if (!(ex.InnerException != null && ex.InnerException is System.Data.SqlClient.SqlException &&
                            ex.InnerException.Message.Equals(@"The database ""RiMEConfig"" cannot be opened. It is acting as a mirror database.")))
                        {
                            ////ExceptionHelper.CentralProcess(ex);
                        }
                        else
                        {
                            ExceptionHelper.CentralProcess(ex);
                        }

                        // Continue trying SQL query on another server.
                        continue;
                    }
                    catch(Exception ex)
                    {
                        ExceptionHelper.CentralProcess(ex);
                        continue;
                    }
                }
                SqlServerHelper.ConnectionString.Server = originalServer;
                return dt;
            }
            else
                return SqlServerHelper.Query(sql);
        }

        /// <summary>
        /// Enables the azure write.
        /// </summary>
        /// <returns>Rows affected.</returns>
        public static int EnableAzureWrite()
        {
            string sql = @"IF EXISTS (SELECT * FROM tempdb.dbo.sysobjects WHERE id = OBJECT_ID('tempdb.dbo.#Keys'))
	                            DROP TABLE dbo.#Keys;                            
                            CREATE TABLE #Keys (vcKey varchar(900), nvcValue nvarchar(MAX));
                            INSERT INTO #Keys (vcKey, nvcValue) VALUES {0};
                            IF EXISTS (SELECT * FROM tempdb.dbo.sysobjects WHERE id = OBJECT_ID('tempdb.dbo.#NewKeysAndValues')) DROP TABLE dbo.#NewKeysAndValues;
                            SELECT DISTINCT C.vcKey, K.nvcValue INTO #NewKeysAndValues FROM RiMEConfig.dbo.Config C JOIN #Keys K ON C.vcKey LIKE '%.%.' + K.vcKey OR C.vcKey LIKE '%.%' + K.vcKey + '%.%';
                            UPDATE RiMEConfig.dbo.Config SET nvcValue = (SELECT KV.nvcValue FROM #NewKeysAndValues KV WHERE KV.vcKey = Config.vcKey) WHERE EXISTS(SELECT KV.nvcValue FROM #NewKeysAndValues KV WHERE KV.vcKey = Config.vcKey);";
            string values = string.Join(",", Settings.Default.AzureWriteKeysAndValues_Enable.Cast<string>().Select(value => { string[] arr = value.Split(new char[] { '\t', '=' }, StringSplitOptions.RemoveEmptyEntries); return "('" + arr[0].Replace("'", "''") + "', N'" + arr[1].Replace("'", "''") + "')"; }).ToArray<string>());
            sql = sql.FormatWith(values);
            string originalServer = SqlServerHelper.ConnectionString.Server;

            ServerAssignmentModel serverAssignment = ServerAssignmentModel.GetInstance();
            if (serverAssignment.CpRkRiskConfigServers.Length > 0)
            {
                int rowsAffected = 0;
                for (int i = 0; i < serverAssignment.CpRkRiskConfigServers.Length; i++)
                {
                    SqlServerHelper.ConnectionString.Server = serverAssignment.CpRkRiskConfigServers[i];
                    try
                    {
                        rowsAffected = SqlServerHelper.Execute(sql);
                        break;
                    }
                    catch (SqlServerHelperException ex)
                    {
                        if (!(ex.InnerException != null && ex.InnerException is System.Data.SqlClient.SqlException &&
                            ex.InnerException.Message.Equals(@"The database ""RiMEConfig"" cannot be opened. It is acting as a mirror database.")))
                        {
                            ExceptionHelper.CentralProcess(ex);
                        }
                        else
                        {
                        }

                        continue;
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.CentralProcess(ex);
                        continue;
                    }
                }

                SqlServerHelper.ConnectionString.Server = originalServer;
                return rowsAffected;
            }
            else
            {
                return SqlServerHelper.Execute(sql);
            }
        }

        /// <summary>
        /// Disables the azure write.
        /// </summary>
        /// <returns>Rows affected.</returns>
        /// <exception cref="System.Exception">Tried all {0} 'CP RK Database Servers' as the target sql server on which to execute the following sql statement but none of the trials succeeded: \r\n{1}\r\nThe tried servers are: {2}.Format2(serverAssignment.CpRkDatabaseServers.Length, sql, string.Join(, , serverAssignment.CpRkDatabaseServers))</exception>
        public static int DisableAzureWrite()
        {
            string sql = @"IF EXISTS (SELECT * FROM tempdb.dbo.sysobjects WHERE id = OBJECT_ID('tempdb.dbo.#Keys'))
	                            DROP TABLE dbo.#Keys;                            
                            CREATE TABLE #Keys (vcKey varchar(900), nvcValue nvarchar(MAX));
                            INSERT INTO #Keys (vcKey, nvcValue) VALUES {0};
                            IF EXISTS (SELECT * FROM tempdb.dbo.sysobjects WHERE id = OBJECT_ID('tempdb.dbo.#NewKeysAndValues')) DROP TABLE dbo.#NewKeysAndValues;
                            SELECT DISTINCT C.vcKey, K.nvcValue INTO #NewKeysAndValues FROM RiMEConfig.dbo.Config C JOIN #Keys K ON C.vcKey LIKE '%.%.' + K.vcKey OR C.vcKey LIKE '%.%' + K.vcKey + '%.%';
                            UPDATE RiMEConfig.dbo.Config SET nvcValue = (SELECT KV.nvcValue FROM #NewKeysAndValues KV WHERE KV.vcKey = Config.vcKey) WHERE EXISTS(SELECT KV.nvcValue FROM #NewKeysAndValues KV WHERE KV.vcKey = Config.vcKey);";
            string values = string.Join(",", Settings.Default.AzureWriteKeysAndValues_Disable.Cast<string>().Select(value => { string[] arr = value.Split(new char[] { '\t', '=' }, StringSplitOptions.RemoveEmptyEntries); return "('" + arr[0].Replace("'", "''") + "', N'" + arr[1].Replace("'", "''") + "')"; }).ToArray<string>());
            sql = sql.FormatWith(values);
            string originalServer = SqlServerHelper.ConnectionString.Server;

            ServerAssignmentModel serverAssignment = ServerAssignmentModel.GetInstance();
            if (serverAssignment.CpRkRiskConfigServers.Length > 0)
            {
                int rowsAffected = 0;
                int exceptionCount = 0;
                int mirrorExceptionCount = 0;
                for (int i = 0; i < serverAssignment.CpRkDatabaseServers.Length; i++)
                {
                    SqlServerHelper.ConnectionString.Server = serverAssignment.CpRkRiskConfigServers[i];
                    try
                    {
                        rowsAffected = SqlServerHelper.Execute(sql);
                        break;
                    }
                    catch (SqlServerHelperException ex)
                    {
                        if (!(ex.InnerException != null && ex.InnerException is System.Data.SqlClient.SqlException &&
                            ex.InnerException.Message.Equals(@"The database ""RiMEConfig"" cannot be opened. It is acting as a mirror database.")))
                        {
                            ExceptionHelper.CentralProcess(ex);
                        }
                        else
                        {
                            mirrorExceptionCount++;
                        }
                        exceptionCount++;
                        continue;
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.CentralProcess(ex);
                        exceptionCount++;
                        continue;
                    }
                }

                if (exceptionCount == serverAssignment.CpRkRiskConfigServers.Length)
                {
                    throw new Exception("Tried all {0} 'CP RK Risk Config' Servers as the target sql server on which to execute the following sql statement but none of the trials succeeded: \r\n{1}\r\nThe tried servers are: {2}".FormatWith(serverAssignment.CpRkRiskConfigServers.Length, sql, string.Join(", ", serverAssignment.CpRkRiskConfigServers)));
                }

                if (mirrorExceptionCount == serverAssignment.CpRkRiskConfigServers.Length)
                {
                    throw new Exception(@"The database ""RiMEConfig"" cannot be opened on all these 'CP RK Risk Config' Server: {0}. It is acting as a mirror database.".FormatWith(string.Join(", ", serverAssignment.CpRkRiskConfigServers)));
                }

                SqlServerHelper.ConnectionString.Server = originalServer;
                return rowsAffected;
            }
            else
            {
                return SqlServerHelper.Execute(sql);
            }
        }

        /// <summary>
        /// Setups the global settings.
        /// </summary>
        /// <returns>Rows affected.</returns>
        /// <exception cref="System.Exception">Tried all {0} 'CP RK Database Servers' as the target sql server on which to execute the following sql statement but none of the trials succeeded: \r\n{1}\r\nThe tried servers are: {2}.Format2(serverAssignment.CpRkDatabaseServers.Length, sql, string.Join(, , serverAssignment.CpRkDatabaseServers))</exception>
        public static int SetupGlobalSettings(
            string netUserName = "Administrator", string netPassword = "#Bugsfor$", string netDomain = ".",
            string sqlServerWindowsAuthUserName = "Administrator", 
            string sqlServerWindowsAuthPassword = "#Bugsfor$",
            string sqlServerWindowsAuthDomain = "."
            )
        {
            string sql = @"IF EXISTS (SELECT * FROM tempdb.dbo.sysobjects WHERE id = OBJECT_ID('tempdb.dbo.#Keys'))
	                            DROP TABLE dbo.#Keys;                            
                            CREATE TABLE #Keys (vcKey varchar(900), nvcValue nvarchar(MAX));
                            INSERT INTO #Keys (vcKey, nvcValue) VALUES {0};
                            IF EXISTS (SELECT * FROM tempdb.dbo.sysobjects WHERE id = OBJECT_ID('tempdb.dbo.#NewKeysAndValues')) DROP TABLE dbo.#NewKeysAndValues;
                            SELECT DISTINCT C.vcKey, K.nvcValue INTO #NewKeysAndValues FROM RiMEConfig.dbo.Config C JOIN #Keys K ON C.vcKey LIKE '%.%.' + K.vcKey OR C.vcKey LIKE '%.%' + K.vcKey + '%.%';
                            UPDATE RiMEConfig.dbo.Config SET nvcValue = (SELECT KV.nvcValue FROM #NewKeysAndValues KV WHERE KV.vcKey = Config.vcKey) WHERE EXISTS(SELECT KV.nvcValue FROM #NewKeysAndValues KV WHERE KV.vcKey = Config.vcKey);";
            string values = string.Join(",", Settings.Default.GlobalSettings.Cast<string>().Select(value => { string[] arr = value.Split(new char[] { '\t', '=' }, StringSplitOptions.RemoveEmptyEntries); return "('" + arr[0].Replace("'", "''").Replace("%quote%", "\"") + "', N'" + arr[1].Replace("'", "''").Replace("%quote%", "\"") + "')"; }).ToArray<string>());
            sql = sql.FormatWith(values);
            string originalServer = SqlServerHelper.ConnectionString.Server;

            ServerAssignmentModel serverAssignment = ServerAssignmentModel.GetInstance(netUserName, netPassword, netDomain);
            //if (serverAssignment.CpRkRiskConfigServers.Length > 0)
            if (serverAssignment.CpRkDatabaseServers.Length > 0)
            {
                int rowsAffected = 0;
                int exceptionCount = 0;
                int mirrorExceptionCount = 0;

                for (int i = 0; i < serverAssignment.CpRkDatabaseServers.Length; i++)
                {
                    Impersonator impersonator = new Impersonator(sqlServerWindowsAuthUserName, sqlServerWindowsAuthDomain, sqlServerWindowsAuthPassword);
                    #region Sql server connection and sql execution
                    SqlServerHelper.ConnectionString.Server = serverAssignment.CpRkDatabaseServers[i];
                    try
                    {
                        Log.Info("Executing command on server '{1}': \r\n{0}".FormatWith(sql, SqlServerHelper.ConnectionString.Server));
                        rowsAffected = SqlServerHelper.Execute(sql);
                        break;
                    }
                    catch (SqlServerHelperException ex)
                    {
                        if (!(ex.InnerException != null && ex.InnerException is System.Data.SqlClient.SqlException &&
                            ex.InnerException.Message.Equals(@"The database ""RiMEConfig"" cannot be opened. It is acting as a mirror database.")))
                        {
                            ExceptionHelper.CentralProcess(ex);
                        }
                        else
                        {
                            mirrorExceptionCount++;
                        }
                        exceptionCount++;
                        continue;
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.CentralProcess(ex);
                        exceptionCount++;
                        continue;
                    }
                    finally
                    {
                        impersonator.Undo();
                    }
                    #endregion Sql server connection and sql execution
                }
                
                if (mirrorExceptionCount == serverAssignment.CpRkDatabaseServers.Length)
                {
                    throw new Exception(@"The database ""RiMEConfig"" cannot be opened on all these 'CP RK Risk Config' Server: {0}. It is acting as a mirror database.".FormatWith(string.Join(", ", serverAssignment.CpRkDatabaseServers)));
                }

                if (exceptionCount == serverAssignment.CpRkDatabaseServers.Length)
                {
                    throw new Exception("Tried all {0} 'CP RK Risk Config' Servers as the target sql server on which to execute the following sql statement but none of the trials succeeded: \r\n{1}\r\nThe tried servers are: {2}".FormatWith(serverAssignment.CpRkDatabaseServers.Length, sql, string.Join(", ", serverAssignment.CpRkDatabaseServers)));
                }

                SqlServerHelper.ConnectionString.Server = originalServer;
                return rowsAffected;
            }
            else
            {
                return SqlServerHelper.Execute(sql);
            }
        }
        #endregion Azure write enable /disable
    }
}
