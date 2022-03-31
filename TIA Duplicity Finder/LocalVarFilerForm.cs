using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using TIA_Duplicity_Finder.Properties;

namespace TIA_Duplicity_Finder
{
    public partial class LocalVarFilerForm : Form
    {
        public string LocalVarFilter;

        public LocalVarFilerForm()
        {
            InitializeComponent();
        }

        private void LocalVarFilerForm_Load(object sender, EventArgs e)
        {
            textBox1.Text = Settings1.Default.LocalVarFilter;
        }

        private void LocalVarFilerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            LocalVarFilter = Settings1.Default.LocalVarFilter = textBox1.Text;
            Settings1.Default.Save();
        }
    }
}
