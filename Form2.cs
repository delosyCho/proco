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

using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using Proco;

namespace Proco
{
    public partial class Form2 : Form
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


        public Form2()
        {
            InitializeComponent();
        }

        void waveSource_DataAvailable(object sender, WaveInEventArgs e)
        {

            if (waveFile != null)
            {
                waveFile.Write(e.Buffer, 0, e.BytesRecorded);
                waveFile.Flush();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            waveSource = new WaveIn();
            waveSource.WaveFormat = new WaveFormat(44100, 1);

            waveSource.DataAvailable += new EventHandler<WaveInEventArgs>(waveSource_DataAvailable);
            waveSource.RecordingStopped += new EventHandler<StoppedEventArgs>(waveSource_RecordingStopped);

            waveFile = new WaveFileWriter(@"temp.wav", waveSource.WaveFormat);

            waveSource.StartRecording();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            waveSource.StopRecording();
        }

        void waveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (waveSource != null)
            {
                waveSource.Dispose();
                waveSource = null;
            }

            if (waveFile != null)
            {
                waveFile.Dispose();
                waveFile = null;
            }

        }

        private void button3_Click(object sender, EventArgs e)
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
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int asymmetric = 0; double asymmetric2 = 0;

            image = new Image<Bgr, byte>(@"savedImage");
            Pool = new int[500]; Pool_Y = new int[500];

            int current = 0;

            for (int i = 20; i < 450; i++)
            {
                int count = 0;

                for (int j = 20; j < 250; j++)
                {
                    if (image.Data[j, i, 2] > 150 && image.Data[j, i, 1] < 150 && image.Data[j, i, 0] < 150)
                    {
                        count++;
                    }
                }

                if (count < 10)
                {
                    CvInvoke.cvCircle(image, new Point(i, 131), 3, new MCvScalar(255, 155, 155), 1, LINE_TYPE.FOUR_CONNECTED, 0);
                }

            }

            for (int j = 20; j < 250; j++)
            {
                for (int i = 20; i < 450; i++)
                {

                    if (image.Data[j, i, 2] > 150 && image.Data[j, i, 1] < 150 && image.Data[j, i, 0] < 150)
                    {
                        
                        Console.WriteLine("@@@@@@@@@@@@@@");
                        PeakH_X = i; PeakH_Y = j;
                        i = 999; j = 999;
                    }

                }
            }

            for (int j = 250 - 1; j >= 20; j--)
            {
                for (int i = 20; i < 450; i++)
                {

                    if (image.Data[j, i, 2] > 150 && image.Data[j, i, 1] < 150 && image.Data[j, i, 0] < 150)
                    {
                        
                        Console.WriteLine("@@@@@@@@@@@@@@");
                        PeakM_X = i; PeakM_Y = j;
                        i = 999; j = -1;
                    }

                }
            }

            bool isMeet = false, MeetPeak = false; ;
            int Start = 0, End = 0;

            for (int i = 20; i < 450; i++)
            {
                bool isPixel = false;

                for (int j = 230 - 1; j >= 20; j--)
                {
                    if(i == PeakH_X){
                        MeetPeak = true;
                    }

                    if (image.Data[j, i, 2] > 150 && image.Data[j, i, 1] < 150 && image.Data[j, i, 0] < 150)
                    {
                        isPixel = true;
                    }
                }

                if(isPixel){
                    if (!isMeet)
                    {
                        Start = i;
                        isMeet = true;
                    }
                }
                else
                {
                    Console.WriteLine("Current" + i);
                    if (isMeet && MeetPeak)
                    {
                        End = i;
                        i = 450;
                    }
                    else
                    {
                        isMeet = false;
                    }
                }
            }

            CvInvoke.cvLine(image, new Point(Start, 0), new Point(Start, 250), new MCvScalar(175, 215, 255), 2, LINE_TYPE.FOUR_CONNECTED, 0);
            CvInvoke.cvLine(image, new Point(End, 0), new Point(End, 250), new MCvScalar(175, 215, 255), 2, LINE_TYPE.FOUR_CONNECTED, 0);

            CvInvoke.cvCircle(image, new Point(PeakH_X, PeakH_Y), 7, new MCvScalar(255, 155, 155), 1, LINE_TYPE.FOUR_CONNECTED, 0);
            CvInvoke.cvCircle(image, new Point(PeakM_X, PeakM_Y), 7, new MCvScalar(255, 155, 155), 1, LINE_TYPE.FOUR_CONNECTED, 0);

            int div = (PeakH_X + PeakM_X) / 2;
            asymmetric = (Math.Abs(Start - div) - Math.Abs(End - div)) * (Math.Abs(Start - div) - Math.Abs(End - div));


            for (int a = (20 / 5); a < (450 / 5); a++)
            {
                int max = 999;

                for (int i = 0; i < 5; i++)
                {
                    for (int j = 20; j < 250; j++)
                    {
                        int t = a * 5 + i;

                        if (image.Data[j, t, 2] > 150 && image.Data[j, t, 1] < 150 && image.Data[j, t, 0] < 150)
                        {
                            if (j < max)
                            {
                                Pool[a] = j; max = j; 
                            }
                            j = 999; 
                        }
                    }
                }
            }

            for (int a = (20 / 5); a < (450 / 5); a++)
            {
                int max = -999;

                for (int i = 0; i < 5; i++)
                {
                    for (int j = 250 - 1; j >= 20; j--)
                    {
                        int t = a * 5 + i;

                        if (image.Data[j, t, 2] > 150 && image.Data[j, t, 1] < 150 && image.Data[j, t, 0] < 150)
                        {
                            if (j > max)
                            {
                                Pool_Y[a] = j; max = j;
                            }
                            j = -1;
                        }
                    }
                }
            }

            int tri_Cnt = 0, tri_Cnt2 = 0;
            int tri_Cnt_des = 0, tri_Cnt2_des = 0;
            

            for (int a = (20 / 5); a < (450 / 5); a++)
            {
                for (int j = 20; j < 250; j++)
                {

                    CvInvoke.cvLine(image, new Point((a * 5), Pool[a]), new Point((a * 5 + 4), Pool[a]), new MCvScalar(175, 215, 255), 2, LINE_TYPE.FOUR_CONNECTED, 0);

                }
            }

            for(int a = (Start / 5); a < (End / 5) - 1; a++){

                if( Pool[a] > Pool[a - 1] && Pool[a] != 0 && Pool[a - 1] != 0  ){
                    CvInvoke.cvLine(image, new Point(((a - 1) * 5) + 3, Pool[a - 1]), new Point((a * 5) + 3, Pool[a]), new MCvScalar(5, 215, 5), 1, LINE_TYPE.FOUR_CONNECTED, 0);
                    tri_Cnt++;
                }
                else
                {
                    tri_Cnt_des--;
                }

            }


            for (int a = (Start / 5); a < (End / 5); a++)
            {
                for (int j = 20; j < 250; j++)
                {

                    CvInvoke.cvLine(image, new Point((a * 5), Pool_Y[a]), new Point((a * 5 + 4), Pool_Y[a]), new MCvScalar(175, 215, 255), 2, LINE_TYPE.FOUR_CONNECTED, 0);

                }
            }

            for (int a = (20 / 5); a < (450 / 5) - 1; a++)
            {

                if (Pool[a] > Pool[a - 1] && Pool[a] != 0 && Pool[a - 1] != 0)
                {
                    CvInvoke.cvLine(image, new Point(((a - 1) * 5) + 3, Pool_Y[a - 1]), new Point((a * 5) + 3, Pool_Y[a]), new MCvScalar(5, 215, 5), 1, LINE_TYPE.FOUR_CONNECTED, 0);
                    tri_Cnt2++;
                }
                else
                {
                    tri_Cnt2_des--;
                }

            }

            for (int a = (Start / 5); a < (End / 5); a++)
            {
                int up = Math.Abs(Pool[a] + 115);
                int down = Math.Abs(Pool_Y[a] - 115);

                double result = Math.Abs(up - down);
                //result = Math.Sqrt(result);

                asymmetric2 += result;
            }


            imageBox4.Image = image;

            label1.Text = "Score  Tri1 : " + tri_Cnt + " " + tri_Cnt_des + " Tri2 : " + tri_Cnt2 + " " + tri_Cnt2_des + " Asymm : " + asymmetric + " Asymm2 : " + asymmetric2;
        }

        private void imageBox4_MouseUp(object sender, MouseEventArgs e)
        {
            Console.WriteLine(e.X + " " + e.Y + " " + image.Data[e.Y, e.X, 2] + " " + image.Data[e.Y, e.X, 1] + " " + image.Data[e.Y, e.X, 0] + " " );
        }


    }
}
