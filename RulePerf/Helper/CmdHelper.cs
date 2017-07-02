using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Security;
using System.Collections.Generic;

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Helper
{
    /// <summary>
    /// Helper to handle command line tool
    /// </summary>
    public class CmdHelper
    {
        private string input;
        public string Input { get { return input; } set { input = value; } }

        private StringBuilder stdOutput;
        public StringBuilder StdOutput { get { return stdOutput; } }

        private StringBuilder stdErr;
        public StringBuilder StdErr { get { return stdErr; } }

        private string workingDirectory;
        public string WorkingDirectory { get { return workingDirectory; } }

        private int lastExitCode;
        public int LastExitCode { get { return lastExitCode; } }

        public CmdHelper() : this(Directory.GetCurrentDirectory())
        {
        }

        public CmdHelper(string workingDirectory) {
            if (!Directory.Exists(workingDirectory))
                Directory.CreateDirectory(workingDirectory);
            this.workingDirectory = workingDirectory;

            stdOutput = new StringBuilder();
            stdErr = new StringBuilder();
        }

        /// <summary>
        /// Clears the output.
        /// </summary>
        public void ClearOutput()
        {
            this.stdOutput = new StringBuilder();
            this.stdErr = new StringBuilder();
        }

        /// <summary>
        /// Starts the CMD directly.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>Started process id</returns>
        public int StartCmdDirectly(string input)
        {
            this.input = input;
            string comSpec = Environment.ExpandEnvironmentVariables("%ComSpec%");

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.WorkingDirectory = this.workingDirectory;
            startInfo.FileName = comSpec;
            startInfo.Arguments = "/K " + input;
            startInfo.UseShellExecute = true;
            process.StartInfo = startInfo;
            int id = -1;
            try
            {
                stdOutput.AppendLine(string.Format("Trying to start '{0}'...", input));
                bool start = process.Start();
                id = process.Id;
                stdOutput.AppendLine(string.Format("Process {0}: '{1}' started {2}...", process.Id, input, start ? "successfully" : "failed"));

                if (process.WaitForExit(Int32.MaxValue))
                {
                    stdOutput.AppendLine();
                    stdOutput.AppendLine(string.Format("Process {0}: '{1}' exited with: {2}", process.Id, input, process.ExitCode));
                    this.lastExitCode = process.ExitCode;
                }
                else
                {
                    stdOutput.AppendLine();
                    stdOutput.AppendLine(string.Format("Timeout: {0} when waiting for process: {1} '{2}'", TimeSpan.FromMilliseconds(Int32.MaxValue), process.Id, input));
                }
            }
            catch (Exception ex)
            {
                stdOutput.AppendLine(string.Format("{0}\r\nCorresponding input: '{1}'", ExceptionHelper.ExceptionLog(ex), input));
            }
            finally
            {
                try
                {
                    if (process != null && !process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    stdOutput.AppendLine(ExceptionHelper.ExceptionLog(ex));
                }
                process.Close();
                process.Dispose();

                if (stdOutput.ToString().Length > 0)
                    Log.Info(stdOutput.ToString());
                if (stdErr.ToString().Length > 0)
                    Log.Error(stdErr.ToString());
            }

            return id;
        }

        /// <summary>
        /// Executes a command and waits for its return or kills it
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>Started process id.</returns>
        public int ExecuteCommand(string command, string userName = "", string password = "", string domain = "")
        {
            command = command.Trim();
            Log.Info(string.Format("Trying to start '{0}' with {1}\\{2}, password: \"{3}\"...", command, domain, userName, password));
            this.input = command;
            this.lastExitCode = -1;

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            if (!string.IsNullOrEmpty(userName))
            {
                startInfo.UserName = userName;
                SecureString secureString = new SecureString();

                for (int i = 0; i < password.Length; i++)
                {
                    secureString.AppendChar(password[i]);
                }

                startInfo.Password = secureString;

                startInfo.Domain = domain;
            }

            // Skip quote, for example, "C:\Share Docs\bla.exe" option /arg:v /arg2:v2
            int quoteIndex = -1;
            if (command.StartsWith("\""))
            {                
                quoteIndex = command.IndexOf('"', 1);
            }

            int index = command.IndexOf(' ', quoteIndex + 1);
            if (index > 0)
            {
                startInfo.FileName = command.Substring(0, index).Trim('\"');
                startInfo.Arguments = command.Substring(index + 1);
            }
            else
            {
                startInfo.FileName = command.Trim('\"');
            }
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.WorkingDirectory = this.workingDirectory;
            //startInfo.FileName = "cmd.exe";
            //startInfo.Arguments = "/C " + input;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            process.StartInfo = startInfo;
            int id = -1;
            try
            {
                bool start = process.Start();
                id = process.Id;
                stdOutput.AppendLine(string.Format("Process {0}: '{1}' started {2}...", process.Id, command, start ? "successfully" : "failed"));

                using(AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null) outputWaitHandle.Set();
                        else
                        {
                            stdOutput.AppendLine(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null) errorWaitHandle.Set();
                        else
                        {
                            stdErr.AppendLine(e.Data);
                        }
                    };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    if (process.WaitForExit(Int32.MaxValue) && outputWaitHandle.WaitOne(Int32.MaxValue) && errorWaitHandle.WaitOne(Int32.MaxValue))
                    {
                        stdOutput.AppendLine();
                        stdOutput.AppendLine(string.Format("Process {0}: '{1}' exited with: {2}", process.Id, command, process.ExitCode));
                        this.lastExitCode = process.ExitCode;
                    }
                    else
                    {
                        stdOutput.AppendLine();
                        stdOutput.AppendLine(string.Format("Timeout: {0} when waiting for process: {1} '{2}'", TimeSpan.FromMilliseconds(Int32.MaxValue), process.Id, command));
                    }
                }
            }
            catch (Exception ex)
            {
                stdOutput.AppendLine(string.Format("{0}\r\nCorresponding input: '{1}'", ExceptionHelper.ExceptionLog(ex), command));
            }
            finally
            {
                if (process != null)
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill();
                    }
                    catch(Exception ex) {
                        stdOutput.AppendLine(ExceptionHelper.ExceptionLog(ex));
                    }
                }
                process.Close();
                process.Dispose();

                if (stdOutput.ToString().Length > 0)
                    Log.Info(stdOutput.ToString());
                if (stdErr.ToString().Length > 0)
                    Log.Error(stdErr.ToString());
            }

            return id;
        }

        public CommandExecutionResult ExecuteCommand2(string command, string workingDirectory, string userName = "", string password = "", string domain = "")
        {
            CommandExecutionResult result = new CommandExecutionResult()
            {
                CommandText = command.Trim(),
                ExitCode = -1
            };
            Log.Info(string.Format("Trying to start '{0}' as '{1}\\{2}'...", result.CommandText, domain, userName));

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            if (!string.IsNullOrEmpty(userName))
            {
                startInfo.UserName = userName;
                SecureString secureString = new SecureString();

                for (int i = 0; i < password.Length; i++)
                {
                    secureString.AppendChar(password[i]);
                }

                startInfo.Password = secureString;

                startInfo.Domain = domain;
            }
            int index = result.CommandText.IndexOf(' ', 0);
            if (index > 0)
            {
                startInfo.FileName = result.CommandText.Substring(0, index);
                startInfo.Arguments = result.CommandText.Substring(index + 1);
            }
            else
            {
                startInfo.FileName = result.CommandText;
            }
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            //startInfo.FileName = "cmd.exe";
            //startInfo.Arguments = "/C " + input;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.WorkingDirectory = workingDirectory;
            if (!Directory.Exists(workingDirectory)) { Directory.CreateDirectory(workingDirectory); }
            process.StartInfo = startInfo;
            try
            {
                bool start = process.Start();
                result.ProcessId = process.Id;
                result.StandardOutput.AppendLine(string.Format("Process {0}: '{1}' started {2}...", process.Id, command, start ? "successfully" : "failed"));

                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null) outputWaitHandle.Set();
                        else
                        {
                            result.StandardOutput.AppendLine(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null) errorWaitHandle.Set();
                        else
                        {
                            result.StandardError.AppendLine(e.Data);
                        }
                    };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    if (process.WaitForExit(Int32.MaxValue) && outputWaitHandle.WaitOne(Int32.MaxValue) && errorWaitHandle.WaitOne(Int32.MaxValue))
                    {
                        result.StandardOutput.AppendLine();
                        result.StandardOutput.AppendLine(string.Format("Process {0}: '{1}' exited with: {2}", process.Id, command, process.ExitCode));
                        result.ExitCode = process.ExitCode;
                    }
                    else
                    {
                        result.StandardOutput.AppendLine();
                        result.StandardOutput.AppendLine(string.Format("Timeout: {0} when waiting for process: {1} '{2}'", TimeSpan.FromMilliseconds(Int32.MaxValue), process.Id, command));
                    }
                }
            }
            catch (Exception ex)
            {
                result.StandardOutput.AppendLine(string.Format("{0}\r\nCorresponding input: '{1}'", ExceptionHelper.ExceptionLog(ex), command));
            }
            finally
            {
                try
                {
                    if (process != null)
                    {
                        try
                        {
                            if (!process.HasExited)
                                process.Kill();
                        }
                        catch (Exception ex)
                        {
                            result.StandardOutput.AppendLine(ExceptionHelper.ExceptionLog(ex));
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.StandardOutput.Append("Exception met with this command: {0}\r\n{1}".FormatWith(command, ExceptionHelper.ExceptionLog(ex)));
                }
                finally
                {
                    process.Close();
                    process.Dispose();

                    //Log.Info(result.StandardOutput.ToString());
                    //Log.Error(result.StandardError.ToString());
                }
            }

            return result;
        }

        public CommandExecutionResult ExecuteCommand2(string command, string userName = "", string password = "", string domain = "")
        {
            return ExecuteCommand2(command, Directory.GetCurrentDirectory(), userName, password, domain);
        }

        /// <summary>
        /// Executes the commands concurrently and waits for them all to exit.
        /// </summary>
        /// <param name="commands">The commands.</param>
        public List<CommandExecutionResult> ExecuteCommandsConcurrently(string[] commands, string userName = "", string password = "", string domain = "")
        {
            List<CommandExecutionResult> results = new List<CommandExecutionResult>();
            CountdownEvent countdownEvent = new CountdownEvent(commands.Length);

            // Start workers
            for (int i = 0; i < commands.Length; i++)
            {
                string command = commands[i];
                ThreadStart ts = delegate()
                {
                    try
                    {
                        CommandExecutionResult result = this.ExecuteCommand2(command, userName, password, domain);
                        lock (results)
                        {
                            results.Add(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.CentralProcess(ex);
                    }
                    finally
                    {
                        countdownEvent.Signal();
                    }
                };

                Thread t = new Thread(ts);
                t.Name = "ExecuteCommandsConcurrently[{0}]".FormatWith(i);
                t.Start();
            }

            // Wait for workers.
            countdownEvent.Wait();
            countdownEvent.Dispose();

            return results;
        }

        /// <summary>
        /// Start cmd. If the executable file is on a network path, then copy it to current directory first.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>Started process id.</returns>
        public int EnhancedStartCmd(string input)
        {
            string[] inputPart = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string exeFileFullName = inputPart[0];
            if (exeFileFullName.StartsWith("\\\\", StringComparison.InvariantCultureIgnoreCase))
            {
                string localFullName = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(exeFileFullName));
                File.Copy(exeFileFullName, localFullName);
                input = localFullName + input.Remove(0, exeFileFullName.Length);
            }

            return ExecuteCommand(input);
        }
    }

    public class CommandExecutionResult
    {
        public string CommandText { get; set; }
        public int ProcessId { get; set; }
        private int exitCode = -1;
        public int ExitCode { get { return this.exitCode; } set { this.exitCode = value; } }
        public StringBuilder StandardOutput = new StringBuilder();
        public StringBuilder StandardError = new StringBuilder();
    }
}
