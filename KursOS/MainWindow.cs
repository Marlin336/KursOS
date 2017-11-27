using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KursOS
{
    public partial class MainWindow : Form
    {
        public FLog LogForm;
        public MainWindow(FLog fl)
        {
            LogForm = fl;
            InitializeComponent();
        }

        private void BEnter_Click(object sender, EventArgs e)
        {
            TBOut.Text += TBIn.Text + "\r\n";
            TBIn.Clear();
            TBIn.Focus();
        }

        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            LogForm.Visible = true;
        }
    }
}
