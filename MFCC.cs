using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proco
{
    class MFCC
    {
        struct Value
        {
            public long smp_period;
            public long smp_freq;
            public int framesize;
            public int frameShift;
            public float preEmph;
            public int lifter;
            public int fbank_num;
            public int delWin;
            public int accWin;
            public float silFloor;
            public float escale;
            public int hipass;
            public int lopass;
            public int enormal;
            public int raw_e;
            public int zmeanframe;
            public int usepower;
            public float vtln_alpha;
            public float vtln_upper;
            public float vtln_lower;

            public int delta;
            public int acc;
            public int energy;
            public int c0;
            public int absesup;
            public int cmn;
            public int cvn;
            public int mfcc_dim;
            public int baselen;
            public int vecbuflen;
            public int veclen;

            public int loaded;
        };

        struct FBankInfo
        {
            public int fttN;
            public int n;
            public int klo;
            public int khi;
            public float fres;
            public float[] cf;
            public short[] loChan;
            public float[] loWt;
            public float[] Re;
            public float[] lm;
        };

        struct DeltaBuf
        {
            public float[,] mfcc;
            public int veclen;
            public float[] vec;
            public int win;
            public int len;
            public int sotre;
            public Boolean[] is_on;
            public int B;
        };

        struct MFCCWork
        {

            public float[] bf;
            public double[] fbank;
            public FBankInfo fb;
            public int bflen;
            public double[] costbl_hamming;
            public double[] costbl_fft;
            public double[] slntbl_fft;
            public double[] costbl_makemfcc;
            public double[] slntbl_wcep;
            public float sqrt2var;
            public float[] ssbuf;
            public float ss_floor;
            public float ss_alpha;

        }


        float[] wave;
        int frameSize;

        int n;

        MFCCWork w;
        Value para;

        public MFCC()
        {

        }

        void ZMeanFrame()
        {
            float mean;

            mean = 0;

            for (int i = 0; i < frameSize; i++)
                mean += wave[i+1];

            mean /= frameSize;

            for (int i = 0; i < frameSize; i++)
                wave[i+1] -= mean;


        }

        float CalcLogRawE()
        {
            double raw_E = 0;
            float energy;

            for (int i = 0; i < frameSize; i++)
                raw_E += wave[i+1] * wave[i+1];

            energy = (float)Math.Log(raw_E);

            return energy;
        }

        void PreEmphasise(float preEmph)
        {
            for (int i = frameSize - 1; i >= 1; i--)
                wave[i+1] -= wave[i] * preEmph;

            wave[0] *= 1.0f - preEmph;
        }

        void make_costbl_hamming()
        {
            float a;

            w.costbl_hamming = new double[frameSize];
            a = 2.0f * (float)Math.PI / (float)frameSize;
            for (int i = 0; i < frameSize; i++)
            {
                w.costbl_hamming[i] = 0.54 - 0.46 * Math.Cos(a * (i));
            }
            
        }

        void Hamming() {
            
            for (int i = 0; i < frameSize; i++)
                wave[i+1] *= (float)w.costbl_hamming[i];

        }

        void make_fft_table()
        {
            int me, me1;

            w.costbl_fft = new double[n];
            w.slntbl_fft = new double[n];

            for (int m = 0; m < n; m++)
            {
                me = 1 << m;
                me1 = me / 2;

                w.costbl_fft[m] = Math.Cos(Math.PI / me1);
                w.slntbl_fft[m] = -Math.Sin(Math.PI / me1);
            }
            
        }

        void FFT(int p, float[] xRe, float[] xIm)
        {
            int ip, j, k, me, me1, n, nv2;
            double uRe, uIm, vRe, vIm, wRe, wIm, tRe, tIm;

            n = 1 << p;
            nv2 = n / 2;

            j = 0;
            for (int i = 0; i < n - 1; i++)
            {
                if(j > i){
                    tRe = xRe[j]; tIm = xIm[j];
                    xRe[j] = xRe[i]; xIm[j] = xIm[i];
                    xRe[i] = (float)tRe; xIm[i] = (float)tIm;
                }

                k = nv2;
                while (j >= k)
                {
                    j -= k; k /= 2;
                }
                j += k;
            }

            for (int m = 0; m < p; m++)
            {
                me = 1 << m; me1 = me / 2;
                uRe = 1.0; uIm = 0;

                wRe = w.costbl_fft[m]; wIm = w.slntbl_fft[m];

                for(j = 0; j < me1; j++){
                    for (int i = j; i < n; i += me )
                    {
                        ip = i * me1;
                        tRe = xRe[ip] * uRe - xIm[ip] * uIm;
                        tIm = xRe[ip] * uIm + xIm[ip] * uRe;
                        xRe[ip] = xRe[i] - (float)tRe; xIm[ip] = xIm[i] - (float)tIm;
                        xRe[i] += (float)tRe; xIm[i] += (float)tIm;
                    }
                    vRe = uRe * wRe - uIm * wIm; vIm = uRe * wIm + uIm * wRe;
                    uRe = vRe; uIm = vIm;
                }
            }
        }

        void MakeFBank()
        {
            int bin;
            double Re, Im, A, P, NP, H, temp;

            for (int k = 0; k < frameSize; k++)
            {
                w.fb.Re[k] = wave[k]; w.fb.lm[k] = 0;
            }
            for (int k = frameSize - 1; k <= w.fb.fttN; k++)
            {
                w.fb.Re[k] = 0; w.fb.lm[k] = 0;
            }

            FFT(n, w.fb.Re, w.fb.lm);

            if( w.ssbuf != null ){
                for (int k = 0; k <= w.fb.fttN; k++)
                {
                    Re = w.fb.Re[k]; Im = w.fb.lm[k];
                    P = Math.Sqrt(Re * Re + Im * Im);
                    NP = w.ssbuf[k];

                    if ((P * P - w.ss_alpha * NP * NP) < 0)
                    {
                        H = w.ss_floor;
                    }
                    else
                    {
                        H = Math.Sqrt(P * P - w.ss_alpha * NP * NP) / P;
                    }
                    w.fb.Re[k - 1] = (float)H * (float)Re;
                    w.fb.lm[k - 1] = (float)H * (float)Im;
                }
            }

            for (int i = 0; i < para.fbank_num; i++)
                w.fbank[i + 1] = 0;

            if (para.usepower == 1)
            {
                for (int k = w.fb.klo; k <= w.fb.khi; k++ )
                {
                    Re = w.fb.Re[k]; Im = w.fb.lm[k];
                    A = Re * Re + Im * Im;
                    bin = w.fb.loChan[k];
                    Re = w.fb.loWt[k] * A;
                    if (bin > 0) w.fbank[bin] += Re;
                    if (bin < para.fbank_num) w.fbank[bin + 1] += A - Re;
                }
            }
            else
            {
                for (int k = w.fb.klo; k <= w.fb.khi; k++)
                {
                    Re = w.fb.Re[k]; Im = w.fb.lm[k];
                    A = Math.Sqrt(Re * Re + Im * Im);
                    bin = w.fb.loChan[k];
                    Re = w.fb.loWt[k] * A;
                    if (bin > 0) w.fbank[bin] += Re;
                    if (bin < para.fbank_num) w.fbank[bin + 1] += A - Re;
                }
            }

            for (bin = 0; bin < para.fbank_num; bin++)
            {
                temp = w.fbank[bin];
                if (temp < 1.0) temp = 1.0;
                w.fbank[bin] = Math.Log(temp); 
            }

        }

        float CalcC0()
        {
            float S;
            S = 0;

            for (int i = 0; i < para.fbank_num; i++)
            {
                S += (float)w.fbank[i];
            }

            return S * w.sqrt2var;
        }

        void MakeMFCC(float[] mfcc)
        {
            int k = 0;
            for (int i = 0; i < para.mfcc_dim; i++)
            {
                mfcc[i] = 0;
                for (int j = 0; j < para.fbank_num; j++)
                    mfcc[i] += (float)w.fbank[j] * (float)w.costbl_makemfcc[k++];

                mfcc[i] *= w.sqrt2var;

            }
        }

        float Mel(int k, float fres)
        {
            return (1127 * (float)Math.Log(1 + (k - 1) * fres));
        }

        Boolean VTLN_recreate_fbank_cf(float[] cf, float mlo, float mhi, int maxChan)
        {
            int chan;
            float minf, maxf, cf_orig, cf_new;
            float scale, cu, cl, au, al;

            minf = 700 * ((float)Math.Exp(mlo / 1127) - 1);
            maxf = 700 * ((float)Math.Exp(mhi / 1127) - 1);

            scale = 1 / ((float)para.vtln_alpha);
            cu = para.vtln_upper * 2 / (1 + scale);
            cl = para.vtln_lower * 2 / (1 + scale);
            au = (maxf - cu * scale) / (maxf - cu);
            al = (cl * scale - minf) / (cl - minf);

            for (chan = 1; chan <= maxChan; chan++)
            {
                cf_orig = 700 * ((float)Math.Exp(cf[chan] / 1127) - 1);
                if( cf_orig > cu ){
                    cf_new = au * (cf_orig - cu) + scale * cu;
                }else if ( cf_orig < cl){
                    cf_new = al * (cf_orig - minf) + minf;
                }else{
                    cf_new = scale * cf_orig;
                }

                cf[chan] = 1127 * (float)Math.Log(1 + cf_new / 700);
            }

            return true;
        }

        

        void WeightCepstrum(float[] mfcc)
        {
            for (int i = 0; i < para.mfcc_dim; i++)
            {
                mfcc[i] *= (float)w.slntbl_wcep[i];
            }
        }

    }
}
