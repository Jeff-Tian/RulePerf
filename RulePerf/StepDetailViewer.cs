// -----------------------------------------------------------------------
// <copyright file="StepDetailViewer.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Scs.Test.RiskTools.RulePerf
{
    using System;
    using System.Windows.Forms;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Helper;
    using Microsoft.Scs.Test.RiskTools.RulePerf.Model;

    /// <summary>
    /// Step Detail viewer
    /// </summary>
    public partial class StepDetailViewer : Form
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StepDetailViewer" /> class.
        /// </summary>
        public StepDetailViewer()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the step.
        /// </summary>
        /// <value>
        /// The step.
        /// </value>
        public Step Step { get; set; }

        /// <summary>
        /// Handles the Load event of the StepDetailViewer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void StepDetailViewer_Load(object sender, EventArgs e)
        {
            if (this.Step != null)
            {
                this.txtName.Text = this.Step.Name.ToString();
                this.txtDescription.Text = this.Step.Description.ToString();
                this.txtStatus.Text = this.Step.Status.ToString();
                if (this.Step.ResultDetail != null)
                {
                    this.txtDetailMessage.Text = this.Step.ResultDetail.Message.ToString();
                    if (this.Step.ResultDetail.Exceptions != null)
                    {
                        this.txtExceptionMessage.Text = ExceptionHelper.ExceptionLog(this.Step.ResultDetail.Exceptions.ToArray());
                    }
                    else
                    {
                        this.txtExceptionMessage.Text = "No exception log.";
                    }
                }
            }
        }
    }
}
