using Microsoft.Scs.Test.RiskTools.RulePerf.UserControl;
namespace Microsoft.Scs.Test.RiskTools.RulePerf
{
    partial class StepSettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.dgvSettings = new Microsoft.Scs.Test.RiskTools.RulePerf.UserControl.ExcelDataGridView();
            this.settingNameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.settingValueDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.settingTypeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.settingEntityModelBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSettings)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.settingEntityModelBindingSource)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnCancel);
            this.panel1.Controls.Add(this.btnSave);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 441);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(924, 36);
            this.panel1.TabIndex = 1;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(84, 8);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "&Reset";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(3, 8);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "&Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.dgvSettings);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 24);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(924, 417);
            this.panel2.TabIndex = 2;
            // 
            // dgvSettings
            // 
            this.dgvSettings.AllowUserToAddRows = false;
            this.dgvSettings.AllowUserToDeleteRows = false;
            this.dgvSettings.AllowUserToOrderColumns = true;
            this.dgvSettings.AutoGenerateColumns = false;
            this.dgvSettings.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSettings.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.settingNameDataGridViewTextBoxColumn,
            this.settingValueDataGridViewTextBoxColumn,
            this.settingTypeDataGridViewTextBoxColumn});
            this.dgvSettings.DataSource = this.settingEntityModelBindingSource;
            this.dgvSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvSettings.Location = new System.Drawing.Point(0, 0);
            this.dgvSettings.Name = "dgvSettings";
            this.dgvSettings.Size = new System.Drawing.Size(924, 417);
            this.dgvSettings.TabIndex = 0;
            this.dgvSettings.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvSettings_CellEndEdit);
            this.dgvSettings.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dgvSettings_CellFormatting);
            this.dgvSettings.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.dgvSettings_CellPainting);
            this.dgvSettings.CellParsing += new System.Windows.Forms.DataGridViewCellParsingEventHandler(this.dgvSettings_CellParsing);
            this.dgvSettings.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.dgvSettings_EditingControlShowing);
            this.dgvSettings.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dgvSettings_KeyDown);
            // 
            // settingNameDataGridViewTextBoxColumn
            // 
            this.settingNameDataGridViewTextBoxColumn.DataPropertyName = "SettingName";
            this.settingNameDataGridViewTextBoxColumn.HeaderText = "SettingName";
            this.settingNameDataGridViewTextBoxColumn.Name = "settingNameDataGridViewTextBoxColumn";
            this.settingNameDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // settingValueDataGridViewTextBoxColumn
            // 
            this.settingValueDataGridViewTextBoxColumn.DataPropertyName = "SettingValue";
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.settingValueDataGridViewTextBoxColumn.DefaultCellStyle = dataGridViewCellStyle4;
            this.settingValueDataGridViewTextBoxColumn.HeaderText = "SettingValue";
            this.settingValueDataGridViewTextBoxColumn.Name = "settingValueDataGridViewTextBoxColumn";
            // 
            // settingTypeDataGridViewTextBoxColumn
            // 
            this.settingTypeDataGridViewTextBoxColumn.DataPropertyName = "SettingType";
            this.settingTypeDataGridViewTextBoxColumn.HeaderText = "SettingType";
            this.settingTypeDataGridViewTextBoxColumn.Name = "settingTypeDataGridViewTextBoxColumn";
            this.settingTypeDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // settingEntityModelBindingSource
            // 
            this.settingEntityModelBindingSource.DataSource = typeof(Microsoft.Scs.Test.RiskTools.RulePerf.Model.SettingEntityModel);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(924, 24);
            this.menuStrip1.TabIndex = 3;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.openToolStripMenuItem.Text = "&Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.saveToolStripMenuItem.Text = "&Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // StepSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(924, 477);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "StepSettingsForm";
            this.Text = "StepSettingsForm";
            this.Load += new System.EventHandler(this.StepSettingsForm_Load);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSettings)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.settingEntityModelBindingSource)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ExcelDataGridView dgvSettings;
        private System.Windows.Forms.BindingSource settingEntityModelBindingSource;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.DataGridViewTextBoxColumn settingNameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn settingValueDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn settingTypeDataGridViewTextBoxColumn;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
    }
}