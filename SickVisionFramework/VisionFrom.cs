using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SickVisionFramework
{
    public partial class VisionFrom : Form
    {
        public VisionFrom()
        {
            InitializeComponent();
        }

        private void VisionFrom_FormClosing(object sender, FormClosingEventArgs e)
        {
            visionControl1.releaseCamera();
        }
    }
}
