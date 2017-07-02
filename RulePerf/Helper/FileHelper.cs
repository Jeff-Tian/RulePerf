// -----------------------------------------------------------------------
// <copyright file="FileHelper.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class FileHelper
    {
        public static bool IsFileLocked(string filePath)
        {
            FileStream fileStream = null;
            try
            {
                fileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (fileStream != null)
                    fileStream.Close();
            }

            //file is not locked
            return false;
        }
    }
}
