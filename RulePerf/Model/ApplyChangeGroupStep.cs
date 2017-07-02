// -----------------------------------------------------------------------
// <copyright file="ApplyChangeGroupStep.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Model
{
    using System;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
    using Microsoft.Scs.Test.RiskTools.RulePerf.DAL;
    using Microsoft.Scs.Test.RiskTools.RulePerf.BLL;
    using System.IO;

    /// <summary>
    /// Apply change group
    /// </summary>
    /// 
    [Serializable()]
    public class ApplyChangeGroupStep : Step
    {
        public ApplyChangeGroupStep()
        {
            this.Name = "Appy change groups.";
            this.Description = "Appy change groups.";
        }

        public ApplyChangeGroupStep(string name, string description)
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
            get {
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
            get { return "ChangeGroupXmlFiles|ApplyChangeGroupCommand"; }
        }
        #endregion Properties

        #region Methods
        protected override void ExecuteMain()
        {
            string log = string.Empty;
            Impersonator impersonator = null;
            try
            {
                this.Status = StepStatusEnum.Executing;

                //foreach (string changeGroupXmlFile in Properties.Settings.Default.ChangeGroupXmlFiles)
                //{
                //    Properties.Settings.Default.DownloadRiMEConfigReferencedList.Add(changeGroupXmlFile);
                //}

                bool pass = true;
                this.ResultDetail = new StepResultDetail("Started.");

                string cmd = Properties.Settings.Default.ApplyChangeGroupCommand;
                impersonator = new Impersonator(
                    Properties.Settings.Default.DomainUserName,
                    Properties.Settings.Default.Domain,
                    Properties.Settings.Default.DomainPassword);

                foreach(string sourceFile in Properties.Settings.Default.ChangeGroupXmlFiles)
                {
                    string fileName = Path.GetFileName(sourceFile);
                    File.Copy(sourceFile, Path.Combine(Directory.GetCurrentDirectory(), fileName), true);
                    cmd = cmd.FormatWith(fileName);

                    int result = ThirdPartyProgramBLL.EnhancedRunCommand(out log, cmd);

                    switch (result)
                    {
                        case 0:
                            if (!log.Contains("Error occured. The remote name could not be resolved: ", StringComparison.InvariantCultureIgnoreCase))
                            {
                                this.ResultDetail.Message += "\r\nSuccessfully ran the command: {0}.".FormatWith(cmd);
                                pass = true;
                            }
                            else
                            {
                                this.Status = StepStatusEnum.Failed;
                                this.ResultDetail.Message += "\r\nThe command '{0}' failed.".FormatWith(cmd);
                                pass = false;
                            }
                            break;

                        case -1073741510:
                            this.Status = StepStatusEnum.Cancelled;
                            this.ResultDetail.Message += "\r\nUser cancelled the command: {0}.".FormatWith(cmd);
                            pass = false;
                            break;

                        case -532462766:
                            this.Status = StepStatusEnum.Warning;
                            this.ResultDetail.Message += "\r\nThis step has run successfully, but some exceptions had been thrown by that step during running. Please check log file for more detailed information.";
                            pass = false;
                            break;

                        default:
                            this.Status = StepStatusEnum.Failed;
                            this.ResultDetail.Message += "\r\nCommand didn't run successfully, please check log for more detailed information.";
                            pass = false;
                            break;
                    }

                    if(!pass) { break; }
                }
                
                if (pass) { this.Status = StepStatusEnum.Pass; }
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

                if (impersonator != null)
                {
                    impersonator.Undo();
                }
            }
        }
        #endregion Methods
    }
}
