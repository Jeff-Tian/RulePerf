using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Scs.Test.RiskTools.RulePerf.Model;
using System;
using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
using System.IO;
using System.Text;
using Microsoft.Scs.Test.RiskTools.RulePerf.Event;
using System.Threading;

namespace Microsoft.Scs.Test.RiskTools.RulePerf.BLL
{
    /// <summary>
    /// A wrapper to run the specified steps.
    /// </summary>
    public class StepsProcessor
    {
        /// <summary>
        /// Processes the steps.
        /// </summary>
        /// <param name="steps">The steps to be processed.</param>
        /// <param name="allStepsCompleted">The callback that need to be executed after all steps completed.</param>
        public static void AsyncProcessSteps(List<Step> steps, RunWorkerCompletedEventHandler allStepsCompleted)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += delegate
            {
                ProcessSteps(steps);
            };
            if (allStepsCompleted != null)
                worker.RunWorkerCompleted += allStepsCompleted;
            worker.RunWorkerAsync();
        }

        public static void ProcessSteps(List<Step> steps)
        {
            RegisterEvents();
            RiskPerfStatusEventPublisher.Instance.PublishEvent(RiskPerfStatus.Started);

            #region Process all steps
            foreach (Step step in steps)
            {
                if (step.Checked)
                {
                    if (!step.IsAsync)
                    {
                        ProcessStep(step);

                        if (step.Status != StepStatusEnum.Pass)
                        {
                            //RiskPerfStatusEventPublisher.Instance.PublishEvent(RiskPerfStatus.Blocked, step.Name, GlobalSettings.GlobalLogPath);
                            RiskPerfStatusEventPublisher.Instance.PublishEvent(RiskPerfStatus.Blocked, step.Name);
                            throw new Exception("Step {0} failed, stopped processing!".FormatWith(step.Name));
                        }
                    }
                    else
                    {
                        lock (GlobalSettings.StepStatus)
                        {
                            GlobalSettings.StepStatus.Upsert(step.GetType().Name, step.Status);
                        }

                        ProcessStepDelegate dl = ProcessStep;
                        Step s = step.Clone();
                        dl.BeginInvoke(s, (asyncResult) =>
                        {
                            if (asyncResult == null) throw new ArgumentNullException("asyncResult");
                            ProcessStepDelegate psd = asyncResult.AsyncState as ProcessStepDelegate;
                            System.Diagnostics.Trace.Assert(psd != null, "Invalid object type");
                            psd.EndInvoke(asyncResult);

                            lock (GlobalSettings.StepStatus)
                            {
                                GlobalSettings.StepStatus.Upsert(step.GetType().Name, step.Status);
                            }

                            if (s.Status != StepStatusEnum.Pass)
                            {
                                RiskPerfStatusEventPublisher.Instance.PublishEvent(RiskPerfStatus.Blocked, s.Name);
                                throw new Exception("Step {0} failed, stopped processing!".FormatWith(s.Name));
                            }
                        }, dl);
                    }
                }
            }
            #endregion Process all steps

            //RiskPerfStatusEventPublisher.Instance.PublishEvent(RiskPerfStatus.Stopped, null, GlobalSettings.GlobalLogPath);
            RiskPerfStatusEventPublisher.Instance.PublishEvent(RiskPerfStatus.Stopped);
        }

        public static void ProcessStep(Step step)
        {
            // Check its deployment sequence
            if (step.DeploySequence == null || step.DeploySequence.Count <= 0)
            {
                step.Execute();
            }
            else
            {
                #region multiple hops
                try
                {
                    // Deploy it
                    step.Status = StepStatusEnum.Deploying;
                    step.ResultDetail = new StepResultDetail("Deploying started...");
                    step.ResultDetail.Exceptions = new List<Exception>();

                    string selfPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    string selfName = Path.GetFileName(selfPath);

                    Properties.Settings.Default.ResultLogPath = step.DeploySequence[step.DeploySequence.Count - 1].AvailableTransferPath;
                    List<string> deployCommands = new List<string>();
                    string deployCommandFormat = "{0}\\" + selfName + " " + typeof(DeploySelfStep).Name + " /DeployTargetPath:\"{1}\" /DeployUserName:\"{2}\" /DeployPassword:\"{3}\" /DeployDomain:\"{4}\"";
                    List<string> remoteExeCommands = new List<string>();
                    string remoteExeCommandFormat = "{0}\\" + selfName + " " + step.GetType().Name + (GetStepParametersForCommandLine(step)).Replace("{", "{{").Replace("}", "}}");


                    // Prepare deployment commands
                    for (int i = 0; i < step.DeploySequence.Count; i++)
                    {
                        deployCommands.Add(
                            GetRemoteExecutionCommandLiteForDeploy(deployCommandFormat, step.DeploySequence.GetRange(0, i + 1))
                        );
                    }

                    Properties.Settings.Default.ResultLogPath = step.DeploySequence[step.DeploySequence.Count - 1].AvailableTransferPath;
                    // Prepare remote execution commands
                    for (int i = 0; i < step.DeploySequence.Count; i++)
                    {
                        remoteExeCommands.Add(
                            GetRemoteExecutionCommandLiteForRunStep(remoteExeCommandFormat, step.DeploySequence.GetRange(0, i + 1))
                        );
                    }

                    // run deploy
                    #region deploy
                    DeploySelfStep deploySelfStep = new DeploySelfStep();
                    Properties.Settings.Default.DeployTargetPath = step.DeploySequence[0].UNCPath;
                    Properties.Settings.Default.DeployUserName = step.DeploySequence[0].UserName;
                    Properties.Settings.Default.DeployPassword = step.DeploySequence[0].Password;
                    Properties.Settings.Default.DeployDomain = step.DeploySequence[0].Domain;
                    deploySelfStep.Execute();

                    step.ResultDetail.Message += deploySelfStep.ResultDetail.Message;
                    if (deploySelfStep.ResultDetail.Exceptions != null && deploySelfStep.ResultDetail.Exceptions.Count > 0)
                        step.ResultDetail.Exceptions.AddRange(deploySelfStep.ResultDetail.Exceptions);
                    step.ResultDetail.Message += "Deployment sequence 0 done.";

                    for (int i = 1; i < deployCommands.Count; i++)
                    {
                        Properties.Settings.Default.RemoteMachine = step.DeploySequence[0].Server;
                        Properties.Settings.Default.RemoteCommand = deployCommands[i];
                        Properties.Settings.Default.RemoteUserName = step.DeploySequence[0].UserName;
                        Properties.Settings.Default.RemotePassword = step.DeploySequence[0].Password;
                        Properties.Settings.Default.RemoteDomain = step.DeploySequence[0].Domain;
                        Properties.Settings.Default.ResultLogPath = step.DeploySequence[0].AvailableTransferPath;
                        Properties.Settings.Default.RemoteTimeout = step.DeploySequence[0].Timeout.ToString();
                        RemoteExeStep remoteExeStep = new RemoteExeStep();
                        remoteExeStep.Execute();

                        step.ResultDetail.Message += remoteExeStep.ResultDetail.Message;
                        if (remoteExeStep.ResultDetail.Exceptions != null && remoteExeStep.ResultDetail.Exceptions.Count > 0)
                            step.ResultDetail.Exceptions.AddRange(remoteExeStep.ResultDetail.Exceptions);
                        step.ResultDetail.Message += "Deployment sequence {0} done.".FormatWith(i);
                    }

                    step.Status = StepStatusEnum.DeployingCompleted;
                    #endregion deploy

                    // Execute
                    step.Status = StepStatusEnum.Executing;

                    Properties.Settings.Default.RemoteMachine = step.DeploySequence[0].Server;
                    Properties.Settings.Default.RemoteCommand = remoteExeCommands[remoteExeCommands.Count - 1];
                    Properties.Settings.Default.RemoteUserName = step.DeploySequence[0].UserName;
                    Properties.Settings.Default.RemotePassword = step.DeploySequence[0].Password;
                    Properties.Settings.Default.RemoteDomain = step.DeploySequence[0].Domain;
                    Properties.Settings.Default.ResultLogPath = step.DeploySequence[0].AvailableTransferPath;
                    Properties.Settings.Default.RemoteTimeout = step.DeploySequence[0].Timeout.ToString();

                    RemoteExeStep remoteExeStep2 = new RemoteExeStep();
                    remoteExeStep2.Execute();

                    step.ResultDetail.Message += remoteExeStep2.ResultDetail.Message;
                    if (remoteExeStep2.ResultDetail.Exceptions != null && remoteExeStep2.ResultDetail.Exceptions.Count > 0)
                        step.ResultDetail.Exceptions.AddRange(remoteExeStep2.ResultDetail.Exceptions);
                    step.ResultDetail.Message += "Remote execution Done.";

                    //step.Status = StepStatusEnum.Pass;
                    step.Status = remoteExeStep2.Status;
                }
                catch (Exception ex)
                {
                    step.Status = StepStatusEnum.Failed;
                    if (step.ResultDetail == null) step.ResultDetail = new StepResultDetail("Error occured.", ex);
                    else if (step.ResultDetail.Exceptions == null)
                        step.ResultDetail.Exceptions = new List<Exception>(new Exception[] { ex });
                    else step.ResultDetail.Exceptions.Add(ex);
                }
                finally
                {
                }
                #endregion multiple hops
            }
        }

        private delegate void ProcessStepDelegate(Step step);

        private static void RegisterEvents()
        {
            try
            {
                string[] toList = Properties.Settings.Default.EmailToList.ToArray();
                string[] ccList = Properties.Settings.Default.EmailCCList.ToArray();

                RiskPerfStatusEventPublisher eventPublisher = RiskPerfStatusEventPublisher.Instance;
                RiskPerfStatusEventSubscriber[] subscribers = new RiskPerfStatusEventSubscriber[]{
                new EmailSubscriber(toList, ccList)
            };

                foreach (RiskPerfStatusEventSubscriber subscriber in subscribers)
                {
                    subscriber.Subscribe(eventPublisher);
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.CentralProcess(ex);
            }
        }

        /// <summary>
        /// Gets the remote execution command.
        /// </summary>
        /// <param name="metaCommand">The meta command format (The command that finally run on the target machine).</param>
        /// <param name="hops">The hops machine. The last hop machine the final target machine.</param>
        /// <returns></returns>
        /// <remarks>
        ///     The logic is work now, but how to handle the recursive double quotes? For example: /RemoteCommand:"/RemoteCommand:"RulePerf.exe DeploySelfStep""
        ///     The nested double quotes problem only raises when the hops count > 2
        ///     
        ///     TODO: Solution found, use \" to escape the double quotes sign "
        /// </remarks>
        private static string GetRemoteExecutionCommandLiteForDeploy(string metaCommandFormat, List<DeployTargetModel> hops)
        {
            if (hops == null || hops.Count <= 0)
                return "";
            else
            {
                string selfPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string selfName = Path.GetFileName(selfPath);

                string wrappedCommand = metaCommandFormat;
                wrappedCommand = wrappedCommand.FormatWith(
                    hops.Count > 1 ? "{0}".FormatWith(hops[hops.Count - 2].Path.TrimEnd('\\')) : "{0}".FormatWith(Path.GetDirectoryName(selfPath).TrimEnd('\\')),
                    hops[hops.Count - 1].UNCPath,
                    hops.Count > 1 ? "{0}".FormatWith(hops[hops.Count-2].UserName) : hops[hops.Count-1].UserName,
                    hops.Count > 1 ? "{0}".FormatWith(hops[hops.Count-2].Password) : hops[hops.Count - 1].Password,
                    hops.Count > 1 ? "{0}".FormatWith(hops[hops.Count-2].Domain) : hops[hops.Count-1].Domain
                );

                for (int i = hops.Count - 1; i >= 2; i--)
                {
                    wrappedCommand = "{0} {1} /RemoteMachine:\"{2}\" /RemoteCommand:\"{3}\" /RemoteUserName:\"{4}\" /RemotePassword:\"{5}\" /RemoteDomain:\"{6}\" /ResultLogPath:\"{7}\" /RemoteTimeout:\"{8}\"".FormatWith(
                        Path.Combine(hops[i - 1].Path, selfName),
                        typeof(RemoteExeStep).Name,
                        hops[i - 1].Server,
                        wrappedCommand,
                        hops[i - 1].UserName,
                        hops[i - 1].Password,
                        hops[i-1].Domain,
                        hops[i-1].AvailableTransferPath,
                        hops[i-1].Timeout
                    );
                }

                return wrappedCommand;
            }
        }

        private static string GetRemoteExecutionCommandLiteForRunStep(string metaCommandFormat, List<DeployTargetModel> hops)
        {
            if (hops == null || hops.Count <= 0)
                return "";
            else
            {
                string selfPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string selfName = Path.GetFileName(selfPath);

                string wrappedCommand = metaCommandFormat;
                wrappedCommand = wrappedCommand.FormatWith(
                    "{0}".FormatWith(hops[hops.Count - 1].Path.TrimEnd('\\'))
                );

                for (int i = hops.Count - 1; i >= 1; i--)
                {
                    wrappedCommand = "{0} {1} /RemoteMachine:{2} /RemoteCommand:\"{3}\" /RemoteUserName:\"{4}\" /RemotePassword:\"{5}\" /RemoteDomain:\"{6}\" /ResultLogPath:\"{7}\" /RemoteTimeout:\"{8}\"".FormatWith(
                        Path.Combine(hops[i].Path, selfName),
                        typeof(RemoteExeStep).Name,
                        hops[i].Server,
                        wrappedCommand.Replace("\"", "\\\""),
                        hops[i].UserName,
                        hops[i].Password,
                        hops[i].Domain,
                        hops[i - 1].AvailableTransferPath,
                        hops[i - 1].Timeout
                    );
                }

                return wrappedCommand;
            }
        }

        private static string GetStepParametersForCommandLine(Step step)
        {
            string[] commonSettings = new CommonStep().SettingNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            string[] stepSettings = step.SettingNames.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            Dictionary<string, string> settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string settingName in commonSettings)
            {
                settings.Upsert(settingName, SettingEntityModel.GetSingle(settingName, true).SettingValue);
            }

            foreach (string settingName in stepSettings)
            {
                settings.Upsert(settingName, SettingEntityModel.GetSingle(settingName, true).SettingValue);
            }

            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string,string> setting in settings)
            {
                string parameter = " /" + setting.Key + ":\"{0}\"";
                // Important! /folder:"C:\folder\" will cause problem by escape the last double quote ".
                string settingValue = setting.Value.TrimEnd('\\');
                parameter = parameter.FormatWith(settingValue);

                sb.Append(parameter);
            }

            return sb.ToString();
        }
    }
}
