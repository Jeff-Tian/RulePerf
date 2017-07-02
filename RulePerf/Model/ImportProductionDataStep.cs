// -----------------------------------------------------------------------
// <copyright file="ImportProductionDataStep.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Model
{
    using System;
    using System.Text;
    using Microsoft.Scs.Test.RiskTools.RulePerf.BLL;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;

    /// <summary>
    /// A step to import production data to bed
    /// </summary>
    [Serializable]
    public class ImportProductionDataStep : Step
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImportProductionDataStep" /> class.
        /// </summary>
        public ImportProductionDataStep()
        {
            this.Name = "Import production data";
            this.Description = "Import production data to bed. You need to copy the data files that exported from product SQL Server and specify their paths in the {0} file.".FormatWith(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportProductionDataStep" /> class.
        /// </summary>
        /// <param name="name">The step name.</param>
        /// <param name="description">The description for this step.</param>
        public ImportProductionDataStep(string name, string description)
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
                return "DataImporter_DatabaseNameMapping|DataImporter_DataDirectory|DataImporter_BCP_Path|DataImporter_IsContinueRun|CommandsUserName|CommandsDomain|CommandsPassword";
            }
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
                Impersonator impersonator = new Impersonator(
                    Properties.Settings.Default.DomainUserName,
                    Properties.Settings.Default.Domain,
                    Properties.Settings.Default.DomainPassword
                    );
                DataImporter importer = new DataImporter();
                success = importer.DoSequence(Properties.Settings.Default.DataImporter_IsContinueRun);
                if (success)
                {
                    if (importer.Exceptions == null || importer.Exceptions.Count <= 0)
                    {
                        if (importer.TargetDataFileInfoList.Count > 0)
                        {
                            this.Status = StepStatusEnum.Pass;
                            this.ResultDetail = new StepResultDetail("Successfully imported data into databases. Total data file(s) processed: {0}".FormatWith(importer.TargetDataFileInfoList.Count));
                        }
                        else
                        {
                            this.Status = StepStatusEnum.Warning;
                            this.ResultDetail = new StepResultDetail("This step didn't meet any error, however, no data file has been found under the specified path '{0}' or '{1}'.".FormatWith(Microsoft.Scs.Test.RiskTools.RulePerf.Properties.Settings.Default.BedTransferFolder, Properties.Settings.Default.DataImporter_DataDirectory));
                        }
                    }
                    else
                    {
                        this.Status = StepStatusEnum.Failed;
                        this.ResultDetail = new StepResultDetail("Met errors during importing. Total data file(s) processed: {0}".FormatWith(importer.TargetDataFileInfoList.Count), importer.Exceptions);
                    }
                }
                else
                {
                    this.Status = StepStatusEnum.Failed;
                    this.ResultDetail = new StepResultDetail("Importing data failed. Please check logs for more detailed information.");
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
