// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf
{
    using System;
    using System.Windows.Forms;
    using Microsoft.CSAT.Utilities;
    using Microsoft.Scs.Test.RiskTools.RulePerf.ConsoleHelpers;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Model;

    /// <summary>
    /// Entry point for RulePerf.exe
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The arguments. If run it without arguments, then the GUI will appear. Otherwise, only command line would be executed.</param>
        [STAThread]
        public static void Main(string[] args)
        {
            // Make sure log file would be generated during the run
            GlobalSettings.GlobalLogPath = Log.StartTraceListners();
            // Make sure any unhandled exceptions were logged.
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                try
                {
                    Log.Error("Unhandled exception logged. IsTerminating: {0}.", e.IsTerminating);
                    ExceptionHelper.CentralProcess(e.ExceptionObject as Exception);
                }
                catch (Exception ex)
                {
                    Log.Error("Exception caught when trying to cast ExceptionObject to Exception.");
                    ExceptionHelper.CentralProcess(ex);
                }
            };
            Log.Info("Started with parameters:\r\n{0}".FormatWith(string.Join(" ", args)));

            if (args != null && args.Length > 0)
            {
                #region Console version

                try
                {
                    RulePerfConsoleArgument consoleArgs = new RulePerfConsoleArgument();

                    ArgumentResult ar = RulePerfArgumentParser.ParseArgument(args, consoleArgs);
                    if (!ar.ParseSucceeded)
                    {
                        Log.Info("Arguments '{0}' were not parsed successfully.".FormatWith(args != null ? string.Join(" ", args) : string.Empty));
                        Console.WriteLine();
                        ar.PrintUsage();

                        Environment.Exit(1);
                    }

                    // TODO: use StepsProcessor to handle this, so that the Step StepProcessorStep can be deleted.
                    foreach (Step step in consoleArgs.Steps)
                    {
                        step.Execute();

                        Log.Info("Finished step <{0}> '{1}': '{2}'.".FormatWith(ar.SelectedOptionName, step.Name, step.Description));

                        if (step.Status != StepStatusEnum.Pass)
                        {
                            // Any error occurred, just stop process steps and exit with error code 1
                            Environment.Exit(1);
                        }
                    }

                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    ExceptionHelper.CentralProcess(ex);
                    Environment.Exit(2);
                }
                #endregion Console version
            }
            else
            {
                Log.Info("Starting with UI...");

                #region GUI version
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new RulePerfForm());
                #endregion GUI version
            }
        }
    }

    public static class GlobalSettings
    {
        public static string GlobalLogPath;
        public static System.Collections.Generic.Dictionary<string, StepStatusEnum>
            StepStatus = new System.Collections.Generic.Dictionary<string, StepStatusEnum>();
    }
}
