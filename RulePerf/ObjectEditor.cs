using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Microsoft.Scs.Test.RiskTools.RulePerf
{
    public partial class ObjectEditor : Form
    {
        public ObjectEditor()
        {
            InitializeComponent();
        }

        public PropertyGrid PropertyGrid
        {
            get
            {
                return this.propGrid;
            }
        }
    }
}
