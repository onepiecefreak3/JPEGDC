using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace JPEGDecoder
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class AppSpecifics
    {
        public AppSpecifics(Stream input)
        {
            using (var br=new BinaryReaderX(input,true,ByteOrder.BigEndian))
            {
                comment = br.ReadCStringA();
                br.ReadBytes(3);
                densityWidth = br.ReadInt16();
                densityHeight = br.ReadInt16();
                thumbWidth = br.ReadByte();
                thumbHeight = br.ReadByte();
            }
        }

        public string comment;
        public short densityWidth;
        public short densityHeight;
        public byte thumbWidth;
        public byte thumbHeight;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ImageInfo
    {
        public ImageInfo(Stream input)
        {
            using (var br=new BinaryReaderX(input,true, ByteOrder.BigEndian))
            {
                prec = br.ReadByte();
                nrOfLines = br.ReadInt16();
                nrOfSamples = br.ReadInt16();
                nrOfComp = br.ReadByte();
                comps = new Component[nrOfComp];
                for (int i = 0; i < nrOfComp; i++) comps[i] = br.ReadStruct<Component>();
            }
        }

        public byte prec;
        public short nrOfLines;
        public short nrOfSamples;
        public byte nrOfComp;
        Component[] comps;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Component
        {
            public byte id;
            public byte sampFac;
            public byte quantTableId;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ScanDetails
    {
        public ScanDetails(Stream input)
        {
            using (var br=new BinaryReaderX(input,true, ByteOrder.BigEndian))
            {
                nrOfComps = br.ReadByte();
                comps = new Component[nrOfComps];
                for (int i = 0; i < nrOfComps; i++) comps[i] = new Component(br.BaseStream);

                spectralBegin = br.ReadByte();
                spectralEnd = br.ReadByte();
                sucApprox = br.ReadByte();
            }
        }

        public byte nrOfComps;
        public Component[] comps;
        public byte spectralBegin;
        public byte spectralEnd;
        public byte sucApprox;
        
        public class Component
        {
            public Component(Stream input)
            {
                using (var br = new BinaryReaderX(input, true))
                {
                    id = br.ReadByte();
                    var tmp = br.ReadByte();
                    dc = (byte)(tmp >> 4);
                    ac = (byte)(tmp & 0x0F);
                }
            }

            public byte id;
            public byte dc;
            public byte ac;
        }
    }

    public class Support
    {
        public static BitArray BitArrayReverse(BitArray input)
        {
            int count = input.Count / 8;
            for (int i=0;i<count;i++)
            {
                for (int j=0;j<4;j++)
                {
                    var help = input[i * 8 + j];
                    input[i * 8 + j] = input[i * 8 + (7 - j)];
                    input[i * 8 + (7 - j)] = help;
                }
            }

            return input;
        }

        public static short[] Shift128(short[] block, bool plus = false)
        {
            short[] result = new short[64];

            if (plus)
            {
                for (int i=0;i<64;i++)
                {
                    result[i] = (block[i] + 128 > 255) ? (short)255 : (block[i] + 128 < 0) ? (short)0 : (short)(block[i] + 128);
                }
            } else
            {
                for (int i = 0; i < 64; i++)
                {
                    result[i] = (short)(block[i] - 128);
                }
            }

            return result;
        }

        private static byte[] shuffle = new byte[] { 0, 1, 8, 16, 9, 2, 3, 10, 17, 24, 32, 25, 18, 11, 4, 5, 12, 19, 26, 33, 40, 48, 41, 34, 27, 20, 13, 6, 7, 14, 21, 28, 35, 42, 49, 56, 57, 50, 43, 36, 29, 22, 15, 23, 30, 37, 44, 51, 58, 59, 52, 45, 38, 31, 39, 46, 53, 60, 61, 54, 47, 55, 62, 63 };
        public static byte[] DeZigZag(byte[] input)
        {
            var result = new byte[64];

            for (int i = 0; i < 64; i++)
            {
                result[shuffle[i]] = input[i];
            }

            return result;
        }
        public static short[] DeZigZag(short[] input)
        {
            var result = new short[64];

            for (int i = 0; i < 64; i++)
            {
                result[shuffle[i]] = input[i];
            }

            return result;
        }
    } 
}
