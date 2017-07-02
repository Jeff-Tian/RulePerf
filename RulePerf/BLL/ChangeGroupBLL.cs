// -----------------------------------------------------------------------
// <copyright file="ChangeGroupBLL.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf.BLL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using System.IO;

    /// <summary>
    /// Wrapper for change group operations
    /// </summary>
    public class ChangeGroupBLL
    {
        public static string GetFirstAvailabeMergedChangeGroup()
        {
            XDocument xdoc = XDocument.Load(Properties.Settings.Default.ChangeGroupMergeHistoryFilePath);
            var changeGroups = from changeGroup in xdoc.Descendants("ChangeGroup")
                               where changeGroup.Element("IsEnabled").Value.Equals("True", StringComparison.InvariantCultureIgnoreCase)
                               orderby changeGroup.Element("Sequence").Value
                               select changeGroup;
            if (changeGroups.Count() > 0)
            {
                string fileName = "Group-" + changeGroups.ElementAt(0).Element("Name").Value + ".xml";
                return Path.Combine(Path.GetDirectoryName(Properties.Settings.Default.ChangeGroupMergeHistoryFilePath), "MergeResult", fileName);
            }
            else
            {
                return "";
            }
        }
    }
}
