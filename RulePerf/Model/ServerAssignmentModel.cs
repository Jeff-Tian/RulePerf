using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
using System.Security.Principal;
using System;

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Model
{
    /// <summary>
    /// The model of the Server Assignment Document
    /// </summary>
    public class ServerAssignmentModel
    {
        private string serverAssignmentXmlPath;

        private XmlDocument xmlDoc;
        /// <summary>
        /// Gets the XML document.
        /// </summary>
        /// <value>
        /// The XML document.
        /// </value>
        public XmlDocument XmlDoc { get { return xmlDoc; } }

        #region Properties
        private string[] cpRkFrontEndMachines;
        public virtual string[] CpRkFrontEndMachines
        {
            get
            {
                if (cpRkFrontEndMachines==null)
                {
                    List<string> machines = new List<string>();
                    XmlNode node = xmlDoc.SelectSingleNode("/Configurations/ServerRoles/ServerRole[@name='CP RK FrontEnd']");
                    if (node != null && node.ChildNodes != null)
                    {
                        foreach (XmlNode n in node.ChildNodes)
                        {
                            machines.Add(n.Attributes["name"].Value);
                        }
                    }

                    cpRkFrontEndMachines= machines.ToArray();
                }

                return cpRkFrontEndMachines;
            }
        }

        private string cpWebStoreConfigPrimaryMachine;
        public virtual string CpWebStoreConfigPrimaryMachine
        {
            get
            {
                if (cpWebStoreConfigPrimaryMachine == null)
                {
                    XmlNode node = xmlDoc.SelectSingleNode("/Configurations/ServerRoles/ServerRole[@name='CP Webstore Config Primary']/Server");
                    if (node != null)
                    {
                        cpWebStoreConfigPrimaryMachine = node.Attributes["name"].Value;
                    }
                    else
                        cpWebStoreConfigPrimaryMachine = "";
                }
                return cpWebStoreConfigPrimaryMachine;
            }
        }

        private string[] cpRkDatabaseServers;
        public virtual string[] CpRkDatabaseServers
        {
            get
            {
                if (cpRkDatabaseServers == null)
                {
                    List<string> servers = new List<string>();
                    XmlNodeList nodeList = xmlDoc.SelectNodes("/Configurations/ServerRoles/ServerRole[starts-with(@name, 'CP RK Database')]/Server");
                    if (nodeList != null)
                    {
                        foreach (XmlNode node in nodeList)
                        {
                            servers.Add(node.Attributes["name"].Value);
                        }
                    }

                    cpRkDatabaseServers = servers.ToArray();
                }

                return cpRkDatabaseServers;
            }
        }

        private string[] cpRkRiskConfigServers;
        public virtual string[] CpRkRiskConfigServers
        {
            get
            {
                if (this.cpRkRiskConfigServers == null)
                {
                    List<string> servers = new List<string>();
                    XmlNodeList nodeList = this.xmlDoc.SelectNodes("/Configurations/ServerRoles/ServerRole[starts-with(@name, 'CP RK Risk Config')]/Server");
                    if (nodeList != null)
                    {
                        foreach (XmlNode node in nodeList)
                        {
                            servers.Add(node.Attributes["name"].Value);
                        }
                    }

                    this.cpRkRiskConfigServers = servers.ToArray();
                }

                return this.cpRkRiskConfigServers;
            }
        }

        #endregion Properties

        private static ServerAssignmentModel model;

        private string netUserName = "Administrator";
        private string netPassword = "#Bugsfor$";
        private string netDomain = ".";

        protected ServerAssignmentModel(string netUserName = "Administrator", string netPassword = "#Bugsfor$", string netDomain = ".")
        {
            this.netUserName = netUserName;
            this.netPassword = netPassword;
            this.netDomain = netDomain;

            serverAssignmentXmlPath = Properties.Settings.Default.ServerAssignmentFilePath.Trim();
            
            if (File.Exists(serverAssignmentXmlPath))
            {
                xmlDoc = new XmlDocument();
                xmlDoc.Load(serverAssignmentXmlPath);
            }
            else
            {
                // Retry
                #region Deprecated
                //if (serverAssignmentXmlPath.StartsWith("\\\\"))
                //{
                //    IntPtr token = IntPtr.Zero;
                //    IntPtr dupToken = IntPtr.Zero;
                //    bool success = RemoteHelper.LogonUser(Properties.Settings.Default.DomainUserName, Properties.Settings.Default.Domain, Properties.Settings.Default.DomainPassword, RemoteHelper.LOGON32_LOGON_NEW_CREDENTIALS, RemoteHelper.LOGON32_PROVIDER_DEFAULT, ref token);

                //    Log.Info("success: {0}. token: {1}".Format2(success, token));
                //    if (!success)
                //    {
                //        RemoteHelper.RaiseLastError();
                //    }
                //    success = RemoteHelper.DuplicateToken(token, 2, ref dupToken);

                //    Log.Info("success: {0}. dupToken: {1}".Format2(success, dupToken));
                //    if (!success)
                //    {
                //        RemoteHelper.RaiseLastError();
                //    }

                //    WindowsIdentity ident = new WindowsIdentity(dupToken);
                //    using (WindowsImpersonationContext impersonatedUser = ident.Impersonate())
                //    {
                //        /*
                //        DirectoryInfo dirInfo = new DirectoryInfo(@"\\bedtransfer\transfer\v-jetian");
                //        FileInfo[] files = dirInfo.GetFiles();

                //        foreach (FileInfo fi in files)
                //        {
                //            Log.Info(fi.FullName);
                //        }
                //        */

                //        if (File.Exists(serverAssignmentXmlPath))
                //        {
                //            Log.Info("{0} exits.".Format2(serverAssignmentXmlPath));
                //            xmlDoc = new XmlDocument();
                //            xmlDoc.Load(serverAssignmentXmlPath);
                //        }
                //        else
                //        {
                //            throw new System.IO.FileNotFoundException("Can't find the Server Assignment Xml file '{0}' Even after logged on.".Format2(serverAssignmentXmlPath), serverAssignmentXmlPath);
                //        }

                //        impersonatedUser.Undo();
                //        RemoteHelper.CloseHandle(token);
                //    }
                //}
                #endregion Deprecated

                Impersonator impersonator = new Impersonator(this.netUserName, this.netDomain, this.netPassword);
                if (File.Exists(serverAssignmentXmlPath))
                {
                    xmlDoc = new XmlDocument();
                    xmlDoc.Load(serverAssignmentXmlPath);
                }
                else
                {
                    throw new System.IO.FileNotFoundException("Can't find the Server Assignment Xml file '{0}' or can't access it by user '{1}\\{2}'.".FormatWith(serverAssignmentXmlPath, this.netDomain, this.netUserName), serverAssignmentXmlPath);
                }
                impersonator.Undo();
            }
        }

        /// <summary>
        /// Gets the instance of this model. There is only 1 instance of this class.
        /// </summary>
        /// <returns>The instance of this class</returns>
        public static ServerAssignmentModel GetInstance(string netUserName = "Administrator", string netPassword = "#Bugsfor$", string netDomain = ".")
        {
            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.Environment) 
                || string.IsNullOrEmpty(Properties.Settings.Default.Environment) 
                || Properties.Settings.Default.Environment.Trim().StartsWith("Bed", System.StringComparison.InvariantCultureIgnoreCase))
            {
                if (model == null) model = new ServerAssignmentModel(netUserName, netPassword, netDomain);
                return model;
            }
            else if (Properties.Settings.Default.Environment.Trim().StartsWith("OneBox", System.StringComparison.InvariantCultureIgnoreCase))
            {
                return OneBoxServerAssignmentModel.GetInstance();
            }
            else
            {
                return null;
            }
        }
    }
}
