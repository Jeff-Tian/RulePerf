using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
using Microsoft.Scs.Test.RiskTools.RulePerf.Model;

namespace Microsoft.Scs.Test.RiskTools.RulePerf.BLL
{
    public class AllowedInstancesConfigBLL
    {
        public static string GetCurrentAllowedInstancesConfigsWithoutErrorHandling()
        {
            StringBuilder result = new StringBuilder();

            ServerAssignmentModel model = ServerAssignmentModel.GetInstance();
            for (int i = 0; i < model.CpRkFrontEndMachines.Length; i++)
            {
                #region Risk Web config
                string riskWebConfigPath = @"\\{0}\{1}$\Program Files (x86)\Microsoft CTP\Risk\web.config".FormatWith(model.CpRkFrontEndMachines[i], "C");
                result.AppendLine("Current configs in machine '{0}: '".FormatWith(model.CpRkFrontEndMachines[i]));

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(riskWebConfigPath);
                XmlNode node = xmlDoc.SelectSingleNode("/configuration/system.serviceModel/behaviors/serviceBehaviors/behavior[@name='RiskWSBehavior']/serviceThrottling");
                result.AppendLine("Service Throttling/maxConcurrentCalls: {0}".FormatWith(node != null && node.Attributes["maxConcurrentCalls"] != null ? node.Attributes["maxConcurrentCalls"].Value : "Not found."));
                result.AppendLine("Service Throttling/maxConcurrentInstances: {0}".FormatWith(node != null && node.Attributes["maxConcurrentInstances"] != null ? node.Attributes["maxConcurrentInstances"].Value : "Not found."));
                result.AppendLine();
                #endregion Risk Web config
                #region machine config
                string[] machineConfigs = new string[] { 
                        @"\\{0}\{1}$\Windows\Microsoft.NET\Framework\v4.0.30319\Config\machine.config".FormatWith(model.CpRkFrontEndMachines[i], "C"),
                        @"\\{0}\{1}$\Windows\Microsoft.NET\Framework64\v4.0.30319\Config\machine.config".FormatWith(model.CpRkFrontEndMachines[i], "C")
                    };
                for (int j = 0; j < machineConfigs.Length; j++)
                {
                    string machineConfigPath = machineConfigs[j];
                    xmlDoc = new XmlDocument();
                    xmlDoc.Load(machineConfigPath);
                    result.AppendLine("Current machine configs in machine '{0}': ".FormatWith(model.CpRkFrontEndMachines[i]));
                    string xpath = "/configuration/system.web/processModel";
                    XmlElement element = xmlDoc.SelectSingleNode(xpath) as XmlElement;
                    if (element != null)
                    {
                        result.AppendLine("processModel/autoConfig: {0}".FormatWith(element.Attributes["autoConfig"] != null ? element.Attributes["autoConfig"].Value : "Not found."));
                        result.AppendLine("processModel/maxWorkerThreads: {0}".FormatWith(element.Attributes["maxWorkerThreads"] != null ? element.Attributes["maxWorkerThreads"].Value : "Not found."));
                        result.AppendLine("processModel/maxIoThreads: {0}".FormatWith(element.Attributes["maxIoThreads"] != null ? element.Attributes["maxIoThreads"].Value : "Not found."));
                        result.AppendLine("processModel/minWorkerThreads: {0}".FormatWith(element.Attributes["minWorkerThreads"] != null ? element.Attributes["minWorkerThreads"].Value : "Not found."));
                        result.AppendLine();
                    }
                    else
                    {
                        result.AppendLine("Xml element '{0}' in machine config '{1}' not found.".FormatWith(xpath, machineConfigPath));
                    }
                }
                #endregion machine config
            }

            return result.ToString();
        }

        public static string GetCurrentAllowedInstancesConfigs()
        {
            StringBuilder result = new StringBuilder();
            try
            {
                result.AppendLine(GetCurrentAllowedInstancesConfigsWithoutErrorHandling());
            }
            catch (Exception ex)
            {
                result.AppendLine(ExceptionHelper.CentralProcess(ex));
            }
            return result.ToString();
        }

        public static string IncreaseAllowedInstancesConfigsWithoutErrorHandling()
        {
            StringBuilder result = new StringBuilder();
            ServerAssignmentModel model = ServerAssignmentModel.GetInstance();
            for (int i = 0; i < model.CpRkFrontEndMachines.Length; i++)
            {
                #region Risk Web Config
                string riskWebConfigPath = @"\\{0}\{1}$\Program Files (x86)\Microsoft CTP\Risk\web.config".FormatWith(model.CpRkFrontEndMachines[i], "C");

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(riskWebConfigPath);
                string bakFilePath = riskWebConfigPath + ".{0}.bak".FormatWith(DateTime.Now.ToString("yyyy-MM-ddThh-mm-ssZ"));
                xmlDoc.Save(bakFilePath);
                result.AppendLine("Backed up current config to '{0}'.".FormatWith(bakFilePath));
                result.AppendLine("Current configs in machine '{0}': ".FormatWith(model.CpRkFrontEndMachines[i]));
                string xpath = "/configuration/system.serviceModel/behaviors/serviceBehaviors/behavior[@name='RiskWSBehavior']/serviceThrottling";
                XmlNode node = xmlDoc.SelectSingleNode(xpath);
                if (node != null)
                {
                    result.AppendLine("Service Throttling/maxConcurrentCalls: {0}".FormatWith(node.Attributes["maxConcurrentCalls"] != null ? node.Attributes["maxConcurrentCalls"].Value : "Not found."));
                    result.AppendLine("Now update it to 1000...");
                    if (node.Attributes["maxConcurrentCalls"] != null) node.Attributes["maxConcurrentCalls"].Value = "1000";
                    else
                    {
                        XmlElement e = node as XmlElement;
                        e.SetAttribute("maxConcurrentCalls", "1000");
                    }
                    result.AppendLine("Service Throttling/maxConcurrentInstances: {0}".FormatWith(node.Attributes["maxConcurrentInstances"] != null ? node.Attributes["maxConcurrentInstances"].Value : "Not found."));
                    result.AppendLine("Now update it to 10000...");
                    if (node.Attributes["maxConcurrentInstances"] != null)
                        node.Attributes["maxConcurrentInstances"].Value = "10000";
                    else
                    {
                        XmlElement e = node as XmlElement;
                        e.SetAttribute("maxConcurrentInstances", "10000");
                    }
                    result.AppendLine();
                    xmlDoc.Save(riskWebConfigPath);
                }
                else
                {
                    result.AppendLine("Xml node '{0}' in config '{1}' not found.".FormatWith(xpath, riskWebConfigPath));
                }
                #endregion Risk Web Config
                #region Machine Config
                string[] machineConfigs = new string[] { 
                        @"\\{0}\{1}$\Windows\Microsoft.NET\Framework\v4.0.30319\Config\machine.config".FormatWith(model.CpRkFrontEndMachines[i], "C"),
                        @"\\{0}\{1}$\Windows\Microsoft.NET\Framework64\v4.0.30319\Config\machine.config".FormatWith(model.CpRkFrontEndMachines[i], "C")
                    };
                for (int j = 0; j < machineConfigs.Length; j++)
                {
                    string machineConfigPath = machineConfigs[j];
                    xmlDoc = new XmlDocument();
                    xmlDoc.Load(machineConfigPath);
                    bakFilePath = machineConfigPath + ".{0}.bak".FormatWith(DateTime.Now.ToString("yyyy-MM-ddThh-mm-ssZ"));
                    xmlDoc.Save(bakFilePath);
                    result.AppendLine("Backed up current machine config to '{0}'.".FormatWith(bakFilePath));
                    result.AppendLine("Current machine configs in machine '{0}': ".FormatWith(model.CpRkFrontEndMachines[i]));
                    xpath = "/configuration/system.web/processModel";
                    XmlElement element = xmlDoc.SelectSingleNode(xpath) as XmlElement;
                    if (element != null)
                    {
                        result.AppendLine("processModel/autoConfig: {0}".FormatWith(element.Attributes["autoConfig"] != null ? element.Attributes["autoConfig"].Value : "Not found."));
                        result.AppendLine("Now update it to false...");
                        element.SetAttribute("autoConfig", "false");
                        result.AppendLine("processModel/maxWorkerThreads: {0}".FormatWith(element.Attributes["maxWorkerThreads"] != null ? element.Attributes["maxWorkerThreads"].Value : "Not found."));
                        result.AppendLine("Now udpate it to 1000...");
                        element.SetAttribute("maxWorkerThreads", "1000");
                        result.AppendLine("processModel/maxIoThreads: {0}".FormatWith(element.Attributes["maxIoThreads"] != null ? element.Attributes["maxIoThreads"].Value : "Not found."));
                        result.AppendLine("Now update it to 100...");
                        element.SetAttribute("maxIoThreads", "100");
                        result.AppendLine("processModel/minWorkerThreads: {0}".FormatWith(element.Attributes["minWorkerThreads"] != null ? element.Attributes["minWorkerThreads"].Value : "Not found."));
                        result.AppendLine("Now update it to 50...");
                        element.SetAttribute("minWorkerThreads", "50");
                        result.AppendLine();
                        xmlDoc.Save(machineConfigPath);
                    }
                    else
                    {
                        result.AppendLine("Xml element '{0}' in machine config '{1}' not found.".FormatWith(xpath, machineConfigPath));
                    }
                }
                #endregion
            }

            return result.ToString();
        }

        public static string IncreaseAllowedInstancesConfigs()
        {
            StringBuilder result = new StringBuilder();
            try
            {
                result.AppendLine(IncreaseAllowedInstancesConfigsWithoutErrorHandling());
            }
            catch (Exception ex)
            {
                result.AppendLine(ExceptionHelper.CentralProcess(ex));
            }
            return result.ToString();
        }

        public static string RestoreAllowedInstancesConfigsToMostRecentBak()
        {
            StringBuilder result = new StringBuilder();
            try
            {
                ServerAssignmentModel model = ServerAssignmentModel.GetInstance();
                for (int i = 0; i < model.CpRkFrontEndMachines.Length; i++)
                {
                    string riskWebConfigPath = @"\\{0}\{1}$\Program Files (x86)\Microsoft CTP\Risk\web.config".FormatWith(model.CpRkFrontEndMachines[i], "C");
                    result.AppendLine(RestoreAllowedInstancesConfigsToMostRecentBak(riskWebConfigPath));
                    string machineConfigPath = @"\\{0}\{1}$\Windows\Microsoft.NET\Framework\v4.0.30319\Config\machine.config".FormatWith(model.CpRkFrontEndMachines[i], "C");
                    result.AppendLine(RestoreAllowedInstancesConfigsToMostRecentBak(machineConfigPath));
                    string x64MachineConfigPath = @"\\{0}\{1}$\Windows\Microsoft.NET\Framework64\v4.0.30319\Config\machine.config".FormatWith(model.CpRkFrontEndMachines[i], "C");
                    result.AppendLine(RestoreAllowedInstancesConfigsToMostRecentBak(x64MachineConfigPath));
                    result.AppendLine();
                }
            }
            catch (Exception ex)
            {
                result.AppendLine(ExceptionHelper.CentralProcess(ex));
            }

            return result.ToString();
        }

        public static string RestoreAllowedInstancesConfigsToMostRecentBak(string configFilePath)
        {
            StringBuilder result = new StringBuilder();
            string configFileDir = Path.GetDirectoryName(configFilePath);
            string configFileName = Path.GetFileName(configFilePath);
            result.AppendLine("Trying to search in directory '{0}'...".FormatWith(configFileDir));
            DirectoryInfo di = new DirectoryInfo(configFileDir);
            if (di.Exists)
            {
                FileInfo[] bakFiles = di.GetFiles("{0}{1}.bak".FormatWith(configFileName, "????-??-??T??-??-??Z"), SearchOption.TopDirectoryOnly);
                FileInfo mostRecentBak = GetMostRecentBakFile(bakFiles);
                if (mostRecentBak != null)
                {
                    File.Copy(mostRecentBak.FullName, configFilePath, true);
                    result.AppendLine("Restored {0} from {1}".FormatWith(configFilePath, mostRecentBak.FullName));
                }
                else
                {
                    throw new System.IO.FileNotFoundException("No bak files for {0} were found.".FormatWith(configFilePath), "{0}{1}.bak".FormatWith(configFilePath, "yyyy-MM-ddThh:mm:ssZ"));
                }
            }
            else
            {
                throw new System.IO.FileNotFoundException("The path '{0}' does not exist".FormatWith(configFileDir), configFileDir);
            }

            return result.ToString();
        }

        #region Helpers
        private static FileInfo GetMostRecentBakFile(FileInfo[] files)
        {
            if (files.Length > 0)
            {
                DateTime? maxTime = GetBakTime(files[0].Name);
                int index = 0;
                for (int i = 1; i < files.Length; i++)
                {
                    DateTime? time = GetBakTime(files[i].Name);
                    if (time.HasValue && (!maxTime.HasValue || time.Value.CompareTo(maxTime.Value) > 0))
                    {
                        maxTime = time;
                        index = i;
                    }
                }

                return files[index];
            }
            else
            {
                return null;
            }
        }

        private static DateTime? GetBakTime(string bakFileName)
        {
            string pattern = @".*\.(\d{4})\-(\d{2})\-(\d{2})[Tt](\d{2})\-(\d{2})\-(\d{2})[Zz]\.[Bb][Aa][Kk]";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            Match match = regex.Match(bakFileName);
            if (match != null && match.Captures != null)
            {
                try
                {
                    DateTime time = new DateTime(match.Captures[0].Value.ToInt(), match.Captures[1].Value.ToInt(), match.Captures[2].Value.ToInt(),
                        match.Captures[3].Value.ToInt(), match.Captures[4].Value.ToInt(), match.Captures[5].Value.ToInt());
                    return time;
                }
                catch
                {
                    //ExceptionHelper.CentralProcess(ex);
                    return null;
                }
            }else {
                return null;
            }
        }
        #endregion
    }
}
