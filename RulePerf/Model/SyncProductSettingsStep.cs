// -----------------------------------------------------------------------
// <copyright file="SyncProductSettingsStep.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Model
{
    using System;
    using Microsoft.Scs.Test.RiskTools.RulePerf.BLL;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Properties;

    /// <summary>
    /// A step to sync bed settings with product
    /// </summary>
    [Serializable]
    public class SyncProductSettingsStep : Step
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SyncProductSettingsStep" /> class with default properties.
        /// </summary>
        public SyncProductSettingsStep()
        {
            this.Name = "Sync production settings to bed";
            this.Description = "Sync production settings to bed";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncProductSettingsStep" /> class with default properties.
        /// </summary>
        /// <param name="name">The step name.</param>
        /// <param name="description">The description for this step.</param>
        public SyncProductSettingsStep(string name, string description)
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
                return "SyncSettingsCommand|DownloadRiMEConfigReferencedList|CommandsUserName|CommandsPassword|CommandsDomain";
            }
        }
        #endregion Properties

        #region Methods
        /// <summary>
        /// Execute this step.
        /// </summary>
        protected override void ExecuteMain()
        {
            string log = string.Empty;
            try
            {
                this.Status = StepStatusEnum.Executing;

                string cmd = Settings.Default.SyncSettingsCommand;
                int result = ThirdPartyProgramBLL.EnhancedRunCommand(
                    out log,
                    cmd,
                    Properties.Settings.Default.DownloadRiMEConfigReferencedList.ToArray()
                    //, Properties.Settings.Default.CommandsUserName
                    //, Properties.Settings.Default.CommandsPassword
                    //, Properties.Settings.Default.CommandsDomain
                    );

                switch (result)
                {
                    case 0:
                        if (!log.Contains("Error occured. The remote name could not be resolved: ", StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.Status = StepStatusEnum.Pass;
                            this.ResultDetail = new StepResultDetail("Successfully ran the command: {0}.".FormatWith(cmd));
                        }
                        else
                        {
                            this.Status = StepStatusEnum.Failed;
                            this.ResultDetail = new StepResultDetail("The command '{0}' failed.".FormatWith(cmd));
                        }
                        break;

                    case -1073741510:
                        this.Status = StepStatusEnum.Cancelled;
                        this.ResultDetail = new StepResultDetail("User cancelled the command: {0}.".FormatWith(cmd));
                        break;

                    case -532462766:
                        this.Status = StepStatusEnum.Warning;
                        this.ResultDetail = new StepResultDetail("This step has run successfully, but some exceptions had been thrown by that step during running. Please check log file for more detailed information.");
                        break;

                    default:
                        this.Status = StepStatusEnum.Failed;
                        this.ResultDetail = new StepResultDetail("Command didn't run successfully, please check log for more detailed information.");
                        break;
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
                    if (!string.IsNullOrEmpty(log))
                    {
                        this.ResultDetail.Message += "Execution log: \r\n{0}".FormatWith(log);
                    }

                    Log.Info(this.ResultDetail.Message);
                }                
            }
        }
        #endregion Methods

        #region Helpers
        #endregion Helpers
    }
}
