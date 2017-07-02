// -----------------------------------------------------------------------
// <copyright file="RulePerfConsoleArgument.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CSAT.Utilities;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Model;

    /// <summary>
    /// The valid arguments for RulePerf.exe command line
    /// </summary>
    public class RulePerfConsoleArgument : IArgumentObject
    {
        /// <summary>
        /// The options
        /// </summary>
        private Dictionary<string, string> options = new Dictionary<string, string>() { 
            { typeof(ExportDataFromSqlServerStep).Name, typeof(ExportDataFromSqlServerStep).Name }, 
            { typeof(SetupGlobalSettingStep).Name, typeof(SetupGlobalSettingStep).Name }, 
            { typeof(BackupDatabasesStep).Name, typeof(BackupDatabasesStep).Name }, 
            { typeof(SyncProductSettingsStep).Name, typeof(SyncProductSettingsStep).Name }, 
            { typeof(CopyDataFilesStep).Name, typeof(CopyDataFilesStep).Name }, 
            { typeof(ImportProductionDataStep).Name, typeof(ImportProductionDataStep).Name }, 
            { typeof(RunReplayToolForAggDataPreparationStep).Name, typeof(RunReplayToolForAggDataPreparationStep).Name }, 
            { typeof(RunReplayToolForBaseLineStep).Name, typeof(RunReplayToolForBaseLineStep).Name }, 
            { typeof(RunReplayToolForChangedStep).Name, typeof(RunReplayToolForChangedStep).Name },
            { typeof(ApplyChangeGroupStep).Name, typeof(ApplyChangeGroupStep).Name },
            { typeof(TestStep).Name, typeof(TestStep).Name },
            { typeof(DeploySelfStep).Name, typeof(DeploySelfStep).Name },
            { typeof(RemoteExeStep).Name, typeof(RemoteExeStep).Name },
            { typeof(StepProcessorStep).Name, typeof(StepProcessorStep).Name },
            { typeof(RestartMachinesStep).Name, typeof(RestartMachinesStep).Name },
            { typeof(DownloadChangeGroupStep).Name, typeof(DownloadChangeGroupStep).Name },
            { typeof(MergeDataFilesStep).Name, typeof(MergeDataFilesStep).Name },
            { typeof(PrepareTransactionDataFileStep).Name, typeof(PrepareTransactionDataFileStep).Name },
            { typeof(RollbackChangeGroupStep).Name, typeof(RollbackChangeGroupStep).Name }, 
            { typeof(DownloadRiMEConfigStep).Name, typeof(DownloadRiMEConfigStep).Name },
            { typeof(RestartServicesStep).Name, typeof(RestartServicesStep).Name },
            { typeof(CommandsExecutingStep).Name, typeof(CommandsExecutingStep).Name }
        };

        /// <summary>
        /// The steps
        /// </summary>
        private List<Step> steps = new List<Step>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RulePerfConsoleArgument" /> class.
        /// </summary>
        public RulePerfConsoleArgument()
        {
            ////BuildSteps();
        }

        /// <summary>
        /// Gets the steps.
        /// </summary>
        /// <value>
        /// The steps.
        /// </value>
        public List<Step> Steps
        {
            get
            {
                return this.steps;
            }
        }

        /// <summary>
        /// Gets the {OptionName: OptionDescription} dictionary object
        /// </summary>
        public IDictionary<string, string> Options
        {
            get
            {
                return this.options;
            }
        }

        /// <summary>
        /// Builds the step.
        /// </summary>
        /// <param name="name">The name.</param>
        public void BuildStep(string name)
        {
            Type t = typeof(Step);

            Type stepType = t.Assembly.GetType("{0}.{1}".FormatWith(t.Namespace, name));
            if (stepType != null)
            {
                Step step = Activator.CreateInstance(stepType) as Step;
                this.steps.Add(step);
            }
        }

        /// <summary>
        /// Builds the steps.
        /// </summary>
        public void BuildSteps()
        {
            Type t = typeof(Step);

            foreach (var ele in this.options)
            {
                Type stepType = t.Assembly.GetType("{0}.{1}".FormatWith(t.Namespace, ele.Key));
                if (stepType != null)
                {
                    Step step = Activator.CreateInstance(stepType) as Step;
                    this.steps.Add(step);
                }
            }
        }

        /// <summary>
        /// Gets the name of the option.
        /// </summary>
        /// <param name="step">The step.</param>
        /// <returns>The option name</returns>
        public string GetOptionName(Step step)
        {
            return step.GetType().Name;
        }

        /// <summary>
        /// Gets the option names.
        /// </summary>
        /// <returns>The option name array</returns>
        public string[] GetOptionNames()
        {
            return (from step in this.steps
                    select this.GetOptionName(step)).ToArray<string>();
        }

        /// <summary>
        /// Gets the step by name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The step object</returns>
        public Step GetStep(string name)
        {
            return this.steps.SingleOrDefault((step) =>
            {
                return name.Equals(step.GetType().Name);
            });
        }
    }
}
