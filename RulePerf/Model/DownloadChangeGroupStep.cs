// -----------------------------------------------------------------------
// <copyright file="PrepareTransactionDataFileStep.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Net;
    using System.IO;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
    using System.IO.Compression;
    using System.Xml.Linq;
    using Microsoft.Scs.Test.RiskTools.RulePerf.BLL;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Prepare transaction data file as per the specified rule ids for replay tool
    /// </summary>
    /// 
    [Serializable()]
    public class DownloadChangeGroupStep : Step
    {
        public DownloadChangeGroupStep()
        {
            this.Name = "Download change group.";
            this.Description = "Download change group";
        }

        public DownloadChangeGroupStep(string name, string description)
        {
            this.Name = name;
            this.Description = description;
        }

        #region Properties
        /// <summary>
        /// Gets or sets a value indicating whether this step is checked. A checked step would be run by <see cref="StepsProcessor" />.
        /// </summary>
        public override bool Checked
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the step name.
        /// </summary>
        public override string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the Step status, the status is one of the <see cref="StepStatusEnum" />.
        /// </summary>
        public override StepStatusEnum Status
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the description for this step.
        /// </summary>
        public override string Description
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the step result. The result is an instance of <see cref="StepResultDetail" />.
        /// </summary>
        public override StepResultDetail ResultDetail
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the serial number of the step. The serial number indicates the step's order among all the steps.
        /// </summary>
        public override int Sequence
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        private System.Collections.Generic.List<DeployTargetModel> deploySequence = new System.Collections.Generic.List<DeployTargetModel>();

        /// <summary>
        /// Gets or sets the deploy sequence.
        /// </summary>
        /// <value>
        /// The deploy sequence.
        /// </value>
        public override System.Collections.Generic.List<DeployTargetModel> DeploySequence
        {
            get
            {
                // It is very important to make sure the DeploySequence holds a List<DeployTargetModel> instance.
                // Or the adding new DeployTargetModel into DeploySequence would fail in the PropertyGrid control at runtime!
                if (this.deploySequence == null) this.deploySequence = new System.Collections.Generic.List<DeployTargetModel>();
                return this.deploySequence;
            }
            set { this.deploySequence = value; }
        }

        /// <summary>
        /// Gets the setting names for this step. The setting names are delimited by pipe character '|'.
        /// </summary>
        public override string SettingNames
        {
            get
            {
                return "ChangeGroupLinks|ChangeGroupDownloadTo|ChangeGroupDownloadingReferencedList";
            }
        }
        #endregion Properties

        protected override void ExecuteMain()
        {
            this.Status = StepStatusEnum.Executing;
            string log = string.Empty;
            try
            {
                Impersonator impersonator = new Impersonator(
                    Properties.Settings.Default.DomainUserName,
                    Properties.Settings.Default.Domain,
                    Properties.Settings.Default.DomainPassword);

                Properties.Settings.Default.ChangeGroupXmlFiles.Clear();
                for (int i = 0; i < Properties.Settings.Default.ChangeGroupLinks.Count; i++)
                {
                    string cmd = @"\\bedtransfer\transfer\RulePerf\MergeChangeGroupTool.exe Merge /retrieveFromRCM:false /isEnforceRefresh:true /Links:{0}".FormatWith(
                        Properties.Settings.Default.ChangeGroupLinks[i]
                        );

                    int result = ThirdPartyProgramBLL.EnhancedRunCommand(
                        out log,
                        cmd,
                        Properties.Settings.Default.ChangeGroupDownloadingReferencedList.ToArray()
                        );

                    switch (result)
                    {
                        case 0:
                            string fileName = log.Replace('\r', ' ').Replace('\n', ' ');
                            fileName = Regex.Replace(fileName, @".*Merge File: (\\\\bedtransfer\\transfer\\RulePerf\\MergeResult\\.+\.xml)\s*.*$", "$1");

                            Log.Info("Downloaded change group from {0} to {1}".FormatWith(
                                Properties.Settings.Default.ChangeGroupLinks[i],
                                fileName
                                ));

                            string downloadedXmlFile = Path.Combine(Properties.Settings.Default.ChangeGroupDownloadTo, Path.GetFileName(fileName));
                            File.Copy(fileName, downloadedXmlFile, true);

                            Log.Info("Copied change group from {0} to {1}".FormatWith(
                                fileName, downloadedXmlFile
                                ));

                            Properties.Settings.Default.ChangeGroupXmlFiles.Add(downloadedXmlFile);
                            break;
                        default:
                            throw new Exception("Command didn't run successfully.{0}".FormatWith(log));
                    }
                }

                Properties.Settings.Default.RuleIds.Clear();

                List<string> ruleIds = new List<string>();

                for (int i = 0; i < Properties.Settings.Default.ChangeGroupXmlFiles.Count; i++)
                {
                    ruleIds.AddRange(GetRulesFromXmlFile(Properties.Settings.Default.ChangeGroupXmlFiles[i]));
                }

                foreach (string ruleId in ruleIds)
                {
                    Properties.Settings.Default.RuleIds.Add(ruleId);
                }

                Properties.Settings.Default.RiskAPICaller_Description = Path.GetFileNameWithoutExtension(Properties.Settings.Default.ChangeGroupXmlFiles[0]);

                SettingEntityModel.SaveAll();

                this.Status = StepStatusEnum.Pass;
                this.ResultDetail = new StepResultDetail("");
                for (int i = 0; i < Properties.Settings.Default.ChangeGroupLinks.Count; i++)
                {
                    this.ResultDetail.Message += "\r\n" + "Successfully downloaded {0} to {1}".FormatWith(
                        Properties.Settings.Default.ChangeGroupLinks[i],
                        Properties.Settings.Default.ChangeGroupXmlFiles[i]
                        );
                }

                impersonator.Undo();
            }
            catch(Exception ex)
            {
                this.Status = StepStatusEnum.Failed;
                this.ResultDetail = new StepResultDetail("Error has occurred, please check log.", ExceptionHelper.CentralProcessSingle2(ex));
            }
            finally
            {
                if (this.ResultDetail != null)
                {
                    Log.Info(this.ResultDetail.Message);
                }
            }
        }
        
        /// <summary>
        /// Gets the rules from XML file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>Returns the rules.</returns>
        public List<string> GetRulesFromXmlFile(string filePath)
        {
            List<string> rules = new List<string>();

            XNamespace urnl = "urn:schemas.microsoft.com/Commerce/Types/RiAS/2011/04";
            XNamespace i = "http://www.w3.org/2001/XMLSchema instance";

            XElement elem = XElement.Load(filePath);

            var configChanges = elem.Elements(urnl + "Changes").Elements(urnl + "ConfigChange");
            foreach (XElement change in configChanges)
            {
                string configObjectType = change.Element(urnl + "ConfigObjectType").Value;

                if (configObjectType == "EvaluationRule")
                {
                    string configVerb = change.Element(urnl + "ConfigVerb").Value;
                    if (!string.Equals(configVerb, "Add", StringComparison.CurrentCultureIgnoreCase))
                    {
                        string key = change.Element(urnl + "Key").Value;

                        if (!rules.Contains(key))
                        {
                            rules.Add(key);
                        }
                    }
                    else
                    {
                        string key = this.GetkeyFromChanges(change, configObjectType);
                        if (!rules.Contains(key))
                        {
                            rules.Add(key);
                        }
                    }
                }
            }

            return rules;
        }

        /// <summary>
        /// Getkeys from changes.
        /// </summary>
        /// <param name="change">The change.</param>
        /// <param name="configObjectType">Type of the config object.</param>
        /// <returns>Returns the key.</returns>
        private string GetkeyFromChanges(XElement change, string configObjectType)
        {
            XNamespace urnl = "urn:schemas.microsoft.com/Commerce/Types/RiAS/2011/04";
            XNamespace i = "http://www.w3.org/2001/XMLSchema instance";

            string keyItem = string.Empty;

            switch (configObjectType)
            {
                case "CriteriaCode":
                    keyItem = "Id";
                    break;

                case "EvaluationRule":
                case "AggregationDefinition":
                case "DerivedAttribute":
                case "ScoreModel":
                case "ListDefinition":
                    keyItem = "Name";
                    break;

                case "PartnerSetting":
                    keyItem = "PartnerName";
                    break;

                case "PartitionSetting":
                    keyItem = "DatabaseName";
                    break;

                case "ExternalDataSetting":
                    keyItem = "DataNamespace";
                    break;

                default:
                    keyItem = "Name";
                    break;
            }

            return change.Element(urnl + "NewConfig").Element(urnl + keyItem).Value;
        }
    }
}
