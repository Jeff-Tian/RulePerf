// -----------------------------------------------------------------------
// <copyright file="DeployTargetModel.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// The model holds the deployment information
    /// </summary>
    /// 
    [Serializable()]
    public class DeployTargetModel
    {
        public string Server { get; set; }
        public string Path { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Domain { get; set; }
        public string AvailableTransferPath { get; set; }
        public TimeSpan Timeout { get; set; }
        public string UNCPath
        {
            get
            {
                if (!string.IsNullOrEmpty(this.Path))
                {
                    return string.Format(@"\\{0}\{1}", this.Server, this.Path.Replace(":", "$"));
                }
                else
                {
                    return string.Format(@"\\{0}\{1}", this.Server, this.Path);
                }
            }
        }
    }
}
