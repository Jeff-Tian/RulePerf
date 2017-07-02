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

    /// <summary>
    /// Apply change group
    /// </summary>
    /// 
    [Serializable()]
    public class RollbackChangeGroupStep : Step
    {
        public RollbackChangeGroupStep()
        {
            this.Name = "Rollback the latest bypassed change groups to [RiMEConfig].[dbo].[Config] table directly.";
            this.Description = "Rollback the latest change groups to [RiMEConfig].[dbo].[Config] table directly.";
        }

        public RollbackChangeGroupStep(string name, string description)
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
            get { return ""; }
        }
        #endregion Properties

        #region Methods
        protected override void ExecuteMain()
        {
            try
            {
                this.Status = StepStatusEnum.Executing;

                Dictionary<string, DataStructure.Package<string, string>> packages = DataStructure.Package<string, string>.FromStringArray(
                    Properties.Settings.Default.BackedUpDatabasesWhenApplyingChanges.ToArray(), "\\");

                if (packages.Count > 0)
                {
                    int success = 0;
                    int failed = 0;
                    Impersonator impersonator = null;
                    string[] parts = Properties.Settings.Default.Environment.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string server in packages.Keys)
                    {
                        if (parts[0].Equals("OneBox", StringComparison.InvariantCultureIgnoreCase) && parts.Length > 1 && parts[1].Equals(SqlServerHelper.ConnectionString.Server, StringComparison.InvariantCultureIgnoreCase))
                        {
                            impersonator = new Impersonator();
                        }

                        SqlServerHelper.ConnectionString.Server = server;
                        foreach (string bakInfo in packages[server].Items)
                        {
                            string database = bakInfo.Split(':')[0];
                            string bakFile = bakInfo.Split(':')[1];

                            try
                            {
                                Log.Info("Restoring database {0} from {1} on server {2}...".FormatWith(database, bakFile, server));
                                if (SqlServerHelper.RestoreDatabase(database, bakFile))
                                {
                                    success++;
                                    Log.Info("Restored database {0} from {1} on server {2}.".FormatWith(database, bakFile, server));
                                }
                                else
                                {
                                    failed++;
                                    Log.Info("Restoring database {0} from {1} on server {2} failed!".FormatWith(database, bakFile, server));
                                }
                            }
                            catch 
                            {
                                failed++;
                                continue;
                            }
                        }
                    }

                    if (impersonator != null)
                    {
                        impersonator.Undo();
                    }

                    if (failed == 0)
                    {
                        this.Status = StepStatusEnum.Pass;
                        this.ResultDetail = new StepResultDetail("Rollback change groups succeded.");
                    }
                    else
                    {
                        throw new Exception("Restoring database status: success = {0}; failed = {1}".FormatWith(success, failed));
                    }
                }
                else
                {
                    this.Status = StepStatusEnum.Cancelled;
                    this.ResultDetail = new StepResultDetail("Rollback change groups cancelled due to no bak files generated by ApplyChangeGroupsStep are found for RiMEConfig database. That means the ApplyChangeGroupStep may not have been run before this step.");
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
                    Log.Info(this.ResultDetail.Message);
                }

                
            }
        }
        #endregion Methods
    }
}
