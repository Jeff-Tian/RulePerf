using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
using Microsoft.Scs.Test.RiskTools.RulePerf.Model;

namespace Microsoft.Scs.Test.RiskTools.RulePerf.BLL
{
    public class ServiceLocatorBLL
    {
        /// <summary>
        /// Lists the service status.
        /// </summary>
        /// <returns>Service status message.</returns>
        public static string ListServiceStatus()
        {
            ServerAssignmentModel serverAssignment = ServerAssignmentModel.GetInstance();
            string cpWebStoreConfigMachine = serverAssignment.CpWebStoreConfigPrimaryMachine;
            // This approach would get socket errors:
            /*
            WMICmdHelper wmiHelper = new WMICmdHelper(cpWebStoreConfigMachine,
                "\"C:\\Program Files (x86)\\Microsoft SPS\\ServiceLocatorServer\\ServiceLocatorControl.exe\" /action:list");
            return wmiHelper.RunCommandReturnOutput();
             */

            string cmd = @"psexec \\{0} -u Administrator -p #Bugsfor$ ""C:\Program Files (x86)\Microsoft SPS\ServiceLocatorServer\ServiceLocatorControl.exe"" /action:list".FormatWith(cpWebStoreConfigMachine);
            string outputFileName = Path.Combine(Directory.GetCurrentDirectory(), @"WMICmdOutput.{0}.txt".FormatWith(DateTime.Now.ToString("yyyy-MM-ddThh-mm-ssZ")));
            cmd += @" >" + outputFileName;
            cmd += " & exit";
            CmdHelper cmdHelper = new CmdHelper();
            cmdHelper.StartCmdDirectly(cmd);

            string outputContent = "";
            try
            {
                outputContent = File.ReadAllText(outputFileName);
                File.Delete(outputFileName);
            }
            catch (Exception ex)
            {
                outputContent += "\r\n" + ExceptionHelper.CentralProcess(ex);
            }

            Log.Info(outputContent);
            return outputContent;
        }

        /// <summary>
        /// Parses from plain list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns>A list of ServiceLocatorModel objects.</returns>
        public static List<ServiceLocatorModel> ParseFromPlainList(string list)
        {
            List<ServiceLocatorModel> services = new List<ServiceLocatorModel>();
            //string pattern = @"ServiceName:(\w+)\t\tEnabled:(True|False)\r\n\tUrl:([^\r\n]+)\r\n\tVersion:((?:\d+.)*\d+)\r\n";
            string pattern = @"ServiceName:([^\t]+)\t\tEnabled:(True|False)\r\n\tUrl:([^\r\n]+)\r\n\tVersion:((?:\d+.)*\d+)\r\n";
            Regex regex = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
            MatchCollection matches = regex.Matches(list);
            foreach (Match match in matches)
            {
                ServiceLocatorModel model = new ServiceLocatorModel();
                
                model.ServiceName = match.Groups[1].Value;
                model.Enabled = match.Groups[2].Value.ToBoolean();
                model.Url = match.Groups[3].Value;
                model.Version = match.Groups[4].Value;

                services.Add(model);
            }

            return services;
        }
    }
}
