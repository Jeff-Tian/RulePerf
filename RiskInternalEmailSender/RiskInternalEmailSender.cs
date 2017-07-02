using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Net;

namespace Microsoft.Scs.Test.RiskTools
{
    public class RiskEmailSender
    {
        public string userName;
        public string passWord;
        public MailAddress from;
        public MailAddressCollection to;
        public MailAddressCollection cc;

        public RiskEmailSender(string from, string[] toList, string[] ccList)
            : this(null, null, from, toList, ccList)
        {
        }

        public RiskEmailSender(string userName, string passWord, string from, string[] toList, string[] ccList)
        {
            this.userName = userName;
            this.passWord = passWord;
            this.from = RiskEmailSenderUtility.GetFullEmailAddress(from);
            this.to = RiskEmailSenderUtility.GetEmailAddressCollection(toList);
            this.cc = RiskEmailSenderUtility.GetEmailAddressCollection(ccList);
        }

        /// <summary>
        /// When email failed to send, it will throw exceptions. The caller should catch it.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="attachMentPath"></param>
        /// <param name="timeOutMs"></param>
        public void SendMail(string subject, string body, string attachMentPath, int timeOutMs = 100000)
        {
            MailMessage mail;
            SmtpClient sender;

            PrepareSmtpClient(subject, body, attachMentPath, timeOutMs, out mail, out sender);
            sender.Send(mail);
        }

        public void SendMailAsync(string subject, string body, string attachMentPath, SendCompletedEventHandler eventHandler, int timeOutMs = 100000)
        {
            MailMessage mail;
            SmtpClient sender;

            PrepareSmtpClient(subject, body, attachMentPath, timeOutMs, out mail, out sender);
            sender.SendAsync(mail, "send Mail");
            sender.SendCompleted += eventHandler;
        }

        private void PrepareSmtpClient(string subject, string body, string attachMentPath, int timeOutMs, out MailMessage mail, out SmtpClient sender)
        {
            mail = new MailMessage()
            {
                From = from,
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = body,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true
            };

            mail.To.AddMailAddresses(this.to);
            mail.CC.AddMailAddresses(this.cc);

            if (attachMentPath != null)
            {
                mail.Attachments.Add(new Attachment(attachMentPath));
            }

            sender = new SmtpClient
            {
                Host = RiskEmailSenderUtility.SMTPHost,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = true,
                Timeout = timeOutMs,
            };

            if (this.userName != null && this.passWord != null)
            {
                sender.UseDefaultCredentials = false;
                sender.Credentials = new NetworkCredential(this.userName, this.passWord);
            }
        }
    }
}
