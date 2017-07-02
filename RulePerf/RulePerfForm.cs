// -----------------------------------------------------------------------
// <copyright file="RulePerfForm.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using System.Windows.Forms;
    using System.Xml.Linq;
    using Microsoft.Scs.Test.RiskTools.RulePerf.BLL;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Model;
    using System.Xml.Serialization;
    using System.Xml;

    /// <summary>
    /// The GUI form of RulePerf.exe
    /// </summary>
    public partial class RulePerfForm : Form
    {
        /// <summary>
        /// The steps list
        /// </summary>
        private List<Step> steps = new List<Step>();

        /// <summary>
        /// The preparation steps for rule perf testing
        /// </summary>
        private List<Step> prepareSteps = new List<Step>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RulePerfForm" /> class.
        /// </summary>
        public RulePerfForm()
        {
            this.InitializeComponent();
        }

        #region Menu Handlers
        /// <summary>
        /// Handles the Click event of the exitToolStrip control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void exitToolStrip_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Save current step status
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void toolStripMenuItemSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Rule Perf Testing Steps File (*.steps)|*.steps";
            saveDialog.CheckPathExists = true;
            saveDialog.Title = "Choose a file to save the current step status";
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                // Very important!!! Save the current steps in grid
                this.dgvSteps.EndEdit();
                this.dgvPrepareSteps.EndEdit();

                try
                {
                    #region Binary serialize
                    // BinaryFormatter has better performance, but it's hard to communicated with other applications

                    BinaryFormatter bf = new BinaryFormatter();
                    using (Stream output = File.OpenWrite(saveDialog.FileName))
                    {
                        bf.Serialize(output, this.steps);

                        bf.Serialize(output, this.prepareSteps);

                        output.Flush();
                        output.Close();
                    }

                    #endregion Binary serialize

                    #region The XmlSerializer don't know how to handle interfaces
                    /*
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<IStep>));
                    using (XmlWriter writer = XmlWriter.Create(saveDialog.FileName))
                    {
                        xmlSerializer.Serialize(writer, this.steps);
                    }*/
                    #endregion The XmlSerializer don't know how to handle interfaces
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to save the Rule Perf Testing Steps File\r\n" + ex.Message, "Rule Perf Testing Tool Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ExceptionHelper.CentralProcess(new Exception("Unable to save the Rule Perf Testing Steps File", ex));
                }
            }
        }

        /// <summary>
        /// Open step status
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void toolStripMenuItemOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "Rule Perf Testing Steps File (*.steps)|*.steps";
            openDialog.CheckPathExists = true;
            openDialog.CheckFileExists = true;
            openDialog.Title = "Choose a file with step statuses to load";
            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    LoadStepsFrom(openDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to read the Rule perf Testing Steps File\r\n" + ex.Message, "Rule Perf Testing Tool Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ExceptionHelper.CentralProcess(new Exception("Unable to read the rule perf testing steps file '{0}'".FormatWith(openDialog.FileName), ex));
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the optionsToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void commonSettingsCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Step step = new CommonStep() { Checked = false, Status = StepStatusEnum.NotExecutable };
            StepSettingsForm stepSettingsForm = new StepSettingsForm(step);
            stepSettingsForm.Show(this);
        }

        private void allSettingsAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StepSettingsForm stepSettingsForm = new StepSettingsForm();
            stepSettingsForm.ShowDialog(this);
        }

        private void loadStepsAndConfigurationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void currentSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.eXRKToolStripMenuItem.Checked = false;
            this.sHRKToolStripMenuItem.Checked = false;
            this.currentSettingsToolStripMenuItem.Checked = true;
            this.customizedToolStripMenuItem.Checked = false;
            LoadStepsConfigurationAndSettings();
        }

        private void eXRKToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.eXRKToolStripMenuItem.Checked = true;
            this.sHRKToolStripMenuItem.Checked = false;
            this.currentSettingsToolStripMenuItem.Checked = false;
            this.customizedToolStripMenuItem.Checked = false;
            LoadStepsConfigurationAndSettings("bed:exrk");
        }

        private void sHRKToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.eXRKToolStripMenuItem.Checked = false;
            this.sHRKToolStripMenuItem.Checked = true;
            this.currentSettingsToolStripMenuItem.Checked = false;
            this.customizedToolStripMenuItem.Checked = false;
            LoadStepsConfigurationAndSettings("bed:shrk");
        }
        #endregion Menu Handlers

        /// <summary>
        /// Handles the Load event of the RulePerf control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void RulePerf_Load(object sender, EventArgs e)
        {
            //this.steps.Add(new TestStep()
            //{
            //    Checked = true
            //});
            //this.prepareSteps.Add(new TestStep()
            //{
            //    Checked = true
            //});
            //goto RefreshStepsLabel;
            #region Rule Perf Testing Steps
            this.steps.Add(new DownloadChangeGroupStep()
            {
                Checked = true
            });

            this.steps.Add(new PrepareTransactionDataFileStep()
            {
                Checked=true
            });

            this.steps.Add(new MergeDataFilesStep()
            {
                DeploySequence = new List<DeployTargetModel>(
                    new DeployTargetModel[]{
                        new DeployTargetModel(){
                            Server = "bedrd",
                            Path = @"C:\RulePerf",
                            UserName="Administrator",
                            Password="#Bugsfor$",
                            Domain = ".",
                            AvailableTransferPath = @"\\bedtransfer\transfer\v-jetian\MergeDataFilesStep.rslt",
                            Timeout = TimeSpan.FromMinutes(30)
                        },
                        new DeployTargetModel(){
                            Server = "EXRKBIWEBSPK01",
                            Path = @"C:\RulePerf",
                            UserName="Administrator",
                            Password="#Bugsfor$",
                            Domain = "EXRK",
                            AvailableTransferPath = @"\\bedtransfer\transfer\v-jetian\MergeDataFilesStepInner.rslt",
                            Timeout = TimeSpan.FromMinutes(30)
                        }
                    }
                ),
                Checked=true
            });

            this.steps.Add(new DownloadRiMEConfigStep()
            {
                Checked = true
            });
            this.steps.Add(new SyncProductSettingsStep()
            {
                DeploySequence = new List<DeployTargetModel>(
                    new DeployTargetModel[]{
                        new DeployTargetModel(){
                            Server="bedrd",
                            Path=@"C:\RulePerf",
                            UserName="Administrator",
                            Password="#Bugsfor$",
                            Domain = ".",
                            AvailableTransferPath=@"\\bedtransfer\transfer\v-jetian\SyncProductSettingsStep.rslt",
                            Timeout=TimeSpan.FromMinutes(10)
                        },
                        new DeployTargetModel(){
                            Server = "EXRKRKUTLBEP01",
                            Path = @"C:\RulePerf",
                            UserName = "Administrator",
                            Password = "#Bugsfor$",
                            Domain = "EXRK",
                            AvailableTransferPath = @"\\bedtransfer\transfer\v-jetian\SyncProductSettingsStepInner.rslt",
                            Timeout = TimeSpan.FromMinutes(10)
                        }
                    }
                ),
                Checked = true
            });
            this.steps.Add(new SetupGlobalSettingStep()
                {
                    DeploySequence = new List<DeployTargetModel>(
                        new DeployTargetModel[]{
                            new DeployTargetModel(){
                                Server = "bedrd",
                                Path = @"C:\RulePerf",
                                UserName = "Administrator",
                                Password = "#Bugsfor$",
                                Domain = ".",
                                AvailableTransferPath = @"\\bedtransfer\transfer\v-jetian\SteupGlobalSettingStep.rslt",
                                Timeout= TimeSpan.FromMinutes(10)
                            }
                        }
                    ),
                    Checked=false
                }
            );
            this.steps.Add(new CommandsExecutingStep()
            {
                DeploySequence = new List<DeployTargetModel>(
                    new DeployTargetModel[]{
                        new DeployTargetModel(){
                            Server = "bedrd",
                            Path = @"C:\RulePerf",
                            UserName="Administrator",
                            Password="#Bugsfor$",
                            Domain = ".",
                            AvailableTransferPath = @"\\bedtransfer\transfer\v-jetian\CommandsExecutingStep.rslt",
                            Timeout = TimeSpan.FromMinutes(30)
                        }
                    }
                ),
                Checked = true,
                Name = "Restart services",
                Description = "Restart services"
            });
            this.steps.Add(new RunReplayToolForBaseLineStep()
            {
                DeploySequence = new List<DeployTargetModel>(
                    new DeployTargetModel[]{
                        new DeployTargetModel(){
                            Server = "bedrd",
                            Path = @"C:\RulePerf",
                            UserName="Administrator",
                            Password="#Bugsfor$",
                            Domain = ".",
                            AvailableTransferPath = @"\\bedtransfer\transfer\v-jetian\RunReplayToolForBaseLineStep.rslt",
                            Timeout = TimeSpan.FromMinutes(60)
                        },
                        new DeployTargetModel(){
                            Server = "EXRKBIWEBSPK01",
                            Path = @"C:\RulePerf",
                            UserName="Administrator",
                            Password="#Bugsfor$",
                            Domain = "EXRK",
                            AvailableTransferPath = @"\\bedtransfer\transfer\v-jetian\RunReplayToolForBaseLineStepInner.rslt",
                            Timeout = TimeSpan.FromMinutes(60)
                        }
                    }
                ),
                Checked = true
            });
            this.steps.Add(new ApplyChangeGroupStep()
            {
                DeploySequence = new List<DeployTargetModel>(
                    new DeployTargetModel[]{
                        new DeployTargetModel(){
                            Server = "bedrd",
                            Path = @"C:\RulePerf",
                            UserName="Administrator",
                            Password="#Bugsfor$",
                            Domain = ".",
                            AvailableTransferPath = @"\\bedtransfer\transfer\v-jetian\ApplyChangeGroupStep.rslt",
                            Timeout = TimeSpan.FromMinutes(10)
                        },
                        new DeployTargetModel(){
                            Server = "EXRKRKUTLBEP01",
                            Path = @"C:\RulePerf",
                            UserName = "Administrator",
                            Password = "#Bugsfor$",
                            Domain = "EXRK",
                            AvailableTransferPath = @"\\bedtransfer\transfer\v-jetian\ApplyChangeGroupStepInner.rslt",
                            Timeout = TimeSpan.FromMinutes(10)
                        }
                    }
                ),
                Checked = true
            });
            this.steps.Add(new CommandsExecutingStep()
            {
                DeploySequence = new List<DeployTargetModel>(
                    new DeployTargetModel[]{
                        new DeployTargetModel(){
                            Server = "bedrd",
                            Path = @"C:\RulePerf",
                            UserName="Administrator",
                            Password="#Bugsfor$",
                            Domain = ".",
                            AvailableTransferPath = @"\\bedtransfer\transfer\v-jetian\CommandsExecutingStep.rslt",
                            Timeout = TimeSpan.FromMinutes(30)
                        }
                    }
                ),
                Checked = true,
                Name = "Restart services",
                Description = "Restart services"
            });
            this.steps.Add(new RunReplayToolForChangedStep()
            {
                DeploySequence = new List<DeployTargetModel>(
                    new DeployTargetModel[]{
                        new DeployTargetModel(){
                            Server = "bedrd",
                            Path = @"C:\RulePerf",
                            UserName="Administrator",
                            Password="#Bugsfor$",
                            Domain = ".",
                            AvailableTransferPath = @"\\bedtransfer\transfer\v-jetian\RunReplayToolForChangedStep.rslt",
                            Timeout = TimeSpan.FromMinutes(60)
                        },
                        new DeployTargetModel(){
                            Server = "EXRKBIWEBSPK01",
                            Path = @"C:\RulePerf",
                            UserName="Administrator",
                            Password="#Bugsfor$",
                            Domain = "EXRK",
                            AvailableTransferPath = @"\\bedtransfer\transfer\v-jetian\RunReplayToolForChangedStepInner.rslt",
                            Timeout = TimeSpan.FromMinutes(60)
                        }
                    }
                ),
                Checked = true
            });
            #endregion Rule Perf Testing Steps

            #region Rule perf preparation steps
            this.prepareSteps.Add(new ExportDataFromSqlServerStep(){
                //DeploySequence = new List<DeployTargetModel>(
                //    new DeployTargetModel[]{
                //        new DeployTargetModel(){
                //            Server = "co1tscorp.phx.gbl",
                //            Path=@"E:\Users\v-jetian\RulePerf",
                //            UserName="v-jetian",
                //            Password="Love532154Mar",
                //            Domain="PHX",
                //            AvailableTransferPath=@"\\transfer\transfer\v-jetian\ExportDataFromSqlServerStep.rslt",
                //            Timeout=TimeSpan.FromHours(10)
                //        }
                //    }
                //),
                IsAsync = false,
                Checked = true
            });

            this.prepareSteps.Add(new CopyDataFilesStep()
            {
                Checked = true
            });

            this.prepareSteps.Add(new ImportProductionDataStep()
            {
                DeploySequence = new List<DeployTargetModel>(
                    new DeployTargetModel[]{
                        new DeployTargetModel(){
                            Server="bedrd",
                            Path=@"C:\RulePerf",
                            UserName="Administrator",
                            Password="#Bugsfor$",
                            Domain="SHRK",
                            AvailableTransferPath = @"\\bedtransfer\transfer\v-jetian\ImportProductionDataStep.rslt",
                            Timeout=TimeSpan.FromHours(10)
                        },
                        new DeployTargetModel(){
                            Server = "SHRKBIWEBSPK01",
                            Path = @"C:\RulePerf",
                            UserName="Administrator",
                            Password="#Bugsfor$",
                            Domain = "SHRK",
                            AvailableTransferPath = @"\\bedtransfer\transfer\v-jetian\ImportProductionDataStepInner.rslt",
                            Timeout = TimeSpan.FromHours(10)
                        }
                    }
                    ),
                    Checked=true
            });
            #endregion Rule perf preparation steps

        RefreshStepsLabel:
            RefreshSteps(DataGridViewAutoSizeColumnsMode.DisplayedCells);

            this.btnRun.Enabled = true;
            this.btnRunPrepareSteps.Enabled = true;
        }

        /// <summary>
        /// Refreshes the steps.
        /// </summary>
        /// <param name="dataGridViewAutoSizeColumnsMode">The data grid view auto size columns mode.</param>
        private void RefreshSteps(DataGridViewAutoSizeColumnsMode dataGridViewAutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells)
        {
            this.dgvSteps.DataSource = this.steps;
            this.dgvSteps.Refresh();
            this.dgvSteps.AutoResizeColumns(dataGridViewAutoSizeColumnsMode);

            this.dgvPrepareSteps.DataSource = this.prepareSteps;
            this.dgvPrepareSteps.Refresh();
            this.dgvPrepareSteps.AutoResizeColumns(dataGridViewAutoSizeColumnsMode);
        }
        
        /// <summary>
        /// Handles the Click event of the btnRun control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void btnRun_Click(object sender, EventArgs e)
        {
            this.toolStripStatusLabel.Text = "Running steps...";

            Button btn = (Button)sender;
            btn.Enabled = false;
            // Only one scenario is supported at the same time. If rule perf testing steps are running, then
            // the prepare steps can't run. And vice versa.
            this.btnRunPrepareSteps.Enabled = false;

            tmrMonitor.Start();

            StepsProcessor.AsyncProcessSteps(
                GetStepsToBeExecuted(this.dgvSteps),
                new RunWorkerCompletedEventHandler((obj, ea) =>
            {
                tmrMonitor.Stop();
                RefreshSteps(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                this.toolStripStatusLabel.Text = "Steps completed.";
                this.btnRun.Enabled = true;
                this.btnRunPrepareSteps.Enabled = true;
            }));
        }

        /// <summary>
        /// Handles the Click event of the btnRunPrepareSteps control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void btnRunPrepareSteps_Click(object sender, EventArgs e)
        {
            this.toolStripStatusLabel.Text = "Running preparation steps...";

            Button btn = (Button)sender;
            btn.Enabled = false;
            // Only one scenario is supported at the same time. If rule perf preparation steps are running, then
            // the rule perf testing steps can't run. And vice versa.
            this.btnRun.Enabled = false;

            tmrMonitor.Start();

            StepsProcessor.AsyncProcessSteps(
                GetStepsToBeExecuted(this.dgvPrepareSteps),
                new RunWorkerCompletedEventHandler((obj, ea) =>
                {
                    tmrMonitor.Stop();
                    RefreshSteps(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                    this.toolStripStatusLabel.Text = "Preparation steps completed.";
                    this.btnRun.Enabled = true;
                    this.btnRunPrepareSteps.Enabled = true;
                }));
        }

        /// <summary>
        /// Handles the Click event of the btnEditSteps control. Edit the steps
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void btnEditSteps_Click(object sender, EventArgs e)
        {
            ObjectEditor stepsEditor = new ObjectEditor();
            stepsEditor.PropertyGrid.SelectedObject = new { Steps = this.steps };
            stepsEditor.Show(this);
        }

        private void btnEditPrepareSteps_Click(object sender, EventArgs e)
        {
            ObjectEditor stepsEditor = new ObjectEditor();
            stepsEditor.PropertyGrid.SelectedObject = new { Steps = this.prepareSteps };
            stepsEditor.Show(this);
        }

        /// <summary>
        /// Gets the steps to be executed.
        /// </summary>
        /// <returns>The steps list</returns>
        private List<Step> GetStepsToBeExecuted(DataGridView dgv)
        {
            List<Step> steps = new List<Step>();
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                if (dgv.Rows[i].Visible)
                {
                    Step step = dgv.Rows[i].DataBoundItem as Step;
                    if (step.Checked)
                    {
                        steps.Add(step);
                    }
                }
            }

            return steps;
        }

        /// <summary>
        /// Handles the Tick event of the tmrMonitor control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void tmrMonitor_Tick(object sender, EventArgs e)
        {
            RefreshSteps();
        }

        /// <summary>
        /// Handles the CellContentClick event of the dgvSteps control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataGridViewCellEventArgs" /> instance containing the event data.</param>
        private void dgvSteps_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;

            if (dgv.Columns[e.ColumnIndex].HeaderText.Equals("Result Detail", StringComparison.InvariantCultureIgnoreCase) 
                && dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().Equals("View Details", StringComparison.InvariantCultureIgnoreCase))
            {
                Step step = dgv.Rows[e.RowIndex].DataBoundItem as Step;
                StepDetailViewer stepDetailViewer = new StepDetailViewer();
                stepDetailViewer.Step = step;
                stepDetailViewer.Show(this);
            }
            else if (dgv.Columns[e.ColumnIndex].HeaderText.Equals("Configuration", StringComparison.InvariantCultureIgnoreCase) 
                && dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().Equals("Change Configuration", StringComparison.InvariantCultureIgnoreCase))
            {
                Step step = dgv.Rows[e.RowIndex].DataBoundItem as Step;
                StepSettingsForm stepSettingsForm = new StepSettingsForm(step);
                stepSettingsForm.Show(this);
            }
            else if (dgv.Columns[e.ColumnIndex].HeaderText.Equals("Deploy Sequence", StringComparison.InvariantCultureIgnoreCase)
               && string.Equals(dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString(), "Edit", StringComparison.InvariantCultureIgnoreCase))
            {
                Step step = dgv.Rows[e.RowIndex].DataBoundItem as Step;
                ObjectEditor deploySequenceConfiguration = new ObjectEditor();
                deploySequenceConfiguration.PropertyGrid.SelectedObject = new { DeploySequence = step.DeploySequence };
                deploySequenceConfiguration.Show(this);
            }
        }

        /// <summary>
        /// Handles the CellFormatting event of the dgvSteps control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataGridViewCellFormattingEventArgs" /> instance containing the event data.</param>
        private void dgvSteps_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            if (dgv.Columns[e.ColumnIndex].HeaderText.Equals("Status"))
            {
                Step step = dgv.Rows[e.RowIndex].DataBoundItem as Step;
                switch (step.Status)
                {
                    case StepStatusEnum.Cancelled:
                        e.CellStyle.ForeColor = Color.DarkGray;
                        break;
                    case StepStatusEnum.Executing:
                        e.CellStyle.ForeColor = Color.Blue;
                        break;
                    case StepStatusEnum.Failed:
                        e.CellStyle.ForeColor = Color.Red;
                        break;
                    case StepStatusEnum.NotExecutable:
                        e.CellStyle.ForeColor = Color.Gray;
                        break;
                    case StepStatusEnum.NotStarted:
                        e.CellStyle.ForeColor = Color.Black;
                        break;

                    case StepStatusEnum.Pass:
                        e.CellStyle.ForeColor = Color.DarkGreen;
                        e.CellStyle.Font = new Font(e.CellStyle.Font, FontStyle.Bold);
                        break;
                    case StepStatusEnum.Warning:
                        e.CellStyle.ForeColor = Color.YellowGreen;
                        break;

                    default:
                        break;
                }
            }
        }

        private void tab_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshSteps(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void LoadStepsConfigurationAndSettings()
        {
            LoadStepsConfigurationAndSettings("");
        }

        private void LoadStepsConfigurationAndSettings(string environment)
        {
            try
            {
                switch (environment.ToLower())
                {
                    case "bed:exrk":
                        try
                        {
                            LoadSettingsFrom(@"\\bedtransfer\transfer\v-jetian\RulePerfTestingSettingsEXRK.rpsettings");
                        }
                        catch
                        {
                            throw;
                        }
                        finally
                        {
                            LoadStepsFrom(@"\\bedtransfer\transfer\v-jetian\RulePerfTestingStepsEXRK.steps");
                        }
                        break;
                    case "bed:shrk":
                        try
                        {
                            LoadSettingsFrom(@"\\bedtransfer\transfer\v-jetian\RulePerfTestingSettingsSHRK.rpsettings");
                        }
                        catch
                        {
                            throw;
                        }
                        finally
                        {
                            LoadStepsFrom(@"\\bedtransfer\transfer\v-jetian\RulePerfTestingStepsSHRK.steps");
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load steps and settings, please check whether the relative files exist. You can check log for more detailed info.\r\nException messages:\r\n{0}".FormatWith(ex.Message),
                    "Rule Perf Testing Tool Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExceptionHelper.CentralProcess(ex);
            }
        }

        private void LoadStepsFrom(string stepsConfigurationFilePath)
        {
            try
            {
                #region Load steps & preparation steps
                BinaryFormatter bf = new BinaryFormatter();
                using (Stream input = File.OpenRead(stepsConfigurationFilePath))
                {
                    this.steps = (List<Step>)bf.Deserialize(input);
                    this.prepareSteps = (List<Step>)bf.Deserialize(input);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                // Make sure the steps are refreshed on the UI no matter the loading is success or not
                this.RefreshSteps();
            }
            #endregion Load steps & preparation steps
        }

        private void LoadSettingsFrom(string settingsFilePath)
        {
            List<SettingEntityModel> settings = new List<SettingEntityModel>();

            XmlSerializer xmlSerializer = new XmlSerializer(settings.GetType());
            using (XmlReader xmlReader = XmlReader.Create(settingsFilePath))
            {
                settings = (List<SettingEntityModel>)xmlSerializer.Deserialize(xmlReader);
                xmlReader.Close();
            }

            foreach (SettingEntityModel model in settings)
            {
                model.Update();
            }
        }
        
    }
}
