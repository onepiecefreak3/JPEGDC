using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPEGDecoder
{
    public class Huffman
    {
        public static List<short[]> YBlocks=new List<short[]>();
        public static List<short[]> CbBlocks=new List<short[]>();
        public static List<short[]> CrBlocks= new List<short[]>();

        public static List<Dictionary<BitArray, int>> dcValues;
        public static void DCValuesInit()
        {
            dcValues = new List<Dictionary<BitArray, int>>();
            for (int i=0;i<0xc;i++)
            {
                int tabVal = (int)Math.Pow(2, i) - 1;
                dcValues.Add(new Dictionary<BitArray, int>());

                if (i>0)
                {
                    for (short j=0;j<Math.Pow(2,i);j++)
                    {
                        if (j >= Math.Pow(2, i) / 2)
                            dcValues[i].Add(GetBits(j, i), ((tabVal + 1) / 2) + j - (int)Math.Pow(2, i) / 2);
                        else
                            dcValues[i].Add(GetBits(j, i), (tabVal - j) * -1);
                    }
                }
            }
        }

        public static Dictionary<BitArray,int> GetTree(List<KeyValuePair<byte,byte>> values)
        {
            var result = new Dictionary<BitArray, int>();
            short value = 0;
            byte size = 1;

            foreach (var val in values)
            {
                while (size != val.Value) {
                    size++;
                    value <<= 1;
                }

                result.Add(GetBits(value, val.Value), val.Key);
                value++;
            }

            return result;
        }

        public static BitArray GetBits(short inp, int size)
        {
            var result = new BitArray(size);
            for (int i=size-1;i>=0;i--)
            {
                if (inp % 2 == 0)
                    result[i] = false;
                else
                    result[i] = true;
                inp /= 2;
            }
            return result;
        }

        public static void Decomp(byte[] data, ScanDetails details, Dictionary<BitArray,int>[] tables)
        {
            BitArray input = Support.BitArrayReverse(new BitArray(data));
            int comp = 0;
            int acPos = 1;
            List<bool> compare=new List<bool>();
            bool dc = true;

            for(int i=0; i<input.Length;i++)
            {
                compare.Add(input[i]);
                var compTmp = new BitArray(compare.ToArray());

                if (dc)
                {
                    int keyId = BitArrayCompare(tables[details.comps[comp].dc], compTmp);
                    if (keyId!=-1)
                    {
                        int length = tables[details.comps[comp].dc].ElementAt(keyId).Value;
                        var bitTmp = new BitArray(length);
                        for (int j = 0; j < length; j++) bitTmp[j] = input[(i++)+1];
                        var keyTmp = BitArrayCompare(dcValues[length], bitTmp);

                        if (details.comps[comp].id == 1)
                        {
                            YBlocks.Add(new short[64]);
                            if (keyTmp == -1)
                                YBlocks[YBlocks.Count() - 1][0] = 0;
                            else
                                YBlocks[YBlocks.Count() - 1][0] = (short)dcValues[length].ElementAt(keyTmp).Value;
                        } else if (details.comps[comp].id == 2)
                        {
                            CbBlocks.Add(new short[64]);
                            if (keyTmp == -1)
                                CbBlocks[CbBlocks.Count() - 1][0] = 0;
                            else
                                CbBlocks[CbBlocks.Count() - 1][0] = (short)dcValues[length].ElementAt(keyTmp).Value;
                        } else if (details.comps[comp].id == 3)
                        {
                            CrBlocks.Add(new short[64]);
                            if (keyTmp == -1)
                                CrBlocks[CrBlocks.Count() - 1][0] = 0;
                            else
                                CrBlocks[CrBlocks.Count() - 1][0] = (short)dcValues[length].ElementAt(keyTmp).Value;
                        }
                        dc = false;
                        compare = new List<bool>();
                    }
                } else
                {
                    int keyId = BitArrayCompare(tables[details.comps[comp].ac+2], compTmp);
                    if (keyId != -1)
                    {
                        if (tables[details.comps[comp].ac+2].ElementAt(keyId).Value != 0)
                        {

                        } else
                        {
                            dc = true;
                            acPos = 1;
                            if (comp+1==details.nrOfComps)
                            {
                                comp = 0;
                            } else
                            {
                                comp++;
                            }
                        }
                        compare = new List<bool>();
                    }
                }
            }
        }

        public static int BitArrayCompare(Dictionary<BitArray,int> dict, BitArray toCompare)
        {
            int result = -1;

            int count = 0;
            foreach(var entry in dict)
            {
                if (entry.Key.Length > toCompare.Length) break;
                if (entry.Key.Length==toCompare.Length)
                {
                    bool same = true;
                    for(int i=0;i<entry.Key.Length;i++)
                    {
                        if (entry.Key[i] != toCompare[i])
                        {
                            same = false;
                            break;
                        }
                    }
                    if (same)
                    {
                        result = count;
                        break;
                    }
                }
                count++;
            }

            return result;
        }
    } 
}
