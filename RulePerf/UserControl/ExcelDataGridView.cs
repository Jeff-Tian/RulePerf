// -----------------------------------------------------------------------
// <copyright file="ExcelDataGridView.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf.UserControl
{
    using System;
    using System.Windows.Forms;

    /// <summary>
    /// A customized DataGridView. To support tab key and enter key.
    /// </summary>
    public class ExcelDataGridView : DataGridView
    {
        /// <summary>
        /// Processes keys used for navigating in the <see cref="T:System.Windows.Forms.DataGridView" />.
        /// </summary>
        /// <param name="e">Contains information about the key that was pressed.</param>
        /// <returns>
        /// true if the key was processed; otherwise, false.
        /// </returns>
        protected override bool ProcessDataGridViewKey(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Tab:
                    if (this.EditingControl != null)
                    {
                        if (this.EditingControl is TextBox)
                        {/*
                            TextBox txt = this.EditingControl as TextBox;
                            int tmp = txt.SelectionStart;
                            txt.Text = txt.Text.Insert(txt.SelectionStart, "\t");
                            txt.SelectionStart = tmp + "\t".Length;*/
                            return true;
                        }
                    }
                    return false;
                case Keys.Enter:
                    if (this.EditingControl != null)
                    {
                        if (this.EditingControl is TextBox)
                        {
                            TextBox txt = this.EditingControl as TextBox;
                            int tmp = txt.SelectionStart;
                            txt.Text = txt.Text.Insert(txt.SelectionStart, Environment.NewLine);
                            txt.SelectionStart = tmp + Environment.NewLine.Length;
                            return true;
                        }
                    }
                    return false;
                case Keys.Up:
                    if (this.EditingControl != null)
                    {
                        if (this.EditingControl is TextBox)
                        {
                            return true;
                        }
                    }
                    return false;
                case Keys.Down:
                    if (this.EditingControl != null)
                    {
                        if (this.EditingControl is TextBox)
                        {
                            return true;
                        }
                    }
                    return false;
            }
            return base.ProcessDataGridViewKey(e);
        }
    }
}
