// -----------------------------------------------------------------------
// <copyright file="OneBoxServerAssignmentModel.cs" company="Microsoft">
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
    /// TODO: Update summary.
    /// </summary>
    public class OneBoxServerAssignmentModel : ServerAssignmentModel
    {
        private string oneBoxServer = string.Empty;

        private OneBoxServerAssignmentModel()
        {

        }

        #region Properties
        public override string[] CpRkFrontEndMachines
        {
            get { return new string[] { this.GetOneBoxServer() }; }
        }

        public override string CpWebStoreConfigPrimaryMachine
        {
            get { return this.GetOneBoxServer(); }
        }

        public override string[] CpRkDatabaseServers
        {
            get { return new string[] { this.GetOneBoxServer() }; }
        }

        public override string[] CpRkRiskConfigServers
        {
            get { return new string[] { this.GetOneBoxServer() }; }
        }
        #endregion Properties

        #region Methods
        public static ServerAssignmentModel GetInstance()
        {
            return new OneBoxServerAssignmentModel();
        }

        private string GetOneBoxServer()
        {
            if (string.IsNullOrEmpty(this.oneBoxServer))
            {
                string[] parts = Properties.Settings.Default.Environment.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1 && parts[0].Equals("OneBox", StringComparison.OrdinalIgnoreCase))
                {
                    this.oneBoxServer = parts[1];
                }
                else
                {
                    this.oneBoxServer = ".";
                }
            }

            return this.oneBoxServer;
        }
        #endregion Methods
    }
}
