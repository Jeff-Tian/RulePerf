// -----------------------------------------------------------------------
// <copyright file="DataImporter.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf.BLL
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using Microsoft.Scs.Test.RiskTools.RulePerf.DAL;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Properties;

    /// <summary>
    /// Import data to SQL Server
    /// </summary>
    public class DataImporter
    {
        /// <summary>
        /// The BCP import command format
        /// </summary>
        /// <remarks>
        /// Example: bcp.exe Lists.dbo.list_item_data in D:\v-jetian\Lists.dbo.ListItemData.txt -Slocalhost -T -c
        /// </remarks>
        private readonly string bcpImportCommandFormat = "{0} in {1} -S{2} -T -c";

        /// <summary>
        /// The target data file info list
        /// </summary>
        private List<DataFileInfo> targetDataFileInfoList;

        /// <summary>
        /// Gets the target data file info list.
        /// </summary>
        /// <value>
        /// The target data file info list.
        /// </value>
        public List<DataFileInfo> TargetDataFileInfoList 
        { 
            get 
            {
                return targetDataFileInfoList; 
            } 
        }

        /// <summary>
        /// The target databases
        /// </summary>
        private List<string> targetDatabases;

        /// <summary>
        /// Gets the target databases.
        /// </summary>
        /// <value>
        /// The target databases.
        /// </value>
        public List<string> TargetDatabases 
        { 
            get 
            { 
                return targetDatabases; 
            } 
        }

        /// <summary>
        /// The indexes diabled list
        /// </summary>
        private List<DataFileInfo> indexesDiabledList = new List<DataFileInfo>();

        /// <summary>
        /// The data cleared list
        /// </summary>
        private List<DataFileInfo> dataClearedList = new List<DataFileInfo>();

        /// <summary>
        /// The exceptions
        /// </summary>
        private List<Exception> exceptions = new List<Exception>();

        /// <summary>
        /// Gets or sets the exceptions.
        /// </summary>
        /// <value>
        /// The exceptions.
        /// </value>
        public List<Exception> Exceptions 
        { 
            get 
            { 
                return exceptions; 
            } 
            
            set 
            { 
                exceptions = value; 
            } 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataImporter" /> class.
        /// </summary>
        public DataImporter()
        {
            GetTargetTableFullName();
        }

        /// <summary>
        /// Gets the command.
        /// </summary>
        /// <param name="dfi">The dfi.</param>
        /// <returns></returns>
        private string GetCommand(DataFileInfo dfi)
        {
            return bcpImportCommandFormat.FormatWith(dfi.TargetTableFullName, dfi.DataFileFullPath, dfi.TargetServer);
        }

        /// <summary>
        /// Get data files from bed transfer share folder
        /// </summary>
        /// <param name="sourceFolder">The source folder.</param>
        /// <param name="destFolder">The dest folder.</param>
        /// <param name="deleteSource">if set to <c>true</c> [delete source].</param>
        /// <returns>How many files have been copied.</returns>
        /// <exception cref="System.IO.DirectoryNotFoundException">The path '{0}' can't be found or accessed. Please check if it exists or check if this machine has the permission to access it or check if the network is working..Format2(di.FullName)</exception>
        public static int CopyDataFiles(string sourceFolder, string destFolder, bool deleteSource = true)
        {
            int i = 0;
            DirectoryInfo di = new DirectoryInfo(sourceFolder);
            if (di.Exists)
            {
                Log.Info("Copying data files from '{0}' to '{1}'...", sourceFolder, destFolder);
                foreach (FileInfo fi in di.GetFiles("*.gz", SearchOption.TopDirectoryOnly))
                {
                    string destFullName = Path.Combine(destFolder, fi.Name);
                    Log.Info("Copying file '{0}' from '{1}' to '{2}'...", fi.Name, fi.FullName, destFullName);
                    fi.CopyTo(destFullName, true);
                    Log.Info("Copied file '{0}' from '{1}' to '{2}'.", fi.Name, fi.FullName, destFullName);
                    if (deleteSource)
                    {
                        fi.Delete();
                        Log.Info("Deleted file '{0}' from '{1}'.", fi.Name, fi.FullName);
                    }
                    i++;
                }
                Log.Info("Copied {0} data file(s) from '{0}' to '{1}'.", i, sourceFolder, destFolder);
            }
            else
            {
                throw new DirectoryNotFoundException("The path '{0}' can't be found or accessed. Please check if it exists or check if this machine has the permission to access it or check if the network is working.".FormatWith(di.FullName));
            }

            return i;
        }

        /// <summary>
        /// Decompresses the data files.
        /// </summary>
        private void DecompressDataFiles()
        {
            DirectoryInfo di = new DirectoryInfo(Settings.Default.DataImporter_DataDirectory);
            Log.Info("Decompressing data files under '{0}'...".FormatWith(Settings.Default.DataImporter_DataDirectory));
            if (di.Exists)
            {
                foreach (FileInfo fi in di.GetFiles("*.gz", SearchOption.TopDirectoryOnly))
                {
                    Log.Info("Decompressing data file '{0}'...".FormatWith(fi.FullName));
                    ZipHelper.DecompressFile(fi.FullName);
                    Log.Info("Decompressed data file '{0}'.", fi.FullName);
                }
            }
        }

        /// <summary>
        /// Gets the full name of the target table.
        /// </summary>
        private void GetTargetTableFullName()
        {
            try
            {
                CopyDataFiles(Settings.Default.BedTransferFolder, Settings.Default.DataImporter_DataDirectory);
                DecompressDataFiles();
            }
            catch (Exception ex)
            {
                ExceptionHelper.CentralProcess(ex);
            }

            targetDataFileInfoList = new List<DataFileInfo>();
            targetDatabases = new List<string>();
            DirectoryInfo di = new DirectoryInfo(Settings.Default.DataImporter_DataDirectory);
            Log.Info("Read data files under '{0}'...".FormatWith(Settings.Default.DataImporter_DataDirectory));
            if (di.Exists)
            {
                foreach (FileInfo fi in di.GetFiles("*.txt", SearchOption.TopDirectoryOnly))
                {
                    DataFileInfo dfi = new DataFileInfo(fi.Name);
                    if (dfi.IsValid())
                    {
                        targetDataFileInfoList.Add(dfi);
                        if (!targetDatabases.Contains(dfi.TargetServer + "\\" + dfi.TargetDatabase))
                        {
                            targetDatabases.Add(dfi.TargetServer + "\\" + dfi.TargetDatabase);
                            Log.Info("Adding targetDatabase: {0}".FormatWith(targetDatabases[targetDatabases.Count - 1]));
                        }
                    }
                }
                Log.Info("Found {0} datafile(s) under the directory '{1}".FormatWith(targetDataFileInfoList.Count, di.FullName));
            }
            else
            {
                Log.Error("The specified path '{0}' does not exist.".FormatWith(Settings.Default.DataImporter_DataDirectory));
            }
        }

        /// <summary>
        /// Imports the specified CMD.
        /// </summary>
        /// <param name="cmd">The CMD.</param>
        /// <param name="dfi">The dfi.</param>
        /// <returns><see cref="CmdHelper"/>'s Last Exit code</returns>
        public int Import(CmdHelper cmd, DataFileInfo dfi)
        {
            ////Process.Start("bcp.exe", GetCommand(tableFullName));
            cmd.ExecuteCommand(
                "{0} ".FormatWith(Settings.Default.DataImporter_BCP_Path) + GetCommand(dfi),
                Properties.Settings.Default.CommandsUserName,
                Properties.Settings.Default.CommandsPassword,
                Properties.Settings.Default.CommandsDomain
                );
            return cmd.LastExitCode;
        }

        /// <summary>
        /// Does the sequence.
        /// </summary>
        /// <param name="isAContinueRun">Whether this run is a continue run.</param>
        /// <returns>True if success else false.</returns>
        public bool DoSequence(bool isAContinueRun = false)
        {
            try
            {
                Impersonator impersonator = new Impersonator(
                    Properties.Settings.Default.CommandsUserName,
                    Properties.Settings.Default.CommandsDomain,
                    Properties.Settings.Default.CommandsPassword
                    );
                // 1. List the databases that would be impacted
                // Already listed in the contruction function

                // 2. Backup the databases if exist, else created a new one
                BackupDatabasesAndDisableConstraints(isAContinueRun);
                // 3. Import data into the databases listed 
                ImportDataIntoDatabasesListed(isAContinueRun);
                //      4) Enalbe all constraints on all tables in the database
                EnableConstraints();

                EnableIndexes();

                return this.exceptions == null || this.exceptions.Count <= 0;
            }
            catch (Exception ex)
            {
                ExceptionHelper.CentralProcess(ex);
                return false;
            }
        }

        /// <summary>
        /// Backups the databases and disable constraints.
        /// </summary>
        /// <exception cref="System.Exception">Backup database failed. Importing stopped.</exception>
        private void BackupDatabasesAndDisableConstraints(bool isAContinousRun = false)
        {
            foreach (string databaseName in targetDatabases)
            {
                string[] a = databaseName.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                string dbName = databaseName;
                string originalServer = SqlServerHelper.ConnectionString.Server;
                if (a.Length >= 2)
                {
                    dbName = a[1];
                    SqlServerHelper.ConnectionString.Server = a[0];
                }

                try
                {
                    Log.Info("Backup databases {0} on {1}...", dbName, SqlServerHelper.ConnectionString.Server);
                    if (SqlServerHelper.DoesDatabaseExist(dbName))
                    {
                        if (!isAContinousRun)
                        {
                            if (!SqlServerHelper.BackupDatabase(dbName))
                            {
                                throw new Exception("Backup database {0} on {1} failed. Importing stopped.".FormatWith(dbName, SqlServerHelper.ConnectionString.Server));
                            }
                            else
                            {
                                Log.Info("Backuped databases {0} on {1}.", dbName, SqlServerHelper.ConnectionString.Server);
                            }
                        }

                        // Disable all constraints on all tables in the database                        
                        Log.Info("Disable all constraints on all tables in the database {0} on {1}...".FormatWith(a[1], SqlServerHelper.ConnectionString.Server));
                        DataTable dt = SqlServerHelper.GetAllUserTableNamesOfDatabase(dbName);
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            string tableName = dt.Rows[i][0].ToString();
                            string cmdText = "ALTER TABLE {0}.{1}.{2} NOCHECK CONSTRAINT ALL".FormatWith(dbName, "dbo", tableName);

                            try
                            {
                                SqlServerHelper.Execute(cmdText);
                                Log.Info("Disabled {0} constraint(s) on table {1} in the database {2} on {3}.".FormatWith("all", tableName, a[1], SqlServerHelper.ConnectionString.Server));
                            }
                            catch(SqlServerHelperException ex)
                            {
                                if (ex.InnerException != null && ex.InnerException is System.Data.SqlClient.SqlException && ex.InnerException.Message.StartsWith("Schema change failed on object", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    // try again
                                    System.Threading.Thread.Sleep(1000);
                                    SqlServerHelper.Execute(cmdText);
                                }
                                else
                                {
                                    throw ex;
                                }
                            }
                        }

                        Log.Info("Disabled all constraints on all tables in the database {0} on {1}...".FormatWith(a[1], SqlServerHelper.ConnectionString.Server));
                    }
                    else
                    {
                        throw new Exception("The database {0} does not exist on {1}!".FormatWith(dbName, SqlServerHelper.ConnectionString.Server));
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    if (a.Length >= 2)
                    {
                        SqlServerHelper.ConnectionString.Server = originalServer;
                    }
                }
            }
        }

        /// <summary>
        /// Imports the data into databases listed.
        /// </summary>
        private void ImportDataIntoDatabasesListed(bool isAContinueRun = false)
        {
            Log.Info("Import data into the databases listed. Data File Count: {0}", targetDataFileInfoList.Count);
            CmdHelper cmd = new CmdHelper(Directory.GetCurrentDirectory());
            string server = SqlServerHelper.ConnectionString.Server;
            foreach (DataFileInfo dfi in targetDataFileInfoList)
            {
                SqlServerHelper.ConnectionString.Server = dfi.TargetServer;

                //      0) Check if the database exists, skip if not
                if (!SqlServerHelper.DoesDatabaseExist(dfi.TargetDatabase))
                {
                    Log.Info("Database {0} does not exist on server {1}. Ignored this data file {2}.".FormatWith(dfi.TargetDatabase, dfi.TargetServer, dfi.DataFileFullPath));
                    continue;
                }

                //      1) For each table that would be impacted, check if it exists, else create it
                if (!SqlServerHelper.DoesTableExist(dfi.TargetDatabase, dfi.Table))
                {
                    // Create
                    // skip for now
                    Log.Info("Table {0} does not exist on database {1} on server {2}. Ignored this data file {3}.".FormatWith(
                        dfi.Table, dfi.TargetDatabase, dfi.TargetServer, dfi.DataFileFullPath));
                    continue;
                }

                //      2) Disable all indexes
                try
                {
                    DisableIndexesOnTable(dfi);
                }
                catch(Exception ex)
                {
                    if (!isAContinueRun)
                        throw;
                    else
                    {
                        ExceptionHelper.CentralProcess(ex);
                        Log.Info("Ignored the above exception because this is a continuous run.");
                    }
                }

                //      3) Now the table exists, check if there were old data in it, clear them if yes
                if (!isAContinueRun)
                {
                    ClearExistingDataOnTable(dfi);
                }

                //      4) Import data using BCP
                int importResult = Import(cmd, dfi);
                if (importResult == 1)
                {
                    Exception ex = new Exception("Importing data failed, please check log for detailed information.\r\n{0}".FormatWith(cmd.StdOutput));
                    this.Exceptions.Add(ex);
                    ExceptionHelper.CentralProcess(ex);
                }
                else
                {
                    dfi.MarkAsDone();
                }
            }

            SqlServerHelper.ConnectionString.Server = server;
        }

        /// <summary>
        /// Clears the existing data on table.
        /// </summary>
        /// <param name="dfi">The data file information object.</param>
        private void ClearExistingDataOnTable(DataFileInfo dfi)
        {
            if (!dataClearedList.Contains(dfi, new DataFileInfoComparer()))
            {
                string cmdText = "DELETE FROM {0}.{1}.{2}".FormatWith(dfi.TargetDatabase, dfi.Owner, dfi.Table);

                // The following may throw this exception:
                // Cannot truncate table 'Lists_0_1.dbo.list_item_data' because it is published for replication or enabled for Change Data Capture.
                ////string cmdText = "TRUNCATE TABLE {0}.{1}.{2}".Format2(dfi.TargetDatabase, dfi.Owner, dfi.Table);
                Log.Info("Clearing old data by this command: '{0}'...".FormatWith(cmdText));
                SqlServerHelper.Execute(cmdText);

                dataClearedList.Add(dfi);
            }
        }

        /// <summary>
        /// Enables the constraints.
        /// </summary>
        private void EnableConstraints()
        {
            foreach (string databaseName in targetDatabases)
            {
                string[] a = databaseName.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                string originalServer = SqlServerHelper.ConnectionString.Server;
                string dbName = databaseName;
                if (a.Length >= 2)
                {
                    SqlServerHelper.ConnectionString.Server = a[0];
                    dbName = a[1];
                }

                if (SqlServerHelper.DoesDatabaseExist(dbName))
                {
                    // Disable all constraints on all tables in the database
                    DataTable dt = SqlServerHelper.GetAllUserTableNamesOfDatabase(dbName);
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string tableName = dt.Rows[i][0].ToString(); 
                        string cmdText = "ALTER TABLE {0}.{1}.{2} CHECK CONSTRAINT ALL".FormatWith(dbName, "dbo", tableName);
                        try
                        {
                            SqlServerHelper.Execute(cmdText);
                            Log.Info("Successfully enabled constraints on {0}.{1}.{2}".FormatWith(dbName, "dbo", tableName));
                        }
                        catch (SqlServerHelperException ex)
                        {
                            if (ex.InnerException != null && ex.InnerException is System.Data.SqlClient.SqlException && ex.InnerException.Message.StartsWith("Schema change failed on object", StringComparison.InvariantCultureIgnoreCase))
                            {
                                // try again
                                System.Threading.Thread.Sleep(1000);
                                SqlServerHelper.Execute(cmdText);
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                    }
                }

                if (a.Length >= 2)
                {
                    SqlServerHelper.ConnectionString.Server = originalServer;
                }
            }
        }

        /// <summary>
        /// Enables the indexes.
        /// </summary>
        private void EnableIndexes()
        {
            string originalServer = SqlServerHelper.ConnectionString.Server;
            foreach (DataFileInfo dfi in indexesDiabledList)
            {
                SqlServerHelper.ConnectionString.Server = dfi.TargetServer;
                SqlServerHelper.RebuildAllIndexesOnTable(dfi.TargetDatabase, dfi.Table);
            } 
            SqlServerHelper.ConnectionString.Server = originalServer;
        }

        /// <summary>
        /// Disable indexes on table to avoid time consuming indexing work when inserting data
        /// </summary>
        /// <param name="dfi">The data file info object.</param>
        private void DisableIndexesOnTable(DataFileInfo dfi)
        {
            if (!indexesDiabledList.Contains(dfi, new DataFileInfoComparer()))
            {
                SqlServerHelper.DisableAllIndexesOnTable(dfi.TargetDatabase, dfi.Table);
                indexesDiabledList.Add(dfi);
            }
        }
    }

    /// <summary>
    /// Compare data file info object
    /// </summary>
    class DataFileInfoComparer : IEqualityComparer<DataFileInfo>
    {
        /// <summary>
        /// Test if the 2 specified data file information objects are equal.
        /// </summary>
        /// <param name="dfi1">The left data file information object.</param>
        /// <param name="dfi2">The right data file information object.</param>
        /// <returns>True if equals else false.</returns>
        public bool Equals(DataFileInfo dfi1, DataFileInfo dfi2)
        {
            if (dfi1.TargetTableFullName.Equals(dfi2.TargetTableFullName))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The instance.</param>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public int GetHashCode(DataFileInfo obj)
        {
            return obj.TargetTableFullName.GetHashCode();
        }
    }

    /// <summary>
    /// Data file information class
    /// </summary>
    public class DataFileInfo
    {
        /// <summary>
        /// The name
        /// </summary>
        private string name;

        /// <summary>
        /// The database
        /// </summary>
        private string database;

        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <value>
        /// The database.
        /// </value>
        public string Database
        {
            get
            {
                return database;
            }
        }

        /// <summary>
        /// The target server
        /// </summary>
        private string targetServer;

        /// <summary>
        /// Gets the target server.
        /// </summary>
        /// <value>
        /// The target server.
        /// </value>
        public string TargetServer 
        { 
            get 
            { 
                return targetServer; 
            } 
        }

        /// <summary>
        /// The target database
        /// </summary>
        private string targetDatabase;

        /// <summary>
        /// Gets the target database.
        /// </summary>
        /// <value>
        /// The target database.
        /// </value>
        public string TargetDatabase 
        { 
            get 
            { 
                return targetDatabase; 
            } 
        }

        /// <summary>
        /// The owner
        /// </summary>
        private string owner;

        /// <summary>
        /// Gets the owner.
        /// </summary>
        /// <value>
        /// The owner.
        /// </value>
        public string Owner
        {
            get
            {
                return owner;
            }
        }

        /// <summary>
        /// The table
        /// </summary>
        private string table;

        /// <summary>
        /// Gets the table.
        /// </summary>
        /// <value>
        /// The table.
        /// </value>
        public string Table
        {
            get
            {
                return table;
            }
        }

        /// <summary>
        /// The data file full path
        /// </summary>
        private string dataFileFullPath;

        /// <summary>
        /// Gets the data file full path.
        /// </summary>
        /// <value>
        /// The data file full path.
        /// </value>
        public string DataFileFullPath 
        { 
            get 
            { 
                return dataFileFullPath; 
            } 
        }

        /// <summary>
        /// Gets the full name of the target table.
        /// </summary>
        /// <value>
        /// The full name of the target table.
        /// </value>
        public string TargetTableFullName 
        { 
            get 
            { 
                return "{0}.{1}.{2}".FormatWith(this.TargetDatabase, this.Owner, this.Table); 
            } 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataFileInfo" /> class.
        /// </summary>
        /// <param name="dataFileName">Name of the data file.</param>
        public DataFileInfo(string dataFileName)
        {
            this.dataFileFullPath = dataFileName;
            this.name = Path.GetFileNameWithoutExtension(dataFileName);
            int index1 = name.IndexOf('.');
            if (index1 > 0)
            {
                this.database = name.Substring(0, index1);
                DatabaseNameMapping mapping = DatabaseNameMapping.GetMapping();
                if (!mapping.Map.TryGetValue(this.database, out this.targetDatabase))
                {
                    this.targetDatabase = this.database;
                    this.targetServer = "localhost";
                }
                else
                {
                    string[] a = this.targetDatabase.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    if (a.Length >= 2)
                    {
                        this.targetServer = a[0];
                        this.targetDatabase = a[1];
                    }
                    else
                    {
                        this.targetDatabase = a[0];
                    }
                }

                int index2 = name.IndexOf('.', index1 + 1);
                if (index2 > 0)
                {
                    this.owner = name.Substring(index1 + 1, index2 - index1 - 1);

                    int index3 = name.IndexOf('(', index2 + 1);
                    if (index3 > 0)
                    {
                        this.table = name.Substring(index2 + 1, index3 - index2 - 1);
                    }
                    else
                    {
                        this.table = name.Substring(index2 + 1);
                    }
                }
                else
                {
                    this.owner = name.Substring(index1 + 1);
                }
            }
        }

        /// <summary>
        /// Determines whether this instance is valid.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </returns>
        public bool IsValid()
        {
            return !(string.IsNullOrEmpty(this.database) || string.IsNullOrWhiteSpace(this.database) || string.IsNullOrEmpty(this.owner) || string.IsNullOrWhiteSpace(this.owner)
                || string.IsNullOrEmpty(this.table) || string.IsNullOrWhiteSpace(this.table));
        }

        /// <summary>
        /// Mark this file is processed successfully. (Move the data file into ~\Sucess folder)
        /// </summary>
        public string MarkAsDone()
        {
            string successPath = Path.Combine(Path.GetDirectoryName(this.dataFileFullPath), "Success");
            string newFileName = Path.Combine(successPath, Path.GetFileName(this.dataFileFullPath));
            try
            {
                if (!Directory.Exists(successPath))
                {
                    Directory.CreateDirectory(successPath);
                }

                File.Move(this.dataFileFullPath, newFileName);
            }
            catch (Exception ex)
            {
                ExceptionHelper.CentralProcess(ex);
            }
            return newFileName;
        }
    }

    /// <summary>
    /// Database name mapping class
    /// </summary>
    public class DatabaseNameMapping
    {
        /// <summary>
        /// The map
        /// </summary>
        private Dictionary<string, string> map;
        /// <summary>
        /// Gets the map.
        /// </summary>
        /// <value>
        /// The map.
        /// </value>
        public Dictionary<string, string> Map { get { return map; } }

        /// <summary>
        /// Prevents a default instance of the <see cref="DatabaseNameMapping" /> class from being created.
        /// </summary>
        private DatabaseNameMapping()
        {
            map = new Dictionary<string, string>();
            System.Collections.Specialized.StringCollection mapCollection = Settings.Default.DataImporter_DatabaseNameMapping;
            for (int i = 0; i < mapCollection.Count; i++)
            {
                string[] mapEntry = mapCollection[i].Split(new char[] { '\t', '='}, StringSplitOptions.RemoveEmptyEntries);
                if (mapEntry.Length > 1)
                {
                    map.Add(mapEntry[0], mapEntry[1]);
                }
            }
        }

        /// <summary>
        /// The instance
        /// </summary>
        private static DatabaseNameMapping instance;

        /// <summary>
        /// Gets the mapping.
        /// </summary>
        /// <returns>An instance of the DatabaseNameMapping class.</returns>
        public static DatabaseNameMapping GetMapping()
        {
            if (instance == null)
                instance = new DatabaseNameMapping();

            return instance;
        }
    }
}
