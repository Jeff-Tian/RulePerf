// -----------------------------------------------------------------------
// <copyright file="CopyDataFilesStep.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Model
{
    using System;
    using Microsoft.Scs.Test.RiskTools.RulePerf.BLL;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Properties;
    using System.Windows.Forms;

    /// <summary>
    /// A step to copy data files from a path to another path
    /// </summary>
    [Serializable]
    public class CopyDataFilesStep : Step
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CopyDataFilesStep" /> class with default properties.
        /// </summary>
        public CopyDataFilesStep()
        {
            this.Name = "Copy Data Files from transfer to bed transfer";
            this.Description = "Copy Data Files from transfer to bed";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CopyDataFilesStep" /> class with customized properties.
        /// </summary>
        /// <param name="name">The step name.</param>
        /// <param name="description">The description for this step.</param>
        public CopyDataFilesStep(string name, string description)
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
                return "CopyDataFiles_From|CopyDataFiles_To|CopyDataFiles_DeleteSource|DataExporter_SuccessFlagFileName";
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
                this.ResultDetail = new StepResultDetail("");
                int result = 0;

                StepStatusEnum stepStatus = StepStatusEnum.Executing;

                //lock (GlobalSettings.StepStatus)
                //{
                //    stepStatus = GlobalSettings.StepStatus.GetValueOrDefault(
                //        typeof(ExportDataFromSqlServerStep).Name, StepStatusEnum.NotExecutable);
                //}

                //if (stepStatus != StepStatusEnum.NotExecutable)
                //{
                do
                {
                    try
                    {
                        result += DataImporter.CopyDataFiles(Settings.Default.CopyDataFiles_From, Settings.Default.CopyDataFiles_To, Properties.Settings.Default.CopyDataFiles_DeleteSource);
                    }
                    catch (System.IO.IOException ioex)
                    {
                        ExceptionHelper.CentralProcess(ioex);
                        Log.Info("Goto wait and then will start another try...");
                        goto WaitAndAgain;
                    }

                    if (MessageBox.Show("Finished one round of copy, do you want to start a new round?",
                        "Rule perf automation", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1)
                        == DialogResult.No)
                    {
                        stepStatus = StepStatusEnum.Pass;
                        break;
                    }

                WaitAndAgain:
                    System.Threading.Thread.Sleep(60000);

                    //lock (GlobalSettings.StepStatus)
                    //{
                    //    stepStatus = GlobalSettings.StepStatus.GetValueOrDefault(
                    //        typeof(ExportDataFromSqlServerStep).Name, StepStatusEnum.NotExecutable);
                    //}
                } while (true || stepStatus == StepStatusEnum.Executing || stepStatus == StepStatusEnum.Deploying
                    || stepStatus == StepStatusEnum.DeployingCompleted);

                switch (stepStatus)
                {
                    case StepStatusEnum.Cancelled:
                        this.Status = StepStatusEnum.Failed;
                        this.ResultDetail.Message += "Failed due to step {0} cancelled.".FormatWith(typeof(ExportDataFromSqlServerStep).Name);
                        break;
                    case StepStatusEnum.Failed:
                        this.Status = StepStatusEnum.Failed;
                        this.ResultDetail.Message += "Failed due to step {0} failed.".FormatWith(typeof(ExportDataFromSqlServerStep).Name);
                        break;
                    case StepStatusEnum.Pass:
                        this.Status = StepStatusEnum.Pass;
                        break;
                    case StepStatusEnum.Warning:
                        this.Status = StepStatusEnum.Warning;
                        this.ResultDetail.Message += "Warning due to step {0}'s result is warning.".FormatWith(typeof(ExportDataFromSqlServerStep).Name);
                        break;
                    default:
                        break;
                }
                //}
                //else
                //{
                //    result += DataImporter.CopyDataFiles(Settings.Default.CopyDataFiles_From, Settings.Default.CopyDataFiles_To, true);

                //    this.Status = StepStatusEnum.Pass;
                //}
                this.ResultDetail.Message += "Successfully copied {0} file(s) from '{1}' to '{2}'.".FormatWith(result, Settings.Default.TransferFolder, Settings.Default.BedTransferFolder);
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
