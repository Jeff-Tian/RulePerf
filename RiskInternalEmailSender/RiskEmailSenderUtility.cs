using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;

namespace Microsoft.Scs.Test.RiskTools
{
    internal static class RiskEmailSenderUtility
    {
        public const string SMTPHost = "smtphost.redmond.corp.microsoft.com";

        public static MailAddress GetFullEmailAddress(string email)
        {
            if (email == null)
            {
                throw new ArgumentNullException("Email address can't be null");
            }

            string result = email;

            if (!result.Contains('@'))
            {
                result = string.Concat(email, "@microsoft.com");
            }

            return new MailAddress(result);
        }

        public static MailAddressCollection GetEmailAddressCollection(string[] emails)
        {
            MailAddressCollection result = new MailAddressCollection();

            if (emails == null || emails.Count() == 0)
            {
                return result;
            }

            foreach (var email in emails)
            {
                result.Add(GetFullEmailAddress(email));
            }

            return result;
        }

        public static void AddMailAddresses(this MailAddressCollection addressCollection, MailAddressCollection addresses)
        {
            foreach (MailAddress address in addresses)
            {
                addressCollection.Add(address);
            }
        }
    }
}
