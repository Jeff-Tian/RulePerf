﻿// -----------------------------------------------------------------------
// <copyright file="BackupDatabasesStep.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Scs.Test.RiskTools.RulePerf.BLL;
    using Microsoft.Scs.Test.RiskTools.RulePerf.DataStructure;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Properties;
    using System.IO;

    /// <summary>
    /// A step class that is used to backup databases on bed.
    /// </summary>
    [Serializable]
    public class BackupDatabasesStep : Step
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BackupDatabasesStep" /> class. With default properties.
        /// </summary>
        public BackupDatabasesStep()
        {
            this.Name = "Backup bed databases";
            this.Description = "Backup bed databases. You can configure which databases need to be backed up in the {0} file.".FormatWith(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupDatabasesStep" /> class. With customized properties.
        /// </summary>
        /// <param name="name">The step name</param>
        /// <param name="description">The description for this step</param>
        public BackupDatabasesStep(string name, string description)
        {
            this.Name = name;
            this.Description = description;
            this.Checked = true;
        }

        #region Properties
        /// <summary>
        /// Gets or sets a value indicating whether this step is checked. A checked step would be run by <see cref="StepsProcessor"/>.
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
        /// Gets the description for this step.
        /// </summary>
        public override string Description
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
        /// Gets the setting names for this step. The setting names are delimited by pipe character '|'.
        /// </summary>
        public override string SettingNames
        {
            get
            {
                return "DatabasesToBeBackedup";
            }
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

        #endregion Properties

        #region Methods
        /// <summary>
        /// Execute this step.
        /// </summary>
        protected override void ExecuteMain()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                this.Status = StepStatusEnum.Executing;
                bool success = true;
                Dictionary<string, Package<string, string>> packages = Package<string, string>.FromStringArray(Settings.Default.DatabasesToBeBackedup.ToArray(), "\\");
                foreach (var p in packages)
                {
                    string log = string.Empty;
                    success &= BackupDatabaseBLL.BackupDatabases(out log, p.Value.Lable, p.Value.Items.ToArray());
                    sb.AppendLine(log);
                }

                if (success)
                {
                    this.Status = StepStatusEnum.Pass;
                    this.ResultDetail = new StepResultDetail("Successfully backed up the databases.");
                }
                else
                {
                    this.Status = StepStatusEnum.Failed;
                    this.ResultDetail = new StepResultDetail("Backed up the databases failed. Please check logs for more detailed information.");
                }
            }
            catch (Exception ex)
            {
                this.Status = StepStatusEnum.Failed;
                this.ResultDetail = new StepResultDetail("Error has occurred, please check log.", ExceptionHelper.CentralProcessSingle2(ex));
            }
            finally
            {
                if (this.ResultDetail != null)
                {
                    string message = sb.ToString();
                    if (!string.IsNullOrEmpty(message))
                    {
                        this.ResultDetail.Message += "Execution log: \r\n{0}".FormatWith(message);
                    }
                }

                
            }
        }
        #endregion Methods

        #region Helpers
        #endregion Helpers
    }
}
