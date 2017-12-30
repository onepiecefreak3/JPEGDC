using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPEGDecoder
{
    class DCT
    {
        //public static short[] test = new short[] { -76,-73,-67,-62,-58,-67,-64,-55,-65,-69,-73,-38,-19,-43,-59,-56,-66,-69,-60,-15,16,-24,-62,-55,-65,-70,-57,-6,26,-22,-58,-59,-61,-67,-60,-24,-2,-40,-60,-58,-49,-63,-68,-58,-51,-60,-70,-53,-43,-57,-64,-69,-73,-67,-63,-45,-41,-49,-59,-60,-63,-52,-50,-34};
        //public static double[] test = new double[] { -415.38, -30.19, -61.20, 27.24, 56.12, -20.10, -2.39, 0.46, 4.47, -21.86, -60.76, 10.25, 13.15, -7.09, -8.54, 4.88, -46.83, 7.37, 77.13, -24.56, -28.91, 9.93, 5.42, -5.65, -48.53, 12.07, 34.10, -14.76, -10.24, 6.30, 1.83, 1.95, 12.12, -6.55, -13.20, -3.95, -1.87, 1.75, -2.79, 3.14, -7.73, 2.91, 2.38, -5.94, -2.38, 0.94, 4.30, 1.85, -1.03, 0.18, 0.42, -2.42, -0.88, -3.02, 4.12, -0.66, -0.17, 0.14, -1.07, -4.19, -1.17, -0.10, 0.50, 1.68 };

        public static List<double[]> cos;
        public static short[] DeTransformation(short[] block)
        {
            short[] result=new short[64];
            double sqrtVal = 1 / Math.Sqrt(2);

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    double tmpRes = 0;

                    for (int i = 0; i < 8; i++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            var ci = (i == 0) ? sqrtVal : 1;
                            var cj = (j == 0) ? sqrtVal : 1;
                            tmpRes += ci * cj * block[i * 8 + j] * cos[x*8+y][i*8+j];
                        }
                    }

                    result[x * 8 + y] = (short)(0.25 * Math.Round(tmpRes));
                }
            }

            return result;
        }

        public static void CalculateCosines()
        {
            cos = new List<double[]>();

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    cos.Add(new double[64]);
                    for (int i = 0; i < 8; i++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            cos[cos.Count()-1][i*8+j]= Math.Cos(((2 * x + 1) * i * Math.PI) / 16) * Math.Cos(((2 * y + 1) * j * Math.PI) / 16);
                        }
                    }
                }
            }
        }
    }
}
