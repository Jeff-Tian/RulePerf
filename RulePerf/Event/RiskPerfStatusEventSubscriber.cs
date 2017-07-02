using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Event
{
    public abstract class RiskPerfStatusEventSubscriber
    {
        public void Subscribe(RiskPerfStatusEventPublisher publisher)
        {
            publisher.riskPerfEvents += HandleEvent;
        }

        protected virtual void HandleEvent(object sender, RiskPerfStatusEvent e)
        {
        }
    }

    class EmailSubscriber : RiskPerfStatusEventSubscriber
    {
        private RiskEmailSender emailSender;

        public EmailSubscriber(string[] toList, string[] ccList = null)
            : this(null, null, toList, ccList)
        {
        }

        public EmailSubscriber(string userName, string passWord, string[] toList, string[] ccList)
        {
            string from = userName ?? Environment.UserName;
            this.emailSender = new RiskEmailSender(userName, passWord, from, toList, ccList);
        }

        protected override void HandleEvent(object sender, RiskPerfStatusEvent e)
        {
            string title = GetEmailTitle();
            string message = GetEmailMessage(e);

            this.emailSender.SendMail(title, message, e.logFile);
        }

        private string GetEmailMessage(RiskPerfStatusEvent e)
        {
            string message = null;

            switch (e.status)
            {
                case RiskPerfStatus.Started:
                    message = "Rule Performance test is starting now!";
                    break;
                case RiskPerfStatus.Blocked:
                    message = string.Format("Rule Performance test is blocked, task is {0}, please see details in attachment", e.taskName);
                    break;
                case RiskPerfStatus.Stopped:
                    message = "Rule Performance test is done!";
                    break;
            }

            return message;
        }

        private string GetEmailTitle()
        {
            return "Rule Performance Test Report";
        }
    }
}
