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
    using System.Xml.Serialization;
    using System.Xml;
    using System.Runtime.Serialization.Formatters.Binary;

    /// <summary>
    /// A step class that is used to remote execute a command.
    /// </summary>
    [Serializable]
    public class RemoteExeStep : Step
    {
        private string maskedRemoteCommand = "";
        private string maskedCommand = "";

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteExeStep" /> class. With default properties.
        /// </summary>
        public RemoteExeStep()
        {
            this.Name = "Remote execution Step";
            this.Description = "Remote execute a command.";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteExeStep" /> class. With customized properties.
        /// </summary>
        /// <param name="name">The step name</param>
        /// <param name="description">The description for this step</param>
        public RemoteExeStep(string name, string description)
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
                return "RemoteMachine|RemoteCommand|RemoteUserName|RemotePassword|RemoteDomain";
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
            this.Status = StepStatusEnum.Executing;
            string logPath = Properties.Settings.Default.ResultLogPath.Trim();
            string cmd = UpsertArgument(Properties.Settings.Default.RemoteCommand, "ResultLogPath", logPath);
                
            Impersonator impersonator = new Impersonator(Properties.Settings.Default.RemoteUserName, Properties.Settings.Default.RemoteDomain, Properties.Settings.Default.RemotePassword);
            WMICmdHelper wmiHelper = new WMICmdHelper(
                Properties.Settings.Default.RemoteMachine,
                cmd,
                Properties.Settings.Default.RemoteUserName,
                Properties.Settings.Default.RemotePassword,
                Properties.Settings.Default.RemoteDomain
                );
            wmiHelper.RunCommandReturnOutput();

            if (wmiHelper.ExitCodeCaptured)
            {
                if (wmiHelper.ExitCode == 0)
                {
                    this.Status = StepStatusEnum.Pass;
                    this.ResultDetail = new StepResultDetail("Successfully executed the following command on {0} as user {2}.\r\n{1}".FormatWith(
                        Properties.Settings.Default.RemoteMachine,
                        Properties.Settings.Default.RemoteCommand,
                        Properties.Settings.Default.RemoteUserName
                        ));
                }
                else
                {
                    this.Status = StepStatusEnum.Failed;
                    this.ResultDetail = new StepResultDetail("Failed to execute the following command on {0} as user {2}. The exit code is {3}. Please check log for more details.\r\n{1}".FormatWith(
                        Properties.Settings.Default.RemoteMachine,
                        Properties.Settings.Default.RemoteCommand,
                        Properties.Settings.Default.RemoteUserName,
                        wmiHelper.ExitCode
                        ));
                }
            }
            else
            {
                // Check the log path
                Log.Info("Failed to get the exit code of the following command on {0}.\r\n{1}\r\nChecking log from network path...".FormatWith(
                        Properties.Settings.Default.RemoteMachine,
                        Properties.Settings.Default.RemoteCommand
                        ));
                try
                {
                    DateTime start = DateTime.Now;
                    DateTime end = DateTime.Now;
                    TimeSpan timeout = TimeSpan.MaxValue;
                    TimeSpan.TryParse(Properties.Settings.Default.RemoteTimeout.ToString(), out timeout);

                    Step step = null;

                    while (!File.Exists(logPath) || FileHelper.IsFileLocked(logPath) || (step = Step.GetFromFile(logPath)) == null)
                    {
                        if (end.Subtract(start).CompareTo(timeout) < 0)
                        {
                            System.Threading.Thread.Sleep(TimeSpan.FromMinutes(1));
                            end = DateTime.Now;
                        }
                        else
                        {
                            throw new TimeoutException("Timed out after {0} waiting for the result file {1}.".FormatWith(
                                timeout.ToString(), logPath
                                ));
                        }
                    }

                    if (step != null)
                    {
                        this.Status = step.Status;
                        this.ResultDetail = new StepResultDetail("", new List<Exception>());
                        if (step.ResultDetail != null)
                        {
                            this.ResultDetail.Message += step.ResultDetail.Message;
                            if (step.ResultDetail.Exceptions != null && step.ResultDetail.Exceptions.Count > 0)
                            {
                                this.ResultDetail.Exceptions.AddRange(step.ResultDetail.Exceptions);
                            }
                        }
                    }
                }
                catch (TimeoutException tex)
                {
                    this.Status = StepStatusEnum.Timeout;
                    this.ResultDetail = new StepResultDetail("Timed out when waiting for the result of '{0}' executed on {1} from network path.".FormatWith(
                        Properties.Settings.Default.RemoteCommand,
                        Properties.Settings.Default.RemoteMachine), tex);
                        
                }
                catch (Exception ex)
                {
                    this.Status = StepStatusEnum.Warning;
                    this.ResultDetail = new StepResultDetail("Failed to get the exit code of the following command on {0}.\r\n{1}".FormatWith(
                        Properties.Settings.Default.RemoteMachine,
                        Properties.Settings.Default.RemoteCommand
                        ), ex);
                }
            }

            impersonator.Undo();            
        }
        #endregion Methods

        #region Helpers
        private string[] ParseCommandArguments(string command, char switchChar = '/')
        {
            List<string> parts = new List<string>();

            bool quoteStart = false;
            bool separateStart = false;
            bool escapeStart = false;
            int index = 0;
            for (int i = 0; i < command.Length; i++)
            {
                switch (command[i])
                {
                    case ' ':
                        if (!quoteStart)
                        {
                            separateStart = true;
                        }
                        else
                        {
                            escapeStart = false;
                            separateStart = false;
                        }
                        break;
                    case '"':
                        if (!escapeStart)
                        {
                            quoteStart = !quoteStart;
                        }
                        else
                        {
                            escapeStart = false;
                            separateStart = false;
                        }
                        break;
                    case '\\':
                        if (!escapeStart)
                        {
                            escapeStart = true;
                        }
                        else
                        {
                            escapeStart = false;
                            separateStart = false;
                        }
                        break;
                    default:
                        if (command[i].Equals(switchChar))
                        {
                            if (separateStart)
                            {
                                parts.Add(command.Substring(index, i - 1 - index));
                                index = i + 1;
                            }
                        }
                        escapeStart = false;
                        separateStart = false;
                        break;
                }
            }

            if (index <= command.Length)
            {
                parts.Add(command.Substring(index));
            }

            return parts.ToArray();
        }

        private string UpsertArgument(string command, string argument, string value)
        {
            command = this.MaskRemoteCommand(command);
            command = this.MaskCommand(command);

            //string[] commandParts = command.Split(new string[]{" /"}, StringSplitOptions.RemoveEmptyEntries);
            string[] commandParts = ParseCommandArguments(command);
            Dictionary<string, string> arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i < commandParts.Length; i++)
            {
                string argumentName = "";
                string argumentValue = "";
                int delimiterIndex = commandParts[i].IndexOf(':');
                if (delimiterIndex >= 0)
                {
                    argumentName = commandParts[i].Substring(0, delimiterIndex).TrimStart('/');
                    if (delimiterIndex < commandParts[i].Length)
                        argumentValue = commandParts[i].Substring(delimiterIndex + 1).Trim('"');
                }
                else
                {
                    argumentName = commandParts[i].TrimStart('/');
                }

                try
                {
                    arguments.Add(argumentName, argumentValue);
                }
                catch (Exception ex)
                {
                    throw new Exception("Exception caught when Add (key = {0}, value = {1})!".FormatWith(argumentName, argumentValue), ex);
                }
            }

            arguments.Upsert(argument, value);

            List<string> argList = new List<string>();
            foreach (KeyValuePair<string, string> arg in arguments)
            {
                argList.Add("/{0}:\"{1}\"".FormatWith(arg.Key, arg.Value));
            }

            return this.UnmaskCommand(
                this.UnmaskRemoteCommand(string.Join(" ", commandParts[0], string.Join(" ", argList.ToArray()))));
        }

        private string MaskRemoteCommand(string command, string mask = "\"@@@@@@\"")
        {
            int index = command.IndexOf("/RemoteCommand:", StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                bool quoteStart = false;
                bool escape = false;
                int indexEnd = command.Length - 1;

                index += 15;
                for (int i = index; i < command.Length; i++)
                {
                    switch (command[i])
                    {
                        case '"':
                            if (!escape)
                            {
                                quoteStart = !quoteStart;
                                if (!quoteStart)
                                {
                                    indexEnd = i;
                                }
                            }
                            escape = false;
                            break;
                        case '\\':
                            escape = true;
                            break;
                        case ' ':
                            if (!quoteStart)
                            {
                                indexEnd = i - 1;
                            }
                            break;
                        default:
                            break;
                    }

                    if (indexEnd < command.Length - 1) break;
                }

                this.maskedRemoteCommand = command.Substring(index, indexEnd - index + 1);
                return command.Substring(0, index) + mask + command.Substring(indexEnd+1);
            }
            else
            {
                return command;
            }
        }

        private string UnmaskRemoteCommand(string command, string mask = "\"@@@@@@\"")
        {
            return command.Replace(mask, this.maskedRemoteCommand);
        }

        private string MaskCommand(string command, string mask = "\"$$$$$\"")
        {
            int index = command.IndexOf("/Commands:", StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                bool quoteStart = false;
                bool escape = false;
                int indexEnd = command.Length - 1;

                index += 10;
                for (int i = index; i < command.Length; i++)
                {
                    switch (command[i])
                    {
                        case '"':
                            if (!escape)
                            {
                                quoteStart = !quoteStart;
                                if (!quoteStart)
                                {
                                    indexEnd = i;
                                }
                            }
                            escape = false;
                            break;
                        case '\\':
                            escape = true;
                            break;
                        case ' ':
                            if (!quoteStart)
                            {
                                indexEnd = i - 1;
                            }
                            break;
                        default:
                            break;
                    }

                    if (indexEnd < command.Length - 1) break;
                }

                this.maskedCommand = command.Substring(index, indexEnd - index + 1);
                return command.Substring(0, index) + mask + command.Substring(indexEnd + 1);
            }
            else
            {
                return command;
            }
        }

        private string UnmaskCommand(string command, string mask = "\"$$$$$\"")
        {
            return command.Replace(mask, this.maskedCommand);
        }
        #endregion Helpers
    }
}