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
    /// Restart machine step
    /// </summary>
    /// 
    [Serializable()]
    public class RestartMachinesStep : Step
    {
        public RestartMachinesStep()
        {
            this.Name = "Restart machines.";
            this.Description = "Restart machines";
        }

        public RestartMachinesStep(string name, string description)
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
            get { return "RestartMachineList|RestartMachineTimeoutTimeSpan|RestartMachinePollInterval"; }
        }
        #endregion Properties

        #region Methods
        protected override void ExecuteMain()
        {
            try
            {
                this.Status = StepStatusEnum.Executing;
                List<string> failedMachineList = new List<string>();

                foreach (string machine in Properties.Settings.Default.RestartMachineList)
                {
                    #region Access is denied (5) ?
                    ////System.Diagnostics.Process process = System.Diagnostics.Process.Start("shutdown", "-r -f -m \\\\{0} -t 0".Format2(machine));
                    //Impersonator impersonator = new Impersonator(
                    //    Properties.Settings.Default.RemoteUserName,
                    //    Properties.Settings.Default.RemoteDomain,
                    //    Properties.Settings.Default.RemotePassword
                    //);
                    //CmdHelper cmd = new CmdHelper();
                    ////cmd.StartCmd("cmd /C echo {2} | runas /noprofile /user:bedrd\\{1} \"shutdown /r /f /m \\\\{0} /t 0\"".Format2(machine, Properties.Settings.Default.RemoteUserName, Properties.Settings.Default.RemotePassword));
                    ////cmd.StartCmd("shutdown /r /f /m \\\\{0} /t 0".Format2(machine));
                    ////cmd.StartCmd(@"whoami > C:\RulePerf\whoami.txt");
                    //if (cmd.LastExitCode != 0)
                    //{
                    //    failedMachineList.Add(machine);
                    //}
                    #endregion Access is denied (5) ?

                    #region Error 21 ?
                    //WMICmdHelper wmi = new WMICmdHelper(machine, "shutdown /r /f /t 0", Properties.Settings.Default.RemoteUserName, Properties.Settings.Default.RemotePassword, Properties.Settings.Default.RemoteDomain);
                    //wmi.RunCommandReturnOutput();
                    //if (wmi.ExitCodeCaptured && wmi.ExitCode != 0)
                    //{
                    //    failedMachineList.Add(machine);
                    //}
                    #endregion Error 21?

                    try
                    {
                        WMICmdHelper wmi = new WMICmdHelper(machine, "", Properties.Settings.Default.RemoteUserName, Properties.Settings.Default.RemotePassword, Properties.Settings.Default.RemoteDomain);
                        wmi.Reboot();
                    }
                    catch(Exception ex)
                    {
                        ExceptionHelper.CentralProcess(ex);
                        failedMachineList.Add(machine);
                    }
                }
                
                // Wait for them shuting down
                System.Threading.Thread.Sleep(new TimeSpan(0, 2, 0));

                DateTime startTime = DateTime.Now;
                TimeSpan timeout = TimeSpan.MaxValue;
                TimeSpan.TryParse(Properties.Settings.Default.RestartMachineTimeoutTimeSpan, out timeout);
                TimeSpan interval = TimeSpan.FromSeconds(30);
                TimeSpan.TryParse(Properties.Settings.Default.RestartMachinePollInterval, out interval);

                while (true)
                {
                    foreach (string machine in Properties.Settings.Default.RestartMachineList)
                    {
                        // rdp.exe listens on port 3389. If testing 3389 is connectable, then the machine should be ready for being remote logged on to
                        bool success = RemoteHelper.TestPort(System.Net.Dns.GetHostAddresses(machine)[0].ToString(), 3389);

                        if (!success)
                            goto PollWait;
                    }

                    System.Threading.Thread.Sleep(interval);
                    break;

                PollWait:
                    DateTime now = DateTime.Now;
                    TimeSpan diff = now.Subtract(startTime);
                    if (diff.CompareTo(timeout) >= 0)
                    {
                        throw new TimeoutException("Not all machines are started after '{0}' seconds".FormatWith(diff.TotalSeconds));
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(interval);
                    }
                }

                if (failedMachineList.Count >= Properties.Settings.Default.RestartMachineList.Count){
                    this.Status = StepStatusEnum.Failed;
                    this.ResultDetail = new StepResultDetail("All the shuting down commands are failed, please check log for detailed information.");
                }
                else if (failedMachineList.Count > 0)
                {
                    this.Status = StepStatusEnum.Warning;
                    this.ResultDetail = new StepResultDetail("Not all the machines are restarted successfully. The following machines are not shutdown successfully: {0}".FormatWith(string.Join(", ", failedMachineList.ToArray())));
                }
                else
                {
                    this.Status = StepStatusEnum.Pass;
                    this.ResultDetail = new StepResultDetail("All the machines are restarted successfully.");
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
