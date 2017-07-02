using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Event
{
    public class RiskPerfStatusEvent : EventArgs
    {
        public RiskPerfStatus status;
        public string taskName;
        public string logFile;

        public RiskPerfStatusEvent(RiskPerfStatus status, string taskName = null, string logFile = null)
        {
            this.status = status;
            this.taskName = taskName;
            this.logFile = logFile;
        }
    }

    public enum RiskPerfStatus
    {
        Started,
        Blocked,
        Stopped
    }
}
