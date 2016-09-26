using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proco
{
    class WaveProcessor
    {

        byte[] waves;
        byte[] poolingData;

        public WaveProcessor(byte[] buffer)
        {
            waves = new byte[buffer.Length];

            for (int i = 0; i < buffer.Length; i++)
            {
                waves[i] = buffer[i];
            }
        }

        public byte[] MaxPooling(int PoolLength)
        {
            poolingData = new byte[waves.Length / PoolLength];

            for (int i = 0; i < poolingData.Length; i++)
            {
                byte Max = 0;

                for (int k = 0; k < PoolLength; k++)
                {
                    if (waves[i * PoolLength + k] > Max)
                    {
                        Max = waves[i * PoolLength + k];
                    }
                }

                poolingData[i] = Max;
            }

            return poolingData;
        }



    }
}
