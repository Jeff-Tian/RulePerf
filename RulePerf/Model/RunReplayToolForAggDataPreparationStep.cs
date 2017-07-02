// -----------------------------------------------------------------------
// <copyright file="RunReplayToolForAggDataPreparationStep.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Model
{
    using System;
    using System.IO;
    using Microsoft.Scs.Test.RiskTools.RulePerf.BLL;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Properties;

    /// <summary>
    /// A step to run replay tool to generate some aggregation data on bed
    /// </summary>
    [Serializable]
    public class RunReplayToolForAggDataPreparationStep : Step
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunReplayToolForAggDataPreparationStep" /> class.
        /// </summary>
        public RunReplayToolForAggDataPreparationStep()
        {
            this.Name = "Run replay tool for Agg data generation";
            this.Description = "Run replay tool to generate some Agg data.";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RunReplayToolForAggDataPreparationStep" /> class.
        /// </summary>
        /// <param name="name">The step name.</param>
        /// <param name="description">The description for this step.</param>
        public RunReplayToolForAggDataPreparationStep(string name, string description)
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
                return "ReplayToolPath|ReplayToolDataFilePath|ReplayToolCount|ReplayToolTPS";
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
                string path = Path.Combine(Settings.Default.ReplayToolPath, "RiskApiCaller.exe");
                string cmd = "{0} call /isZipped:{1} /TPS:{2} /path:{3} /count:{4} /newGuid:{5} /needLog:{6} /rptType:\"{7}\" /description:\"{8}\" /isBaseLine:{9} /isDisplay:{10} /jumpMinute:{11}".FormatWith(path, true, Properties.Settings.Default.ReplayToolTPS, Properties.Settings.Default.RiskAPICaller_DataFilePath, Properties.Settings.Default.ReplayToolCount, true, false, string.Empty, "For generating agg data", false, false, 0);
                int result = ThirdPartyProgramBLL.RunCommand(out log, cmd);

                switch (result)
                {
                    case 0:
                        this.Status = StepStatusEnum.Pass;
                        this.ResultDetail = new StepResultDetail("Successfully ran replay tool.");
                        break;

                    case -1073741510:
                        this.Status = StepStatusEnum.Cancelled;
                        this.ResultDetail = new StepResultDetail("User cancelled the replay tool run.");
                        break;

                    default:
                        this.Status = StepStatusEnum.Failed;
                        this.ResultDetail = new StepResultDetail("Replay tool didn't run successfully, please check log for more detailed information.");
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
