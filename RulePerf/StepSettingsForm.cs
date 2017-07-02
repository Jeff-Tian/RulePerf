using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
using Microsoft.Scs.Test.RiskTools.RulePerf.Model;
using Microsoft.Scs.Test.RiskTools.RulePerf.UserControl;
using System.Xml.Serialization;
using System.Xml;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Scs.Test.RiskTools.RulePerf
{
    public partial class StepSettingsForm : Form
    {
        private Step step;
        public Step Step { get { return step; } }

        private List<SettingEntityModel> settings;
        
        public StepSettingsForm(Step step = null)
        {
            InitializeComponent();

            this.step = step;
        }

        /// <summary>
        /// Loads the settings for the specified step.
        /// </summary>
        public void LoadSettings()
        {
            if (this.step != null)
            {
                string[] settingNames = this.step.SettingNames.Split(new char[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries);
                settings = SettingEntityModel.Get(settingNames);
            }
            else
            {
                settings = SettingEntityModel.GetAllSettings();
            }
        }

        public void LoadSettingsFrom(List<SettingEntityModel> settings)
        {
            if (this.step != null)
            {
                this.settings = new List<SettingEntityModel>();
                string[] settingNames = this.step.SettingNames.Split(new char[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (string settingName in settingNames)
                {
                    var models = from setting in settings
                                               where setting.SettingName.Equals(settingName)
                                               select setting;
                    SettingEntityModel model = models.FirstOrDefault();
                    if (model != null)
                    {
                        this.settings.Add(model);
                        model.Update();
                    }
                }
            }
            else
            {
                this.settings = settings;
                foreach (SettingEntityModel model in this.settings)
                {
                    model.Update();
                }
            }
        }

        /// <summary>
        /// Refreshes the settings binding.
        /// </summary>
        public void RefreshSettingsBinding()
        {
            this.dgvSettings.DataSource = this.settings;
            this.dgvSettings.Refresh();
            this.dgvSettings.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            this.dgvSettings.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
        }

        /// <summary>
        /// Handles the KeyDown event of the dgvSettings control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="KeyEventArgs" /> instance containing the event data.</param>
        private void dgvSettings_KeyDown(object sender, KeyEventArgs e)
        {/*
            DataGridView dgv = sender as DataGridView;
            if (dgv.SelectedCells[0].OwningColumn.HeaderText.Equals("SettingValue", System.StringComparison.InvariantCultureIgnoreCase))
            {
                e.Handled = true;
                if (e.KeyData == Keys.Enter)
                {
                    e.Handled = true;
                }
            }*/
        }

        /// <summary>
        /// Handles the EditingControlShowing event of the dgvSettings control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataGridViewEditingControlShowingEventArgs" /> instance containing the event data.</param>
        private void dgvSettings_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is TextBox)
            {
                TextBox txt = (TextBox)e.Control;
                txt.AcceptsReturn = true;
                txt.AcceptsTab = true;

                DataGridView dgv = sender as DataGridView;
                string settingName = dgv.Rows[dgv.SelectedCells[0].RowIndex].Cells[settingNameDataGridViewTextBoxColumn.Index].Value.ToString();
                if (settingName.Equals("DomainPassword", StringComparison.OrdinalIgnoreCase))
                {
                    txt.UseSystemPasswordChar = true;
                    txt.PasswordChar = '*';
                }
            }
        }

        /// <summary>
        /// Handles the CellEndEdit event of the dgvSettings control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataGridViewCellEventArgs" /> instance containing the event data.</param>
        private void dgvSettings_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            ExcelDataGridView edgv = sender as ExcelDataGridView;
            SettingEntityModel model = edgv.Rows[e.RowIndex].DataBoundItem as SettingEntityModel;
            if (model != null)
            {
                model.Update();
            }
        }

        /// <summary>
        /// Handles the CellParsing event of the dgvSettings control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataGridViewCellParsingEventArgs" /> instance containing the event data.</param>
        private void dgvSettings_CellParsing(object sender, DataGridViewCellParsingEventArgs e)
        {
            
        }

        /// <summary>
        /// Handles the CellPainting event of the dgvSettings control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataGridViewCellPaintingEventArgs" /> instance containing the event data.</param>
        private void dgvSettings_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            //if (e.ColumnIndex == settingValueDataGridViewTextBoxColumn.Index)
            //{
            //    DataGridView dgv = sender as DataGridView;
            //    if (e.RowIndex >= 0)
            //    {
            //        string settingName = dgv.Rows[e.RowIndex].Cells[settingNameDataGridViewTextBoxColumn.Index].Value.ToString();

            //        if (settingName.Equals("DomainPassword", StringComparison.OrdinalIgnoreCase))
            //        {
            //            e.CellStyle.ForeColor = e.CellStyle.BackColor;
            //            e.CellStyle.SelectionForeColor = e.CellStyle.SelectionBackColor;
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void btnSave_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
            this.LoadSettings();
            RefreshSettingsBinding();
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Reset();
            this.LoadSettings();
            RefreshSettingsBinding();
        }

        /// <summary>
        /// Handles the Load event of the StepSettingsForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void StepSettingsForm_Load(object sender, EventArgs e)
        {
            if (this.step != null)
            {
                this.Text = "Settings for {0}".FormatWith(this.step.Name);

                LoadSettings();

                RefreshSettingsBinding();
            }
            else
            {
                this.Text = "All Settings.";
                LoadSettings();
                RefreshSettingsBinding();
            }
        }

        private void dgvSettings_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == settingValueDataGridViewTextBoxColumn.Index)
            {
                DataGridView dgv = sender as DataGridView;
                string settingName = dgv.Rows[e.RowIndex].Cells[settingNameDataGridViewTextBoxColumn.Index].Value.ToString();
                if (settingName.Equals("DomainPassword", StringComparison.OrdinalIgnoreCase))
                {
                    e.Value = new string('*', e.Value.ToString().Length);
                }
                else if (settingName.Equals("RemoteCommand", StringComparison.OrdinalIgnoreCase))
                {
                    e.Value = Log.EncryptDomainPassword(e.Value.ToString());
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Rule Perf Testing Settings File (*.rpsettings)|*.rpsettings";
            saveDialog.CheckPathExists = true;
            saveDialog.Title = "Choose a file to save the current settings";
            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Very important!!! Save the current values in grid
                this.dgvSettings.EndEdit();

                List<KeyValuePair<string, string>> encryptedSettings = new List<KeyValuePair<string, string>>();
                try
                {
                    // Encrypt sensative messages
                    foreach (SettingEntityModel setting in this.settings)
                    {
                        if (setting.SettingName.Equals("DomainPassword", StringComparison.OrdinalIgnoreCase))
                        {
                            encryptedSettings.Add(new KeyValuePair<string, string>(setting.SettingName, setting.SettingValue));
                            setting.SettingValue = new string('*', setting.SettingValue.Length);
                        }

                        if (setting.SettingName.Equals("RemoteCommand", StringComparison.OrdinalIgnoreCase))
                        {
                            encryptedSettings.Add(new KeyValuePair<string, string>(setting.SettingName, setting.SettingValue));
                            //setting.SettingValue = Regex.Replace(setting.SettingValue, "(?<=/DomainPassword:\"?)[^ ]*(?=\"? *)", "******", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                            setting.SettingValue = Log.EncryptDomainPassword(setting.SettingValue);
                        }
                    }

                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<SettingEntityModel>));
                    using (XmlWriter xmlWriter = XmlWriter.Create(saveDialog.FileName))
                    {
                        xmlSerializer.Serialize(xmlWriter, this.settings);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to save the current settings\r\n" + ex.Message, "Rule Perf Testing Tool Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ExceptionHelper.CentralProcess(new Exception("Unable to save the rule perf testing settings", ex));
                }
                finally
                {
                    // Recover the encrypted messages
                    foreach (KeyValuePair<string, string> pair in encryptedSettings)
                    {
                        SettingEntityModel model = this.settings.Find(m => { return m.SettingName.Equals(pair.Key); });
                        if (model != null)
                        {
                            model.SettingValue = pair.Value;
                        }
                    }
                }
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "Rule Perf Testing Settings File (*.rpsettings)|*.rpsettings";
            openDialog.CheckPathExists = true;
            openDialog.CheckFileExists = true;
            openDialog.Title = "Choose a rule perf testing settings file to open";
            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(this.settings.GetType());
                    using (XmlReader xmlReader = XmlReader.Create(openDialog.FileName))
                    {
                        LoadSettingsFrom((List<SettingEntityModel>)xmlSerializer.Deserialize(xmlReader));
                        xmlReader.Close();
                    }

                    this.RefreshSettingsBinding();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to read the settings file\r\n" + ex.Message, "Rule Perf Testing Tool Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ExceptionHelper.CentralProcess(new Exception("Unable to read the setting file '{0}'.".FormatWith(openDialog.FileName), ex));
                }
            }
        }
    }
}
