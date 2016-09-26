using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Proco
{
    public partial class Viewer : Form
    {
        Bitmap mybit;
        int maxIndex = 0;

        public Viewer( double[,] Data, int index )
        {
            InitializeComponent();
            this.Text = this.Text + index;

            mybit = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            pictureBox1.Image = mybit;

            Brush b = new SolidBrush(Color.White);
            Brush b2 = new SolidBrush(Color.Black);
            Brush b3 = new SolidBrush(Color.Red);
            
            Graphics g = Graphics.FromImage(pictureBox1.Image);

            Pen mypen = new Pen(b);
            g.FillRectangle(b, new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height));

            double Max = -999;

            for (int i = 0; i < 256; i++ )
            {
                if(Max < Data[index, i]){
                    Max = Data[index, i];
                    maxIndex = i;
                }

                try
                {
                    if (Data[index, i] > Data[index, (i-1)] && Data[index, i] > Data[index, (i+1)])
                    {
                        g.FillRectangle(b3, new Rectangle((i * 2), 0, (i * 2) + 2, (int)Data[index, i]));
                    }
                    else
                    {
                        g.FillRectangle(b2, new Rectangle((i * 2), 0, (i * 2) + 2, (int)Data[index, i]));
                    }
                }
                catch
                {
                    g.FillRectangle(b2, new Rectangle((i * 2), 0, (i * 2) + 2, (int)Data[index, i]));
                }
                

            }
        }

        private void Viewer_Load(object sender, EventArgs e)
        {

        }

        public int getMax()
        {
            return maxIndex;
        }
    }
}
