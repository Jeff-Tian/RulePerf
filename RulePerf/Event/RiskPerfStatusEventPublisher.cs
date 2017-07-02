using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Event
{
    public class RiskPerfStatusEventPublisher
    {
        private static RiskPerfStatusEventPublisher _instance;

        protected RiskPerfStatusEventPublisher()
        {
        }

        public static RiskPerfStatusEventPublisher Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }
                else
                {
                    _instance = new RiskPerfStatusEventPublisher();
                    return _instance;
                }
            }
        }
        public event EventHandler<RiskPerfStatusEvent> riskPerfEvents;

        public void PublishEvent(RiskPerfStatus status, string taskName = null, string logFile = null)
        {
            HandleEvent(new RiskPerfStatusEvent(status, taskName, logFile));
        }

        protected void HandleEvent(RiskPerfStatusEvent e)
        {
            try
            {
                EventHandler<RiskPerfStatusEvent> eventHandler = riskPerfEvents;
                if (eventHandler != null)
                {
                    eventHandler(this, e);
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.CentralProcess(ex);
            }
        }
    }
}
