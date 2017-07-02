// -----------------------------------------------------------------------
// <copyright file="IStep.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
    using System.Xml.Serialization;
    using System.Xml;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    /// <summary>
    /// Status of a step
    /// </summary>
    public enum StepStatusEnum
    {
        /// <summary>
        /// Indicating a step has not been started yet
        /// </summary>
        NotStarted,

        /// <summary>
        /// Indicating a step is currently running
        /// </summary>
        Executing,

        /// <summary>
        /// Indicating a step has failed
        /// </summary>
        Failed,

        /// <summary>
        /// Indicating a step has been cancelled by user
        /// </summary>
        Cancelled,

        /// <summary>
        /// Indicating there were warnings being thrown during the executing
        /// </summary>
        Warning,

        /// <summary>
        /// Indicating a step has been successfully executed
        /// </summary>
        Pass, 

        /// <summary>
        /// Indicating a step is not executable but for other uses
        /// </summary>
        NotExecutable,

        /// <summary>
        /// Indicating a step is under deployment
        /// </summary>
        Deploying,

        DeployingCompleted,

        Timeout
    }
    
    /// <summary>
    /// The Step interface. A Step is to be executed by the <see cref="StepsProcessor"/>.
    /// </summary>
    /// 
    [Serializable()]
    public abstract class Step
    {
        #region Properties
        /// <summary>
        /// Gets or sets a value indicating whether this step is checked. A checked step would be run by <see cref="StepsProcessor"/>.
        /// </summary>
        public abstract bool Checked { get; set; }

        /// <summary>
        /// Gets the step name.
        /// </summary>
        public abstract string Name { get; set; }

        /// <summary>
        /// Gets or sets the Step status, the status is one of the <see cref="StepStatusEnum"/>.
        /// </summary>
        public abstract StepStatusEnum Status { get; set; }

        /// <summary>
        /// Gets the description for this step.
        /// </summary>
        public abstract string Description { get; set; }
        
        /// <summary>
        /// Gets or sets the step result. The result is an instance of <see cref="StepResultDetail"/>.
        /// </summary>
        [Browsable(false)]
        public abstract StepResultDetail ResultDetail { get; set; }

        /// <summary>
        /// Gets or sets the serial number of the step. The serial number indicates the step's order among all the steps.
        /// </summary>
        [Browsable(false)]
        public abstract int Sequence { get; set; }

        /// <summary>
        /// Gets or sets the deploy sequence.
        /// </summary>
        /// <value>
        /// The deploy sequence.
        /// </value>
        [Browsable(true)]
        public abstract List<DeployTargetModel> DeploySequence { get; set; }

        /// <summary>
        /// Gets the setting names for this step. The setting names are delimited by pipe character '|'.
        /// </summary>
        [Browsable(false)]
        public abstract string SettingNames { get; }

        private bool isAsync = false;
        public bool IsAsync
        {
            get
            {
                return this.isAsync;
            }
            set
            {
                this.isAsync = value;
            }
        }

        #endregion Properties 

        #region Methods
        public static Step GetFromFile(string filePath)
        {
            Step step = null;

            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                using (Stream input = File.OpenRead(filePath))
                {
                    input.Position = 0;
                    step = bf.Deserialize(input) as Step;
                }
            }
            catch (System.Runtime.Serialization.SerializationException)
            {
                // The serialization may be still processing
                step = null;
            }

            return step;
        }

        /// <summary>
        /// Execute this step.
        /// </summary>
        public virtual void Execute(){
            PreExecute();
            ExecuteMain();
            PostExecute();
        }

        protected virtual void PreExecute()
        {
            string logPath = Properties.Settings.Default.ResultLogPath.Trim();
            try
            {
                if (!string.IsNullOrEmpty(logPath) && File.Exists(logPath))
                {
                    File.Delete(logPath);
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.CentralProcess(ex);
            }
        }

        protected virtual void ExecuteMain()
        {
            // Customized logics
        }

        protected virtual void PostExecute()
        {
            string logPath = Properties.Settings.Default.ResultLogPath.Trim();

            if (!string.IsNullOrEmpty(logPath))
            {
                ////FileAttributes attr = File.GetAttributes(logPath);
                ////if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
                if(IsFilePath(logPath))
                {
                    // Only serialize when the logPath is a filename (not a directory name)

                    Impersonator impersonateor = new Impersonator(
                        Properties.Settings.Default.DomainUserName,
                        Properties.Settings.Default.Domain,
                        Properties.Settings.Default.DomainPassword);
                    try
                    {
                        // There was an error reflecting type 'RulePerf.Model.RestartMachinesStep'.
                        //XmlSerializer xmlSerializer = new XmlSerializer(this.GetType());
                        //using (XmlWriter writer = XmlWriter.Create(logPath))
                        //{
                        //    xmlSerializer.Serialize(writer, this);
                        //}
                        BinaryFormatter bf = new BinaryFormatter();
                        using (Stream output = File.OpenWrite(logPath))
                        {
                            bf.Serialize(output, this);
                            output.Flush();
                            output.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.CentralProcess(ex);
                    }
                    finally
                    {
                        try
                        {
                            impersonateor.Undo();
                        }
                        finally { }
                    }
                }
            }
        }

        private bool IsFilePath(string path)
        {
            try
            {
                if (Directory.Exists(path)) return false;
                if (File.Exists(path)) return true;
                if (path.EndsWith("\\")) return false;

                return true;
            }
            catch
            {
                return true;
            }
        }
        #endregion Methods
    }

    /// <summary>
    /// A container who includes the detailed information for a step
    /// </summary>
    /// 
    [Serializable()]
    public class StepResultDetail
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StepResultDetail" /> class.
        /// </summary>
        /// <param name="message">Detailed message for the step running result</param>
        public StepResultDetail(string message)
        {
            this.Message = message;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StepResultDetail" /> class.
        /// </summary>
        /// <param name="message">Detailed message for the step running result</param>
        /// <param name="ex">Exception that has been thrown out during step running</param>
        public StepResultDetail(string message, Exception ex)
        {
            this.Message = message;
            this.Exceptions = new List<Exception>() { ex };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StepResultDetail" /> class.
        /// </summary>
        /// <param name="message">Detailed message for the step running result</param>
        /// <param name="exceptions">Exceptions that has been thrown out during step running</param>
        public StepResultDetail(string message, List<Exception> exceptions)
        {
            this.Message = message;
            this.Exceptions = exceptions;
        }

        /// <summary>
        /// Gets or sets the message for the step running result
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the exceptions that has been thrown out during step running
        /// </summary>
        public List<Exception> Exceptions { get; set; }
    }
}
