// -----------------------------------------------------------------------
// <copyright file="DataExporter.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf.BLL
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using Microsoft.Scs.Test.RiskTools.RulePerf.DAL;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Properties;

    /// <summary>
    /// Export data from SQL server
    /// </summary>
    public class DataExporter
    {
        /// <summary>
        /// The BCP BATCH COUNT
        /// </summary>
        private const int BCP_BATCH_COUNT = 2000000;

        /// <summary>
        /// The BCP import command format
        /// </summary>
        private readonly string bcpImportCommandFormat = "\"SELECT * FROM {0}\" queryout {1} -S{2} -T -c";

        /// <summary>
        /// The BCP import command ex format
        /// </summary>
        private readonly string bcpImportCommandExFormat = "\"SELECT * FROM {0}\" queryout {1} -S{2} -T -c -F{3} -L{4}";

        /// <summary>
        /// The source table list
        /// </summary>
        private System.Collections.Specialized.StringCollection sourceTableList;

        /// <summary>
        /// The compress and copy exceptions
        /// </summary>
        private List<Exception> compressAndCopyExceptions = new List<Exception>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DataExporter" /> class.
        /// </summary>
        public DataExporter()
        {
            this.sourceTableList = Settings.Default.DataExporter_TableList;
            this.SuccessTableCount = 0;
        }

        /// <summary>
        /// Gets or sets the compress and copy exceptions.
        /// </summary>
        /// <value>
        /// The compress and copy exceptions.
        /// </value>
        public List<Exception> CompressAndCopyExceptions
        {
            get
            {
                return this.compressAndCopyExceptions;
            }

            set
            {
                this.compressAndCopyExceptions = value;
            }
        }

        /// <summary>
        /// Gets or sets the success table count.
        /// </summary>
        /// <value>
        /// The success table count.
        /// </value>
        public int SuccessTableCount { get; set; }

        /// <summary>
        /// Gets the source table list.
        /// </summary>
        /// <value>
        /// The source table list.
        /// </value>
        public System.Collections.Specialized.StringCollection SourceTableList
        {
            get
            {
                return this.sourceTableList;
            }
        }

        /// <summary>
        /// Does the sequence.
        /// </summary>
        /// <returns>True if success else false.</returns>
        public bool DoSequence()
        {
            try
            {
                CmdHelper cmd = new CmdHelper();
                foreach (string sourceTable in this.SourceTableList)
                {
                    cmd.ExecuteCommand("bcp.exe " + this.GetCommand(sourceTable));
                    this.SuccessTableCount++;
                }

                return true;
            }
            catch (Exception ex)
            {
                ExceptionHelper.CentralProcess(ex);
                return false;
            }
        }

        /// <summary>
        /// Does the sequence.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns>True is success else false.</returns>
        public bool DoSequence(out string log)
        {
            log = string.Empty;
            this.SuccessTableCount = 0;
            CmdHelper cmd = new CmdHelper();
            foreach (string sourceTable in this.SourceTableList)
            {
                SqlServerHelper.ConnectionString.Server = Settings.Default.DataExporter_ServerName;
                int count = SqlServerHelper.QueryScaler("SELECT COUNT(*) FROM {0} (nolock)".FormatWith(sourceTable)).ToInt();
                int startRow = 1;
                int endRow = BCP_BATCH_COUNT;
                int serialNo = 1;
                if (endRow < count)
                {
                    while (startRow <= count && endRow <= count)
                    {
                        log += this.DoBcpCommandEx(cmd, serialNo, startRow, endRow, sourceTable);
                        serialNo++;
                        startRow = endRow + 1;
                        endRow = Math.Min(count, endRow + BCP_BATCH_COUNT);
                    }

                    // TODO: check if all the cmd exit with 0 in the above loop, if then execute the below statement.
                    this.SuccessTableCount++;
                }
                else
                {
                    log += DoBcpCommand(cmd, sourceTable);
                }
            }

            return this.SuccessTableCount == this.SourceTableList.Count;
        }

        /// <summary>
        /// Does the BCP command ex.
        /// </summary>
        /// <param name="cmd">The CMD.</param>
        /// <param name="serialNo">The serial no.</param>
        /// <param name="startRow">The start row.</param>
        /// <param name="endRow">The end row.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <returns>The command text.</returns>
        private string DoBcpCommandEx(CmdHelper cmd, int serialNo, int startRow, int endRow, string tableName)
        {
            string outDataFileFullName = "";
            string log = "";
            cmd.ClearOutput();
            cmd.ExecuteCommand("bcp.exe " + GetCommandEx(serialNo, startRow, endRow, tableName, out outDataFileFullName));
            if (cmd.StdOutput.Length > 0)
            {
                log = "Command Output:\r\n{0}\r\n".FormatWith(cmd.StdOutput.ToString());
            }

            if (cmd.StdErr.Length > 0)
            {
                log += "Command Output:\r\n{0}\r\n".FormatWith(cmd.StdErr.ToString());
            }

            if (cmd.LastExitCode == 0)
            {
                AsyncCompress(outDataFileFullName);
            }

            return log;
        }

        /// <summary>
        /// Does the BCP command.
        /// </summary>
        /// <param name="cmd">The CMD.</param>
        /// <param name="sourceTable">The source table.</param>
        /// <returns>The log text.</returns>
        private string DoBcpCommand(CmdHelper cmd, string sourceTable)
        {
            string outDataFileFullName = "";
            string log = "";
            cmd.ClearOutput();
            cmd.ExecuteCommand("bcp.exe " + GetCommand(sourceTable, out outDataFileFullName));
            if (cmd.StdOutput.Length > 0)
            {
                log = "Command Output:\r\n{0}\r\n".FormatWith(cmd.StdOutput.ToString());
            }

            if (cmd.StdErr.Length > 0)
            {
                log += "Command Output:\r\n{0}\r\n".FormatWith(cmd.StdErr.ToString());
            }

            if (cmd.LastExitCode == 0)
            {
                this.SuccessTableCount++;
                AsyncCompress(outDataFileFullName);
            }

            return log;
        }

        /// <summary>
        /// Compress the file asynchronously.
        /// </summary>
        /// <param name="dataFileFullName">Full name of the data file.</param>
        private void AsyncCompress(string dataFileFullName)
        {
            Thread t = new Thread(() =>
            {
                try
                {
                    Log.Info("Compressing file '{0}'...".FormatWith(dataFileFullName));
                    string compressedFileFullName = ZipHelper.CompressFile(dataFileFullName);
                    Log.Info("Compressed file '{0}' to '{1}'.".FormatWith(dataFileFullName, compressedFileFullName));

                    string destFullName = Path.Combine(Settings.Default.TransferFolder, Path.GetFileName(compressedFileFullName));
                    Log.Info("Copying file '{0}' to '{1}'...".FormatWith(compressedFileFullName, destFullName));
                    File.Copy(compressedFileFullName, destFullName, true);
                    Log.Info("Copied file '{0}' to '{1}'...".FormatWith(compressedFileFullName, destFullName));
                }
                catch (Exception ex)
                {
                    this.CompressAndCopyExceptions.Add(ExceptionHelper.CentralProcessSingle2(ex));
                }
            }) { Name = "Compress Data File '{0}' and copy it to '{1}'".FormatWith(dataFileFullName, Settings.Default.TransferFolder), IsBackground = false };
            t.Start();
        }

        /// <summary>
        /// Gets the command.
        /// </summary>
        /// <param name="sourceTableFullName">Full name of the source table.</param>
        /// <returns>the command text</returns>
        private string GetCommand(string sourceTableFullName)
        {
            string outDataFileFullName = "";
            return GetCommand(sourceTableFullName, out outDataFileFullName);
        }

        /// <summary>
        /// Gets the command.
        /// </summary>
        /// <param name="sourceTableFullName">Full name of the source table.</param>
        /// <param name="outDataFileFullName">Full name of the out data file.</param>
        /// <returns>the command text</returns>
        private string GetCommand(string sourceTableFullName, out string outDataFileFullName)
        {
            string destFilePath = Path.Combine(Directory.GetCurrentDirectory(), "{0}.txt".FormatWith(sourceTableFullName));
            outDataFileFullName = destFilePath;
            return bcpImportCommandFormat.FormatWith(sourceTableFullName, destFilePath, Settings.Default.DataExporter_ServerName);
        }

        /// <summary>
        /// Gets the command ex.
        /// </summary>
        /// <param name="serialNo">The serial no.</param>
        /// <param name="startRow">The start row.</param>
        /// <param name="endRow">The end row.</param>
        /// <param name="sourceTableFullName">Full name of the source table.</param>
        /// <param name="outDataFileFullName">Full name of the out data file.</param>
        /// <returns>The command text</returns>
        private string GetCommandEx(int serialNo, int startRow, int endRow, string sourceTableFullName, out string outDataFileFullName)
        {
            string destFilePath = Path.Combine(Directory.GetCurrentDirectory(), "{0}({1}).txt".FormatWith(sourceTableFullName, serialNo));
            outDataFileFullName = destFilePath;
            return bcpImportCommandExFormat.FormatWith(sourceTableFullName, destFilePath, Settings.Default.DataExporter_ServerName, startRow, endRow);
        }
    }
}
