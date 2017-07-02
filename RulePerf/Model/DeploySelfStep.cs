// -----------------------------------------------------------------------
// <copyright file="TestStep.cs" company="Microsoft">
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
    public class DeploySelfStep : Step
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BackupDatabasesStep" /> class. With default properties.
        /// </summary>
        public DeploySelfStep()
        {
            this.Name = "Deploy self step";
            this.Description = "Deploy self to another machine.";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupDatabasesStep" /> class. With customized properties.
        /// </summary>
        /// <param name="name">The step name</param>
        /// <param name="description">The description for this step</param>
        public DeploySelfStep(string name, string description)
        {
            this.Name = name;
            this.Description = description;
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
                return "DeployTargetPath|DeployUserName|DeployPassword|DeployDomain";
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
            // Note: This step is used by StepsProcessor to deploy other steps, it can NOT swallow exceptions by itself.
            try
            {
                this.Status = StepStatusEnum.Executing;

                //string selfPath = System.Windows.Forms.Application.StartupPath;
                string selfPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                //Impersonator impersonator = new Impersonator("administrator", ".", "#Bugsfor$");
                Impersonator impersonator = new Impersonator(
                    Properties.Settings.Default.DeployUserName,
                    Properties.Settings.Default.DeployDomain,
                    Properties.Settings.Default.DeployPassword
                    );

                if (!Directory.Exists(Properties.Settings.Default.DeployTargetPath))
                {
                    Directory.CreateDirectory(Properties.Settings.Default.DeployTargetPath);
                }

                string targetPath = Path.Combine(Properties.Settings.Default.DeployTargetPath, Path.GetFileName(selfPath));
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        File.Copy(
                            selfPath,
                            targetPath,
                            true);

                        if (Properties.Settings.Default.DeployReferencedList != null)
                        {
                            foreach (string referencedFile in Properties.Settings.Default.DeployReferencedList)
                            {
                                File.Copy(referencedFile, Path.Combine(Path.GetDirectoryName(targetPath), Path.GetFileName(referencedFile)), true);
                            }
                        }
                        break;
                    }
                    catch (IOException)
                    {
                        if (i < 3)
                        {
                            System.Threading.Thread.Sleep(1000 * 30);
                            continue;
                        }
                        else
                            throw;
                    }
                }

                bool success = true;

                if (success)
                {
                    this.Status = StepStatusEnum.Pass;
                    this.ResultDetail = new StepResultDetail("Successfully deployed to {0}.".FormatWith(targetPath));
                }
                else
                {
                    this.Status = StepStatusEnum.Failed;
                    this.ResultDetail = new StepResultDetail("Deploying failed. Please check logs for more detailed information.");
                }

                Log.Info(this.ResultDetail.Message);
                impersonator.Undo();
            }
            catch (Exception ex)
            {
                // Make sure the exceptions are logged (If this step ran on a remote machine, we should log the exceptions on remote machine too)
                ExceptionHelper.CentralProcess(ex);
                throw;
            }
            finally
            {
                
            }
        }
        #endregion Methods

        #region Helpers
        #endregion Helpers
    }
}