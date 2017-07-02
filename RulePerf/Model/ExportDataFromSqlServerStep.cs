// -----------------------------------------------------------------------
// <copyright file="ExportDataFromSqlServerStep.cs" company="Microsoft">
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
    /// A step to export data from SQL server, usually is expected to be run on the PHX domain
    /// </summary>
    [Serializable]
    public class ExportDataFromSqlServerStep : Step
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExportDataFromSqlServerStep" /> class with default properties.
        /// </summary>
        public ExportDataFromSqlServerStep()
        {
            this.Name = "Export data from product sql server";
            this.Description = "Export data from product sql server.";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportDataFromSqlServerStep" /> class with customized properties.
        /// </summary>
        /// <param name="name">The step name.</param>
        /// <param name="description">The description for this step.</param>
        public ExportDataFromSqlServerStep(string name, string description)
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
                return "DataExporter_ServerName|DataExporter_TableList";
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

                var exporter = new DataExporter();
                bool result = exporter.DoSequence(out log);
                if (result)
                {
                    if (exporter.SuccessTableCount == exporter.SourceTableList.Count)
                    {
                        if (exporter.CompressAndCopyExceptions.Count <= 0)
                        {
                            this.Status = StepStatusEnum.Pass;
                            this.ResultDetail = new StepResultDetail("Successfully exported data from SQL Server {0}. Total data file(s) processed: {1}".FormatWith(Settings.Default.DataExporter_ServerName, exporter.SuccessTableCount));
                        }
                        else
                        {
                            this.Status = StepStatusEnum.Warning;
                            this.ResultDetail = new StepResultDetail("Successfully exported data from SQL Server {0}. Total data file(s) processed: {1}. However, not all of them were compressed and copied to {2} successfully, you may need to check detailed log and then manually compress and copy them.".FormatWith(Settings.Default.DataExporter_ServerName, exporter.SuccessTableCount, Settings.Default.TransferFolder));
                        }
                    }
                    else
                    {
                        this.Status = StepStatusEnum.Failed;
                        this.ResultDetail = new StepResultDetail("Error has occurred, please check log.");
                    }
                }
                else
                {
                    this.Status = StepStatusEnum.Failed;
                    this.ResultDetail = new StepResultDetail("Error has occurred, please check log.");
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
