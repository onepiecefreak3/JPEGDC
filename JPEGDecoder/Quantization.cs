using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPEGDecoder
{
    public class Quantization
    {
        public static short[] DeQuant(short[] input, byte[] table)
        {
            var result = new short[64];

            for (int i = 0; i < 64; i++)
            {
                result[i] = (short)(input[i]*table[i]);
            }

            return result;
        }
    }
}
