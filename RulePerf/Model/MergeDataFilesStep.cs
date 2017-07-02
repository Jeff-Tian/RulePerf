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
    using System.IO;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
    using Microsoft.Scs.Test.RiskTools.RulePerf.BLL;

    /// <summary>
    /// Prepare transaction data file as per the specified rule ids for replay tool
    /// </summary>
    /// 
    [Serializable()]
    public class MergeDataFilesStep : Step
    {
        public MergeDataFilesStep()
        {
            this.Name = "Merge multiple data files into 1.";
            this.Description = "Merge multiple data files into 1.";
        }

        public MergeDataFilesStep(string name, string description)
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
                return "ReplayToolPath|RiskAPICaller_DataFilePath|RuleIds|RiskAPICaller_Count_Run|TransactionCountPerRule";
            }
        }
        #endregion Properties

        protected override void ExecuteMain()
        {
            try
            {
                string log = "";
                this.Status = StepStatusEnum.Executing;
                Impersonator impersonator = new Impersonator(
                    Properties.Settings.Default.DomainUserName,
                    Properties.Settings.Default.Domain,
                    Properties.Settings.Default.DomainPassword);

                Properties.Settings.Default.RiskAPICaller_Count_Run = (Properties.Settings.Default.TransactionCountPerRule * Properties.Settings.Default.RuleIds.Count).ToString();

                Properties.Settings.Default.Save();

                string path = Path.Combine(Properties.Settings.Default.ReplayToolPath, "MixedMerge.exe");

                string cmd = "{0} Mix /SrcFileFolder:\"{1}\" /RuleID:\"{2}\" /DefaultDstCnt:{3} /DstFileFolder:\"{4}\" /ImpersonateUserName:{5} /ImpersonateDomain:{6} /ImpersonatePassword:{7}".FormatWith(
                    path,
                    Properties.Settings.Default.RuleDestFolder, 
                    string.Join(",", Properties.Settings.Default.RuleIds.ToArray()),
                    Properties.Settings.Default.RiskAPICaller_Count_Run,
                    Properties.Settings.Default.RiskAPICaller_DataFilePath,
                    Properties.Settings.Default.DomainUserName, 
                    Properties.Settings.Default.Domain,
                    Properties.Settings.Default.DomainPassword
                    );

                Log.Info("Starting {0}".FormatWith(cmd));

                int result = ThirdPartyProgramBLL.RunCommand(out log, cmd);
                
                switch (result)
                {
                    case 0:
                        this.Status = StepStatusEnum.Pass;
                        this.ResultDetail = new StepResultDetail(log + "\r\nSuccessfully ran mixed merge tool.");
                        break;

                    case -1073741510:
                        this.Status = StepStatusEnum.Cancelled;
                        this.ResultDetail = new StepResultDetail(log + "\r\nUser cancelled the replay tool run.");
                        break;

                    case -532462766:
                        this.Status = StepStatusEnum.Warning;
                        this.ResultDetail = new StepResultDetail(log + "\r\nThis step has run successfully, but some exceptions had been thrown by that step during running. Please check log file for more detailed information.");
                        break;

                    default:
                        this.Status = StepStatusEnum.Failed;
                        this.ResultDetail = new StepResultDetail(log + "\r\nFailed, please check log for more detailed information.");
                        break;
                }

                impersonator.Undo();
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
    }
}
