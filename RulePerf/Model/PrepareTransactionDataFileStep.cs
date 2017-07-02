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
    using System.Threading;

    /// <summary>
    /// Prepare transaction data file as per the specified rule ids for replay tool
    /// </summary>
    /// 
    [Serializable()]
    public class PrepareTransactionDataFileStep : Step
    {
        public PrepareTransactionDataFileStep()
        {
            this.Name = "Prepare transaction data file.";
            this.Description = "Prepare transaction data file as per the specified rule ids for replay tool.";
        }

        public PrepareTransactionDataFileStep(string name, string description)
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
                return "RuleIds|RuleSourceFolder|RuleDestFolder";
            }
        }

        /// <summary>
        /// The decompress exceptions
        /// </summary>
        private List<Exception> decompressExceptions = new List<Exception>();

        /// <summary>
        /// Gets or sets the decompress exceptions.
        /// </summary>
        /// <value>
        /// The decompress exceptions.
        /// </value>
        public List<Exception> DecompressExceptions
        {
            get
            {
                return this.decompressExceptions;
            }

            set
            {
                this.decompressExceptions = value;
            }
        }
        #endregion Properties

        protected override void ExecuteMain()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                this.Status = StepStatusEnum.Executing;
                Impersonator impersonator = new Impersonator(
                    Properties.Settings.Default.DomainUserName,
                    Properties.Settings.Default.Domain,
                    Properties.Settings.Default.DomainPassword);

                foreach (string ruleId in Properties.Settings.Default.RuleIds)
                {
                    string sourcePath = Path.Combine(
                           Properties.Settings.Default.RuleSourceFolder,
                           "{0}.dat.gz".FormatWith(ruleId)
                           );
                    string destFolder = Properties.Settings.Default.RuleDestFolder;
                    if (!destFolder.EndsWith("\\")) destFolder += "\\";
                    string destPath = Path.Combine(
                        Path.GetDirectoryName(destFolder),
                        "{0}.dat.gz".FormatWith(ruleId)
                        );
                    File.Copy(sourcePath, destPath, true);
                    sb.AppendLine("Copied {0} to {1}".FormatWith(sourcePath, destPath));

                    // Decompress is not needed any more after Harry's update to MixMerge.exe tool
                    //AsyncDecompress(destPath);
                    //Decompress(destPath);
                }

                impersonator.Undo();

                this.Status = StepStatusEnum.Pass;
                this.ResultDetail = new StepResultDetail(sb.ToString() + "\r\nSuccessfully prepared the gz files.");
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

        /// <summary>
        /// Decompress the file asynchronously.
        /// </summary>
        /// <param name="dataFileFullName">Full name of the data file.</param>
        private void AsyncDecompress(string dataFileFullName)
        {
            Thread t = new Thread(() =>
            {
                Decompress(dataFileFullName);
            }) { Name = "Decompress Data File '{0}'.".FormatWith(dataFileFullName), IsBackground = false };
            t.Start();
        }

        private void Decompress(string dataFileFullName)
        {
            try
            {
                Log.Info("Deompressing file '{0}'...".FormatWith(dataFileFullName));
                string decompressedFileFullName = ZipHelper.DecompressFile(dataFileFullName);
                Log.Info("Decompressed file '{0}' to '{1}'.".FormatWith(dataFileFullName, decompressedFileFullName));
            }
            catch (Exception ex)
            {
                this.DecompressExceptions.Add(ExceptionHelper.CentralProcessSingle2(ex));
            }
        }
    }
}
