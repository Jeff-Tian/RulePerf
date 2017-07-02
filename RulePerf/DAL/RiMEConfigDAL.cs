using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
using Microsoft.Scs.Test.RiskTools.RulePerf.Model;

namespace Microsoft.Scs.Test.RiskTools.RulePerf.DAL
{
    /// <summary>
    /// The wrapper to operate on the RiMEConfig database
    /// </summary>
    public class RiMEConfigDAL
    {
        #region Change Group prop bypassing
        /// <summary>
        /// Gets a config by its key and configObjectType.
        /// </summary>
        /// <param name="configObjectType">The integer value that represents the type of the config object.</param>
        /// <param name="key">The key.</param>
        /// <returns>The config object</returns>
        public static RiMEConfigModel GetConfig(int configObjectType, string key)
        {
            string sql = "SELECT iConfigObjectType, vcKey, nvcValue, dtUpdatedTime FROM RiMEConfig.dbo.Config WHERE iConfigObjectType = @configObjectType AND vcKey = @key";
            DataTable dt = SqlServerHelper.Query(sql, new SqlParameter("configObjectType", configObjectType), new SqlParameter("key", key));
            if (dt.Rows.Count > 0)
            {
                RiMEConfigModel model = new RiMEConfigModel() { ConfigObjectType = dt.Rows[0][0].ToInt(), Key = dt.Rows[0][1].ToString(), Value = dt.Rows[0][2].ToString(), UpdatedTime = dt.Rows[0][3].ToDateTime() };
                return model;
            }
            else
                return null;
        }

        /// <summary>
        /// Gets a list of config objects that are all in the specified type.
        /// </summary>
        /// <param name="configObjectType">A integer value represents the type of the config object.</param>
        /// <returns>A list of config objects.</returns>
        public static List<RiMEConfigModel> GetConfig(int configObjectType)
        {
            List<RiMEConfigModel> result = new List<RiMEConfigModel>();
            string sql = "SELECT iConfigObjectType, vcKey, nvcValue, dtUpdatedTime FROM RiMEConfig.dbo.Config WHERE iConfigObjectType = @configObjectType";
            DataTable dt = SqlServerHelper.Query(sql, new SqlParameter("configObjectType", configObjectType));
            for(int i = 0; i < dt.Rows.Count; i++) 
            {
                RiMEConfigModel model = new RiMEConfigModel() { ConfigObjectType = dt.Rows[i][0].ToInt(), Key = dt.Rows[i][1].ToString(), Value = dt.Rows[i][2].ToString(), UpdatedTime = dt.Rows[i][3].ToDateTime() };
                result.Add(model);
            }

            return result;
        }

        /// <summary>
        /// Updates a specified config's value.
        /// </summary>
        /// <param name="newConfig">The config object that needs to be updated.</param>
        /// <param name="newValue">The new value.</param>
        /// <returns>How many rows in the [RiMEConfig].[dbo].[Config] table have been affected.</returns>
        public static int UpdateConfig(ConfigChangeModel newConfig, string newValue)
        {
            string sql = "UPDATE RiMEConfig.dbo.Config SET nvcValue = @newConfig WHERE iConfigObjectType = @configObjectType AND vcKey = @key";
            int rowsAffected = SqlServerHelper.Execute(sql, 
                new SqlParameter("newConfig", newValue.Replace("'", "''")), 
                new SqlParameter("configObjectType", (int)newConfig.ConfigObjectType),
                new SqlParameter("key", newConfig.Key.ToString()));

            return rowsAffected;
        }

        /// <summary>
        /// Adds a new config to the [RiMEConfig].[dbo].[Config] table.
        /// </summary>
        /// <param name="newConfig">The new config object.</param>
        /// <param name="newValue">The new value.</param>
        /// <returns>How many rows in [RiMEConfig].[dbo].[Config] table have been affected</returns>
        public static int AddConfig(ConfigChangeModel newConfig, string newValue)
        {
            string sql = "INSERT INTO RiMEConfig.dbo.Config (iConfigObjectType, vcKey, nvcValue, dtUpdatedTime) VALUES (@configObjectType, @key, @newValue, GETDATE());";
            int rowsAffected = SqlServerHelper.Execute(sql, new SqlParameter("configObjectType", (int)newConfig.ConfigObjectType),
                new SqlParameter("key", newConfig.Key.ToString()), new SqlParameter("newValue", newValue));

            return rowsAffected;
        }

        /// <summary>
        /// Removes a config from [RiMEConfig].[dbo].[Config] table.
        /// </summary>
        /// <param name="model">The config object that needs to be removed from [RiMEConfig].[dbo].[Config] table.</param>
        /// <returns>How many rows in [RiMEConfig].[dbo].[Config] table have been affected.</returns>
        public static int RemoveConfig(ConfigChangeModel model)
        {
            string sql = "DELETE FROM RiMEConfig.dbo.Config WHERE vcKey = @key AND iConfigObjectType = @configObjectType";
            int rowsAffected = SqlServerHelper.Execute(
                sql,
                new SqlParameter("key", model.Key.ToString()),
                new SqlParameter("configObjectType", (int)model.ConfigObjectType)
                );

            return rowsAffected;
        }

        /// <summary>
        /// Gets the maximum key value allocated in the [RiMEConfig].[dbo].[Config] table for a special type.
        /// </summary>
        /// <param name="configObjectType">A integer value that represents the type of the config object.</param>
        /// <returns>The maximum key value.</returns>
        public static string GetMaxKeyOfConfig(int configObjectType)
        {
            string sql = "SELECT TOP 1 vcKey FROM RiMEConfig.dbo.Config WHERE iConfigObjectType = @configObjectType ORDER BY CAST(vcKey AS INT) DESC";
            return (string)SqlServerHelper.QueryScaler(sql, new SqlParameter("configObjectType", configObjectType));
        }

        public static bool IsKeyConflicted(int configObjectType, string key)
        {
            string sql = "SELECT * FROM RiMEConfig.dbo.Config WHERE iConfigObjectType = @configObjectType AND vcKey = @key";
            return SqlServerHelper.Query(sql, new SqlParameter("configObjectType", configObjectType), new SqlParameter("key", key)).Rows.Count > 0;
        }

        #endregion Change Group prop bypassing

        #region Azure write enable / disable
        
        #endregion Azure write enable /disable
    }
}
