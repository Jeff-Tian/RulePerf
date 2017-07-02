using System.IO;
using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
using Microsoft.Scs.Test.RiskTools.RulePerf.Properties;
using System;

namespace Microsoft.Scs.Test.RiskTools.RulePerf.BLL
{
    public class ThirdPartyProgramBLL
    {
        /// <summary>
        /// Run command directly
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="cmd">The CMD.</param>
        /// <returns>Last exit code</returns>
        public static int RunCommand(out string log, string cmd, string userName = "", string password = "", string domain = "")
        {
            CmdHelper cmdHelper = new CmdHelper();
            Log.Info("Trying to start \r\n{0}".FormatWith(cmd));
            cmdHelper.ExecuteCommand(cmd, userName, password, domain);
            log = "";
            if (cmdHelper.StdOutput.Length > 0)
                log = "Command Output:\r\n{0}\r\n".FormatWith(cmdHelper.StdOutput.ToString());
            if (cmdHelper.StdErr.Length > 0)
                log += "Command Output:\r\n{0}\r\n".FormatWith(cmdHelper.StdErr.ToString());

            return cmdHelper.LastExitCode;
        }

        /// <summary>
        /// Run command. If the executable file is on a network path, then copy it to local machine first, then run
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="cmd">The CMD.</param>
        /// <returns>Last exit code</returns>
        public static int EnhancedRunCommand(out string log, string cmd, string[] referencedFiles = null, 
            string userName="", string password="", string domain="")
        {
            string[] cmdPart = cmd.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string executableFileFullName = cmdPart[0].Trim('\"');

            if (referencedFiles != null && referencedFiles.Length > 0)
            {
                try
                {
                    foreach (string file in referencedFiles)
                    {
                        string localFile = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(file));
                        if (!File.Exists(localFile))
                        {
                            File.Copy(file, localFile, true);
                        }
                        else
                        {
                            // TODO: only copy if the last updated time of remote file is newer
                            File.Copy(file, localFile, true);
                        }
                    }
                }
                catch (System.UnauthorizedAccessException)
                {
                    // Retry 
                    Impersonator impersonator = new Impersonator(
                        Properties.Settings.Default.DomainUserName,
                        Properties.Settings.Default.Domain,
                        Properties.Settings.Default.DomainPassword);

                    foreach (string file in referencedFiles)
                    {
                        string localFile = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(file));
                        if (!File.Exists(localFile))
                        {
                            File.Copy(file, localFile, true);
                        }
                        else
                        {
                            // TODO: only copy if the last updated time of remote file is newer
                            File.Copy(file, localFile, true);
                        }
                    }

                    impersonator.Undo();
                }
            }

            if (executableFileFullName.StartsWith("\\\\", StringComparison.InvariantCultureIgnoreCase))
            {
                // Copy it from network path to local machine
                string localFullName = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(executableFileFullName));
                if (!File.Exists(localFullName))
                {
                    try
                    {
                        File.Copy(executableFileFullName, localFullName);
                    }
                    catch (System.UnauthorizedAccessException)
                    {
                        // Retry 
                        Impersonator impersonator = new Impersonator(
                            Properties.Settings.Default.DomainUserName,
                            Properties.Settings.Default.Domain,
                            Properties.Settings.Default.DomainPassword);

                        File.Copy(executableFileFullName, localFullName);

                        impersonator.Undo();
                    }
                }

                cmd = "\"" + localFullName + "\"" + cmd.Remove(0, executableFileFullName.Length);
            }

            return RunCommand(out log, cmd, userName, password, domain);
        }
    }
}
