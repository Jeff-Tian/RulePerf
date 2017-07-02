// -----------------------------------------------------------------------
// <copyright file="CommonStep.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Model
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A common step. This is not a executable step though, its mainly usage is for global settings.
    /// </summary>
    [Serializable]
    public class CommonStep : Step
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommonStep" /> class with default properties.
        /// </summary>
        public CommonStep()
        {
            this.Name = "Global Setting";
            this.Description = "This step is for configuration only";
            this.Status = StepStatusEnum.NotExecutable;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommonStep" /> class with customized properties.
        /// </summary>
        /// <param name="name">The step name.</param>
        /// <param name="description">The description for this step.</param>
        public CommonStep(string name, string description)
        {
            this.Name = name;
            this.Description = description;
            this.Status = StepStatusEnum.NotExecutable;
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
                return "ServerAssignmentFilePath|SQLConnectionTimeout|SQLCommandTimeout|TransferFolder|BedtransferFolder|Environment|ResultLogPath|RemoteTimeout|Domain|DomainUserName|DomainPassword|EmailToList|EmailCCList|EmailUserName|EmailPassword|DeployReferencedList";
            }
        }
        #endregion Properties

        #region Methods
        /// <summary>
        /// Execute this step.
        /// </summary>
        protected override void ExecuteMain()
        {
            this.Status = StepStatusEnum.NotExecutable;
            
        }
        #endregion Methods

        #region Helpers
        #endregion Helpers
    }
}
