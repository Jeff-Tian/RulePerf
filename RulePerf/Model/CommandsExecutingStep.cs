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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    /// <summary>
    /// Restart machine step
    /// </summary>
    /// 
    [Serializable()]
    public class CommandsExecutingStep : Step
    {
        public CommandsExecutingStep()
        {
            this.Name = "Execute commands one by one.";
            this.Description = "Execute commands one by one.";
        }

        public CommandsExecutingStep(string name, string description)
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
            get { return "Commands|CommandsUserName|CommandsPassword|CommandsDomain"; }
        }
        #endregion Properties

        #region Methods
        protected override void ExecuteMain()
        {
            try
            {
                this.Status = StepStatusEnum.Executing;
                Impersonator impersonator = new Impersonator(
                    Properties.Settings.Default.CommandsUserName,
                    Properties.Settings.Default.CommandsDomain,
                    Properties.Settings.Default.CommandsPassword
                    );

                System.Collections.Generic.List<CommandExecutionResult> results = new CmdHelper().ExecuteCommandsConcurrently(
                    Properties.Settings.Default.Commands.ToArray(),
                    Properties.Settings.Default.CommandsUserName,
                    Properties.Settings.Default.CommandsPassword,
                    Properties.Settings.Default.CommandsDomain);

                this.Status = StepStatusEnum.Pass;
                this.ResultDetail = new StepResultDetail("");
                StringBuilder sb = new StringBuilder();
                this.ResultDetail.Exceptions = new List<Exception>();
                foreach (CommandExecutionResult result in results)
                {
                    if (result.ExitCode != 0)
                    {
                        this.Status = StepStatusEnum.Failed;
                    }
                    sb.Append(result.StandardOutput.ToString());
                    this.ResultDetail.Exceptions.Add(new Exception(result.StandardError.ToString()));
                }

                this.ResultDetail.Message = sb.ToString();
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
    }
}
