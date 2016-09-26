using NAudio.Wave;
using NAudio.Mixer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Threading;

using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace Proco
{
    

    public partial class Form1 : Form
    {
        private WaveIn waveSource = null;
        private WaveFileWriter waveFile = null;

        int m_readSkip = 16;            // 속도를 위해 스킵할때
        int m_bufferSize = 1024 * 16;   // 16 byte 버퍼
        double m_msecsPerPoint;         // 차트의 각 포인트의 milli seconds

        Bitmap mybit;
        int maxIndex = 0;

        Image<Bgr, byte> image;

        int PeakH_X, PeakH_Y;
        int PeakM_X, PeakM_Y;

        int[] Pool, Pool_Y;

        Thread mythread;
        private bool isRunning = true;

        int[,] ThresSet = new int[3, 7];

        int hHigh = 256, hLow = 1, sHigh = 256, sLow = 1, vHigh = 256, vLow = 0, threshold = 1;

        int YHigh = 125, YLow = 125, CbHigh = 256 , CbLow = 1, CrHigh = 256, CrLow = 1;

        int RHigh = 125, RLow = 125, GHigh = 125, GLow = 125, BHigh = 125, BLow = 125;

        bool isSetThres = true;

        class Label_class
        {

            public int amount = 0;

            public int x = 0, y = 0;
            int hX = -9999, hY = -9999, mX = 9999, mY = 9999;

            public Label_class()
            {
            }

            public void reset()
            {
                amount = 0;
                x = 0; y = 0;
                hX = -9999; hY = -9999; mX = 9999; mY = 9999;
            }

            public void getPos()
            {
                try
                {
                    x = x / amount;
                    y = y / amount;
                }
                catch
                {

                }
                
            }

            public int getDistance(int myX, int myY)
            {
                int result = (int)Math.Sqrt( Math.Abs(myX - x) * Math.Abs(myX - x) + Math.Abs(myY - y) * Math.Abs(myY - y) );

                return result;
            }

            public void getData(int x1, int y1)
            {
                if (x1 > hX)
                {
                    hX = x1;
                }
                if (y1 > hY)
                {
                    hY = y1;
                }
                if (mX < x1)
                {
                    mX = x1;
                }
                if (mY < y1)
                {
                    mY = y1;
                }

                this.x += x1;
                this.y += y1;

                amount++;
            }

        }

        String win = "Window";

        bool isCaptured = false;

        Matrix<Byte> mat;

        Capture capture;

        struct HSV{
            public int h;        // Hue: 0 ~ 255 (red:0, gree: 85, blue: 171) 
            public int s;        // Saturation: 0 ~ 255 
            public int v;
        };

        public int getMin(int[] value)
        {

            int min = 255;

            for (int i = 0; i < value.Length; i++)
            {
                if(min < value[i]){
                    min = value[i];
                }
            }

            return min;
        }

        public int getMax(int[] value)
        {

            int min = 0;

            for (int i = 0; i < value.Length; i++)
            {
                if (min > value[i])
                {
                    min = value[i];
                }
            }

            return min;
        }

HSV RGB2HSV(Byte r, Byte g, Byte b)
{
    int rgb_min, rgb_max;

    int[] RGB_temp = new int[3];
    RGB_temp[0] = b; RGB_temp[1] = g; RGB_temp[2] = r;

    rgb_min = getMax(RGB_temp);
    rgb_max = getMin(RGB_temp);

	HSV hsv;
	hsv.v = rgb_max;
	if (hsv.v == 0) {
		hsv.h = hsv.s = 0;
		return hsv;
	}

	hsv.s = 255 * (rgb_max - rgb_min) / hsv.v;
	if (hsv.s == 0) {
		hsv.h = 0;
		return hsv;
	}

	if (rgb_max == r) {
		hsv.h = 0 + 43 * (g - b) / (rgb_max - rgb_min);
	}
	else if (rgb_max == g) {
		hsv.h = 85 + 43 * (b - r) / (rgb_max - rgb_min);
	}
	else /* rgb_max == rgb.b */ {
		hsv.h = 171 + 43 * (r - g) / (rgb_max - rgb_min);
	}

	return hsv;
}

        int numberOfLabel = 0;
        bool[] isLabelObject = new bool[640 * 480];
        int[] LabelNumber = new int[640 * 480];
        int[] checkLabel = new int[640 * 480];
        bool[] isChecked = new bool[640 * 480];
        int[] amountOfLabel = new int[640 * 480];
        Label_class[] labels = new Label_class[640 * 480];

        HSV[] myhsv = new HSV[640 * 480];

        bool isCheck = false;

        Image<Bgr, Byte> capturedImage = null, currentImage = null;
        Image<Bgr, Byte> storeImage = null;
        bool[,] isDataPixel = null;

        public Form1()
        {
            InitializeComponent();
            
        }

        private void button1_Click(object sender, EventArgs e)
        {

            NAudio.Wave.WaveChannel32 wave = new NAudio.Wave.WaveChannel32(new NAudio.Wave.WaveFileReader("temp.wav"));

            byte[] buffer = new byte[m_bufferSize];
            int read = 0;

            chart1.Series.Add("wave");
            chart1.Series["wave"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
            chart1.Series["wave"].ChartArea = "ChartArea1";
            chart1.Series["wave"].Color = Color.Red;

            while (wave.Position < wave.Length)
            {
                read = wave.Read(buffer, 0, m_bufferSize);
                WaveProcessor waveprocessor = new WaveProcessor(buffer);
                byte[] pooled = waveprocessor.MaxPooling(40);

                for (int i = 0; i < read / 4; i++)
                {
                    chart1.Series["wave"].Points.Add(BitConverter.ToSingle(buffer, i * 4));
                }

            }

            chart1.SaveImage(@"savedimage", System.Drawing.Imaging.ImageFormat.Jpeg);

            //timer1.Interval = 15;
            //timer1.Enabled = true;

            capture = new Emgu.CV.Capture(1);

            mythread = new Thread(new ThreadStart(Run));
            mythread.Start();

            //imageBox1.Width = 640; imageBox1.Height = 480;
            //imageBox3.Width = 640; imageBox3.Height = 480;
            imageBox2.Width = 160; imageBox2.Height = 120;

        }

        void Run()
        {
            while (isRunning)
            {
                for (int i = 0; i < 640 * 480; i++)
                {
                    LabelNumber[i] = -1; amountOfLabel[i] = 0;
                    checkLabel[i] = -1;
                    isChecked[i] = false;
                    isLabelObject[i] = false;
                    labels[i].reset();
                    //workSpace.data[i] = 0;
                }

                CvInvoke.cvGrabFrame(capture);


                Image<Bgr, Byte> original = capture.QueryFrame();
                Image<Bgr, Byte> camMat = new Image<Bgr, Byte>(640, 480);

                if (isCaptured)
                {
                    camMat = currentImage.Copy();
                }
                else
                {
                    camMat = capture.QueryFrame().Copy();
                }
                

                Image<Ycc, Byte> ycc = camMat.Convert<Ycc, Byte>();
                Image<Hsv, Byte> hsv = camMat.Convert<Hsv, Byte>();
                Image<Bgr, Byte> myimage = camMat.Convert<Bgr, Byte>();
                 
                //imageBox3.Image = myimage;

                Image<Gray, Byte> gray = camMat.Convert<Gray, byte>();
                Image<Gray, Byte> binary = new Image<Gray, byte>(640, 480);

                Image<Gray, Byte> workSpace = new Image<Gray, byte>(640, 480);
                
                for (int j = 0; j < 640; j++)
                {
                    for (int i = 0; i < 480; i++)
                    {
                        myhsv[i * camMat.Width + j] = RGB2HSV(myimage.Data[i, j, 2], myimage.Data[i, j, 1], myimage.Data[i, j, 0]);
                        workSpace.Data[i, j, 0] = 0;

                        if (gray.Data[i, j, 0] > threshold)
                        {
                            binary.Data[i, j, 0] = 255;
                        }
                        else
                        {
                            binary.Data[i, j, 0] = 0;
                        }

                    }
                }

                Image<Gray, Byte> marker = new Image<Gray, byte>(640, 480);
                Image<Gray, Byte> fg = new Image<Gray, byte>(640, 480);
                Image<Gray, Byte> bg = new Image<Gray, byte>(640, 480);

                Image<Gray, Byte> window_binary = new Image<Gray, byte>(160, 120);
                
                for (int j = 280; j < 440; j++)
                {
                    for (int i = 180; i < 300; i++)
                    {

                        if (binary.Data[i, j, 0] > 128)
                        {
                            window_binary.Data[(i-180),(j-280),0] = 0;
                        }
                        else
                        {
                            /*
                            if (myhsv[j + i * 160].h > hLow && myhsv[j + i * 160].h < hHigh &&
                                myhsv[j + i * 160].s > sLow && myhsv[j + i * 160].s < sHigh &&
                                myhsv[j + i * 160].v > vLow && myhsv[j + i * 160].v < vHigh)
                            {

                                window_binary.Data[(i - 180), (j - 280), 0] = 255;

                            }
                            else
                            {
                                window_binary.Data[(i - 180), (j - 280), 0] = 0;
                            }
                            */
                            if (hsv.Data[i, j, 0] >= hLow && hsv.Data[i, j, 0] <= hHigh &&
                                hsv.Data[i, j, 1] >= sLow && hsv.Data[i, j, 1] <= sHigh &&
                                hsv.Data[i, j, 2] >= vLow && hsv.Data[i, j, 2] <= vHigh &&
                                (ycc.Data[i, j, 0] <= YLow || ycc.Data[i, j, 0] >= YHigh) &&
                                ycc.Data[i, j, 1] >= CbLow && ycc.Data[i, j, 1] <= CbHigh &&
                                ycc.Data[i, j, 2] >= CrLow && ycc.Data[i, j, 2] <= CrHigh &&
                                (camMat.Data[i, j, 0] <= RLow || camMat.Data[i, j, 0] >= RHigh) &&
                                (camMat.Data[i, j, 1] <= GLow || camMat.Data[i, j, 1] >= GHigh) &&
                                (camMat.Data[i, j, 2] <= BLow || camMat.Data[i, j, 2] >= BHigh) ) 
                            {

                                window_binary.Data[(i - 180), (j - 280), 0] = 255;

                            }
                            else
                            {
                                window_binary.Data[(i - 180), (j - 280), 0] = 0;
                            }

                        }

                    }
                }


                //라벨링
                int ac = 3; numberOfLabel = 0;

                for (int i = ac; i < window_binary.Width - ac; i++)
                {
                    for (int j = ac; j < window_binary.Height - ac; j++)
                    {

                        if (window_binary.Data[j, i, 0] == 255)
                        {

                            if (!isChecked[i + j * window_binary.Width])
                            {
                                LabelNumber[i + j * window_binary.Width] = numberOfLabel;

                                for (int x = -ac; x < ac; x++)
                                {
                                    for (int y = -ac; y < ac; y++)
                                    {

                                        isChecked[(i + x) + (j + y) * window_binary.Width] = true;
                                        checkLabel[(i + x) + (j + y) * window_binary.Width] = numberOfLabel;

                                    }
                                }

                                numberOfLabel++;
                            }
                            else
                            {
                                LabelNumber[i + j * window_binary.Width] = checkLabel[i + j * window_binary.Width];

                                for (int x = -ac; x < ac; x++)
                                {
                                    for (int y = -ac; y < ac; y++)
                                    {

                                        isChecked[(i + x) + (j + y) * window_binary.Width] = true;
                                        checkLabel[(i + x) + (j + y) * window_binary.Width] = checkLabel[i + j * window_binary.Width];

                                    }
                                }

                            }

                        }

                    }
                }

                for (int i = ac; i < window_binary.Width - ac; i++)
                {
                    for (int j = ac; j < window_binary.Height - ac; j++)
                    {

                        if (window_binary.Data[j, i, 0] == 255)
                        {
                            labels[LabelNumber[i + j * window_binary.Width]].getData(i, j);
                            amountOfLabel[LabelNumber[i + j * window_binary.Width]]++;

                        }

                    }
                }

                int max = -999;
                int index = -1;

                for (int i = 0; i < numberOfLabel; i++)
                {
                    labels[i].getPos();

                    if (amountOfLabel[i] > max)
                    {
                        max = amountOfLabel[i];
                        index = i;
                    }

                }

                
                for (int i = ac; i < window_binary.Width - ac; i++)
                {
                    for (int j = ac; j < window_binary.Height - ac; j++)
                    {

                        if (window_binary.Data[j, i, 0] == 255)
                        {

                            if (amountOfLabel[LabelNumber[i + j * window_binary.Width]] > 150 && labels[LabelNumber[i + j * window_binary.Width]].getDistance(80, 60) < 35)
                            {
                                window_binary.Data[j, i, 0] = 125;
                                isLabelObject[i + j * window_binary.Width] = true;
                                workSpace.Data[(j + 180), (i + 280), 0] = 255;
                            }

                        }

                    }
                }

                for (int i = ac; i < window_binary.Width - ac; i++)
                {
                    for (int j = ac; j < window_binary.Height - ac; j++)
                    {

                        if (workSpace.Data[((j+1) + 180), (i + 280), 0] != workSpace.Data[(j + 180), (i + 280), 0])
                        {

                            //myimage.Data[j + 180, i + 280, 0] = 255;
                            //myimage.Data[j + 180, i + 280, 1] = 255;
                            //myimage.Data[j + 180, i + 280, 2] = 0;

                        }

                    }
                }

                for (int i = ac; i < window_binary.Width - ac; i++)
                {
                    for (int j = ac; j < window_binary.Height - ac; j++)
                    {
                        if( isLabelObject[i + j * window_binary.Width] ){
                            //workSpace.Data[j, i, 0] = 255;

                        }
                    }
                }

                CvInvoke.cvErode(workSpace, fg, IntPtr.Zero, 0);
                CvInvoke.cvDilate(workSpace, bg, IntPtr.Zero, 16);
                CvInvoke.cvThreshold(bg, bg, 1, 128, THRESH.CV_THRESH_BINARY_INV);

                marker = fg + bg;
                Image<Gray, Int32> markers = marker.Convert<Gray, Int32>();
                
                CvInvoke.cvWatershed(camMat, markers);

                Image<Gray, Byte> result = markers.Convert<Gray, Byte>();

                int avgPointX = 0, avgPointY = 0, avgCnt = 0;
                int segAvgPointX = 0, segAvgPointY = 0, segAvgCnt = 0;
                int segHx = -999, segMx = 999, segHy = -999, segMy = 999;

                int segHx_y = -999, segMx_y = 999, segHy_x = -999, segMy_x = 999;

                
                for (int i = 3; i < result.Width - 3; i++ )
                {
                    for (int j = 3; j < result.Height - 3; j++ )
                    {
                        if (result.Data[j, i, 0] == 255)
                        {

                            if (i > segHx)
                            {
                                segHx = i;
                                segHx_y = j;
                            }


                            if (i < segMx)
                            {
                                segMx = i;
                                segMx_y = j;
                            }


                            if (j > segHy)
                            {
                                segHy = j;
                                segHy_x = i;
                            }


                            if (j < segMy)
                            {
                                segMy = j;
                                segMy_x = i;
                            }
                                


                            segAvgPointX += i; segAvgPointY += j;
                            segAvgCnt++;
                        }

                        if ((int)result.Data[j, i, 0] != (int)result.Data[j, (i + 1), 0])
                        {
                           // Console.WriteLine(" " + result.Data[j, i, 0] + " " + result.Data[j, (i + 1), 0] + " ");
                            avgPointX += i; avgPointY += j;
                            avgCnt++;

                            myimage.Data[j, i, 0] = 255;
                            myimage.Data[j, i, 1] = 255;
                            myimage.Data[j, i, 2] = 255;

                        }

                    }
                }

                Point[] points = new Point[4];
                int pX = segHx - segMx;
                int pY = segHy - segMy;


                for (int j = segMy; j < segHy; j++ )
                {

                    if (result.Data[j, (segMx + (pX * 1 / 3)), 0] == 255)
                    {
                        points[0].X = (segMx + (pX * 1 / 3)); points[0].Y = j;

                    }

                }

                for (int j = segHy - 1; j >= segMy; j--)
                {

                    if (result.Data[j, (segMx + (pX * 1 / 3)), 0] == 255)
                    {
                        points[1].X = (segMx + (pX * 1 / 3)); points[1].Y = j;

                    }

                }



                for (int j = segMy; j < segHy; j++)
                {

                    if (result.Data[j, (segMx + (pX * 2 / 3)), 0] == 255)
                    {
                        points[2].X = (segMx + (pX * 2 / 3)); points[2].Y = j;

                    }

                }

                for (int j = segHy - 1; j >= segMy; j--)
                {

                    if (result.Data[j, (segMx + (pX * 2 / 3)), 0] == 255)
                    {
                        points[3].X = (segMx + (pX * 2 / 3)); points[3].Y = j;

                    }

                }


                try
                {
                    segAvgPointX = segAvgPointX / segAvgCnt;
                    segAvgPointY = segAvgPointY / segAvgCnt;
                }
                catch
                {

                }

                try
                {
                    avgPointX = avgPointX / avgCnt;
                    avgPointY = avgPointY / avgCnt;
                }
                catch
                {

                }

                Console.WriteLine("X: " + segHx + " " + segMx + " Y: " + segHy + " " + segMy);

                CvInvoke.cvCircle(result, new Point(segAvgPointX, segAvgPointY), 5, new MCvScalar(0, 255, 255), 1, LINE_TYPE.FOUR_CONNECTED, 0);
                CvInvoke.cvRectangle(result, new Point(segMx, segMy), new Point(segHx, segHy), new MCvScalar(0, 255, 255), 1, LINE_TYPE.FOUR_CONNECTED, 0);
                CvInvoke.cvCircle(myimage, new Point(avgPointX, avgPointY), 5, new MCvScalar(0, 255, 255), 1, LINE_TYPE.FOUR_CONNECTED, 0);
                //CvInvoke.cvRectangle(camMat, new Point(avgPointX, avgPointY), new Point(avgPointX+2, avgPointY+2), new MCvScalar(0, 255, 255), 1, LINE_TYPE.FOUR_CONNECTED, 0);

                CvInvoke.cvCircle(myimage, new Point(segHx, segHx_y), 5, new MCvScalar(0, 255, 255), 1, LINE_TYPE.FOUR_CONNECTED, 0);
                CvInvoke.cvCircle(myimage, new Point(segMx, segMx_y), 5, new MCvScalar(0, 255, 255), 1, LINE_TYPE.FOUR_CONNECTED, 0);

                CvInvoke.cvCircle(myimage, new Point(segHy_x, segHy), 5, new MCvScalar(0, 255, 255), 1, LINE_TYPE.FOUR_CONNECTED, 0);
                CvInvoke.cvCircle(myimage, new Point(segMy_x, segMy), 5, new MCvScalar(0, 255, 255), 1, LINE_TYPE.FOUR_CONNECTED, 0);

                for (int k = 0; k < 4; k++ )
                {
                    CvInvoke.cvCircle(myimage, points[k], 5, new MCvScalar(55, 255, 55), 1, LINE_TYPE.FOUR_CONNECTED, 0);
                }

                if (isCheck)
                {
                    int[] a = null;
                    a[1] = 5;
                }

                //Console.WriteLine(numberOfLabel + "@@@@@@@@@@@@@@@@@@@@");
                //CvInvoke.cvCvtColor(camMat, gray, COLOR_CONVERSION.BGR2GRAY);

                imageBox4.Image = myimage;
                //imageBox3.Image = result;
                imageBox2.Image = window_binary;
                //imageBox1.Image = binary;
            }

            //Console.WriteLine("isRunning!!");
        } 

        private void timer1_Tick(object sender, EventArgs e)
        {
           

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            mythread.Suspend();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            isCheck = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Form2 f2 = new Form2();
            f2.Show();

            mythread = new Thread(() => Run());

            for (int i = 0; i < 640 * 480; i++)
            {
                labels[i] = new Label_class();
            }

            trackBar2.Value = 255;
            trackBar3.Value = 255;
            trackBar5.Value = 255;
        }

        private void trackBar1_CursorChanged(object sender, EventArgs e)
        {
            //Console.WriteLine("@@@");
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            if(isSetThres){
                hLow = trackBar1.Value;
                label1.Text = "H:" + trackBar1.Value + " , " + trackBar2.Value;
            }
            
        }

        private void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            if (isSetThres)
            {
                hHigh = trackBar2.Value;
                label1.Text = "H:" + trackBar1.Value + " , " + trackBar2.Value;
            }
            
        }

        private void trackBar4_ValueChanged(object sender, EventArgs e)
        {
            if (isSetThres)
            {
                sLow = trackBar4.Value;
                label2.Text = "S:" + trackBar4.Value + " , " + trackBar3.Value;
            }
            
        }

        private void trackBar3_ValueChanged(object sender, EventArgs e)
        {
            if (isSetThres)
            {
                sHigh = trackBar3.Value;
                label2.Text = "S:" + trackBar4.Value + " , " + trackBar3.Value;
            }
            
        }

        private void trackBar6_ValueChanged(object sender, EventArgs e)
        {
            if (isSetThres)
            {
                vLow = trackBar6.Value;
                label3.Text = "V:" + trackBar6.Value + " , " + trackBar5.Value;
            }
            
        }

        private void trackBar5_ValueChanged(object sender, EventArgs e)
        {
            if (isSetThres)
            {
                vHigh = trackBar5.Value;
                label3.Text = "V:" + trackBar6.Value + " , " + trackBar5.Value;
            }
            
        }

        private void trackBar7_ValueChanged(object sender, EventArgs e)
        {
            if (isSetThres)
            {
                threshold = trackBar7.Value;
                label3.Text = "V:" + trackBar7.Value + " , " + trackBar5.Value;
            }
            
        }

        private void trackBar12_ValueChanged(object sender, EventArgs e)
        {
            if (isSetThres)
            {
                YLow = trackBar12.Value;
                label8.Text = "V:" + trackBar12.Value + " , " + trackBar5.Value;
            }
            
        }

        private void trackBar13_ValueChanged(object sender, EventArgs e)
        {
            if (isSetThres)
            {
                YHigh = trackBar13.Value;
                label8.Text = "V:" + trackBar13.Value + " , " + trackBar5.Value;
            }
            
        }

        private void trackBar10_ValueChanged(object sender, EventArgs e)
        {
            if (isSetThres)
            {
                CbLow = trackBar10.Value;
                label7.Text = "Cb:" + trackBar10.Value + " , " + trackBar5.Value;
            }
            
        }

        private void trackBar11_ValueChanged(object sender, EventArgs e)
        {
            if (isSetThres)
            {
                CbHigh = trackBar11.Value;
                label7.Text = "Cb:" + trackBar11.Value + " , " + trackBar5.Value;
            }
            
        }

        private void trackBar8_ValueChanged(object sender, EventArgs e)
        {
            if (isSetThres)
            {
                CrLow = trackBar8.Value;
                label6.Text = "Cr:" + trackBar8.Value + " , " + trackBar5.Value;
            }
            
        }

        private void trackBar9_ValueChanged(object sender, EventArgs e)
        {
            if (isSetThres)
            {
                CrHigh = trackBar9.Value;
                label6.Text = "Cr:" + trackBar9.Value + " , " + trackBar5.Value;
            }
            
        }

        private void trackBar18_ValueChanged(object sender, EventArgs e)
        {
            if (isSetThres)
            {
                RLow = trackBar18.Value;
                label11.Text = "R:" + trackBar18.Value + " , " + trackBar19.Value;
            }
            
        }

        private void trackBar19_ValueChanged(object sender, EventArgs e)
        {
            if (isSetThres)
            {
                RHigh = trackBar19.Value;
                label11.Text = "R:" + trackBar18.Value + " , " + trackBar19.Value;
            }
            
        }

        private void trackBar16_ValueChanged(object sender, EventArgs e)
        {
            if (isSetThres)
            {
                GLow = trackBar16.Value;
                label10.Text = "G:" + trackBar16.Value + " , " + trackBar17.Value;
            }
            
        }

        private void trackBar17_ValueChanged(object sender, EventArgs e)
        {
            if (isSetThres)
            {
                GHigh = trackBar17.Value;
                label10.Text = "G:" + trackBar16.Value + " , " + trackBar17.Value;
            }
            
        }

        private void trackBar14_ValueChanged(object sender, EventArgs e)
        {
            if (isSetThres)
            {
                BLow = trackBar14.Value;
                label8.Text = "B:" + trackBar14.Value + " , " + trackBar15.Value;
            }
            
        }

        private void trackBar15_ValueChanged(object sender, EventArgs e)
        {
            BHigh = trackBar15.Value;
            label9.Text = "B:" + trackBar14.Value + " , " + trackBar15.Value;
        }

        bool isClicked = false;
        bool isClicked2 = false;

        int lastPosX = -1, lastPosY = -1;
        int myCode = -1;

        private void button3_Click(object sender, EventArgs e)
        {
            isCaptured = true;
             capturedImage = capture.QueryFrame().Copy();
             currentImage = capture.QueryFrame().Copy();

             storeImage = capture.QueryFrame().Copy(); 
             imageBox5.Image = capturedImage;

             isDataPixel = new bool[capturedImage.Height, capturedImage.Width];

             for (int i = 0; i < capturedImage.Width; i++)
             {

                 for (int j = 0; j < capturedImage.Height; j++)
                 {
                     isDataPixel[j, i] = false;
                 }
             }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if( isClicked ){
                isClicked = false;
            }
            else
            {
                isClicked = true;
            }

            lastPosX = -1;
            lastPosY = -1;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (isClicked2)
            {
                isClicked2 = false;
            }
            else
            {
                isClicked2 = true;
            }


            isClicked = false;

            lastPosX = -1;
            lastPosY = -1;
        }

        private void imageBox5_MouseUp(object sender, MouseEventArgs e)
        {
            
            if( isClicked ){

                if(lastPosX == -1){
                    lastPosX = e.X;
                    lastPosY = e.Y;
                }
                else
                {
                    CvInvoke.cvLine(capturedImage, new Point(lastPosX, lastPosY), new Point(e.X, e.Y), new MCvScalar(5,155,5), 1, LINE_TYPE.EIGHT_CONNECTED, 0);
                    lastPosX = e.X; lastPosY = e.Y;
                }

            }
            else if (isClicked2)
            {

                if (lastPosX == -1)
                {
                    lastPosX = e.X;
                    lastPosY = e.Y;
                }
                else
                {
                    CvInvoke.cvLine(capturedImage, new Point(lastPosX, lastPosY), new Point(e.X, e.Y), new MCvScalar(155, 15, 155), 1, LINE_TYPE.EIGHT_CONNECTED, 0);
                    lastPosX = e.X; lastPosY = e.Y;
                }

            }

            imageBox5.Refresh();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < capturedImage.Width; i++ )
            {

                int start = 0, end = 0;

                for (int j = 0; j < capturedImage.Height; j++ )
                {

                    if (capturedImage.Data[j, i, 0] == 5 && capturedImage.Data[j, i, 1] == 155 && capturedImage.Data[j, i, 2] == 5 && start == 0)
                    {
                        start = j;
                    }
                    else if (capturedImage.Data[j, i, 0] == 155 && capturedImage.Data[j, i, 1] == 15 && capturedImage.Data[j, i, 2] == 155 && end == 0)
                    {
                        end = j;

                        for (int a = start; a < end; a++)
                        {
                            capturedImage.Data[a, i, 0] = 255;
                            capturedImage.Data[a, i, 1] = 255;
                            capturedImage.Data[a, i, 2] = 255;

                            isDataPixel[a, i] = true;
                        }

                        start = 0; end = 0;
                    }

                }


            }

            imageBox5.Refresh();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            capturedImage = storeImage.Copy();
            imageBox5.Image = capturedImage;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            double[,] tp = new double[7, 256], tn = new double[7, 256], fp = new double[7, 256], fn = new double[7, 256];
            double tpW = double.Parse(textBox2.Text), tnW = 1.0, fpW = double.Parse(textBox3.Text), fnW = 1;
            double[,] score = new double[7, 256];

            Image<Hsv, Byte> hsv = storeImage.Convert<Hsv, Byte>();
            Image<Gray, Byte> gray = storeImage.Convert<Gray, Byte>();
            Image<Ycc, Byte> YCrCb = storeImage.Convert<Ycc, Byte>();


            try
            {
                myCode = int.Parse(textBox1.Text);
            }
            catch
            {
                myCode = 0;
            }

            for (int c = 0; c < 256; c++ )
            {

                for (int i = 280; i < 440; i++)
                {
                    for (int j = 180; j < 300; j++)
                    {
                        
                        

                        for (int myCode = 0; myCode < 7; myCode++ )
                        {
                            byte data = 0;

                            if (myCode == 0)
                            {
                                data = gray.Data[j, i, 0];
                            }else if(myCode == 1){
                                data = hsv.Data[j, i, 1];
                            }
                            else if (myCode == 2)
                            {
                                data = YCrCb.Data[j, i, 0];
                            }
                            else if (myCode == 3)
                            {
                                data = YCrCb.Data[j, i, 1];
                            }
                            else if (myCode == 4)
                            {
                                data = YCrCb.Data[j, i, 2];
                            }
                            else if (myCode == 5)
                            {
                                data = storeImage.Data[j, i, 0];
                            }
                            else if (myCode == 6)
                            {
                                data = storeImage.Data[j, i, 1];
                            }


                            if( myCode >= 5 || myCode == 2 || myCode == 0 ){
                                if (data < c)
                                {
                                    // positive

                                    if (isDataPixel[j, i])
                                    {
                                        tp[myCode, c]++;
                                    }
                                    else
                                    {
                                        fp[myCode, c]++;
                                    }

                                }
                                else
                                {
                                    //negative

                                    if (isDataPixel[j, i])
                                    {
                                        tn[myCode, c]++;
                                    }
                                    else
                                    {
                                        fn[myCode, c]++;
                                    }
                                }
                            }
                            else
                            {
                                if (data > c)
                                {
                                    // positive

                                    if (isDataPixel[j, i])
                                    {
                                        tp[myCode, c]++;
                                    }
                                    else
                                    {
                                        fp[myCode, c]++;
                                    }

                                }
                                else
                                {
                                    //negative

                                    if (isDataPixel[j, i])
                                    {
                                        tn[myCode, c]++;
                                    }
                                    else
                                    {
                                        fn[myCode, c]++;
                                    }
                                }
                            }
                            
                        }
                        

                    }
                }

                for (int myCode = 0; myCode < 7; myCode++)
                {
                    double Precision = (tp[myCode, c] * tpW) / ((tp[myCode, c] * tpW) + (fp[myCode, c] * fpW));
                    double Recall = (tp[myCode, c] * tpW) / ((tp[myCode, c] * tpW) + (fn[myCode, c] * fnW));

                    score[myCode, c] = 2 * (Precision * Recall) / (Precision + Recall);
                    score[myCode, c] = score[myCode, c] * 5000 - 300;

                }

                
            }
            
            for (int myCode = 0; myCode < 7; myCode++)
            {
                Viewer viewer = new Viewer(score, myCode);
                viewer.Show();

                if (myCode == 0)
                {
                    threshold = viewer.getMax() + 3;
                }
                else if (myCode == 1)
                {
                    sLow = viewer.getMax() - 3;
                }
                else if (myCode == 2)
                {
                    YLow = viewer.getMax() - 3;
                }
                else if (myCode == 3)
                {
                    CbLow = viewer.getMax() - 3;
                }
                else if (myCode == 4)
                {
                    CrLow = viewer.getMax() - 3;
                }
                else if (myCode == 5)
                {
                    RLow = viewer.getMax() + 3;
                }
                else if (myCode == 6)
                {
                    GLow = viewer.getMax() + 3;
                }
                
            }

            int count = 0;

            try
            {
                count = int.Parse(textBox4.Text);
            }
            catch
            {
                textBox1.Text = "0";
            }

            ThresSet[count, 0] = sLow;
            ThresSet[count, 1] = YLow;
            ThresSet[count, 2] = CbLow;
            ThresSet[count, 3] = CrLow;
            ThresSet[count, 4] = RLow;
            ThresSet[count, 5] = GLow;
            ThresSet[count, 6] = threshold;

            if (count == 2)
            {
                count = 0;
            }
            else
            {
                count = count + 1;
            }

            textBox4.Text = count + "";

            isSetThres = false;
            
        }

        private void trackBar18_Scroll(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            isSetThres = true;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            int count = 0;

            try
            {
                count = int.Parse(textBox4.Text);

            }
            catch
            {
                textBox1.Text = "0";
            }

            sLow = ThresSet[count, 0];
            YLow = ThresSet[count, 1];
            CbLow = ThresSet[count, 2];
            CrLow = ThresSet[count, 3];
            RLow = ThresSet[count, 4];
            GLow = ThresSet[count, 5];
            threshold = ThresSet[count, 6];

            if (count == 2)
            {
                count = 0;
            }
            else
            {
                count = count + 1;
            }

            textBox4.Text = count + "";
        }


    }
}
