// -----------------------------------------------------------------------
// <copyright file="SetupGlobalSettingStep.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Model
{
    using System;
    using Microsoft.Scs.Test.RiskTools.RulePerf.BLL;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;

    /// <summary>
    /// Setup Global setting by update the settings in RiMEConfig Database
    /// </summary>
    [Serializable]
    public class SetupGlobalSettingStep : Step
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetupGlobalSettingStep" /> class. With default properties.
        /// </summary>
        public SetupGlobalSettingStep()
        {
            this.Name = "Setup Global Setting";
            this.Description = "Set global settings. For example, you can disable Azure Write, change to all async write mode, on bed to be the same as on production.";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SetupGlobalSettingStep" /> class. With customized properties.
        /// </summary>
        /// <param name="name">The step name</param>
        /// <param name="description">Description for the step.</param>
        public SetupGlobalSettingStep(string name, string description)
        {
            this.Name = name;
            this.Description = description;
        }

        #region Properties
        /// <summary>
        /// Gets or sets a value indicating whether this step is checked or not. If this property is set to true, then it will be run by the <see cref="StepsProcessor"/>.
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
        /// Gets the description of the step
        /// </summary>
        public override string Description
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the Step status, the status is one of the <see cref="StepStatusEnum"/>.
        /// </summary>
        public override StepStatusEnum Status
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the step result. The result is an instance of <see cref="StepResultDetail"/>.
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
                ////return "AzureWriteKeysAndValues_Disable|AzureWriteKeysAndValues_Enable";
                return "GlobalSettings|ServerAssignmentFilePath|DomainUserName|DomainPassword|Domain";
            }
        }

        #endregion Properties

        #region Methods
        /// <summary>
        /// Execute this step.
        /// </summary>
        protected override void ExecuteMain()
        {
            try
            {
                this.Status = StepStatusEnum.Executing;
                int results = RiMEConfigBLL.SetupGlobalSettings(
                    Properties.Settings.Default.DomainUserName,
                    Properties.Settings.Default.DomainPassword,
                    Properties.Settings.Default.Domain,
                    Properties.Settings.Default.RemoteUserName,
                    Properties.Settings.Default.RemotePassword,
                    Properties.Settings.Default.RemoteDomain
                    );
                if (results > 0)
                {
                    this.Status = StepStatusEnum.Pass;
                    this.ResultDetail = new StepResultDetail("Successfully disabled Azure write!");
                }
                else
                {
                    this.Status = StepStatusEnum.Pass;
                    this.ResultDetail = new StepResultDetail("Didn't change any data in RiMEConfig.dbo.Config table. Please check log for detailed information. If no related error occurred, then it indicated that the Azure write had already been disabled before this step.");
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
                    Log.Info(this.ResultDetail.Message);
                }

                
            }
        }
        #endregion Methods

        #region Helpers
        #endregion Helpers
    }
}
