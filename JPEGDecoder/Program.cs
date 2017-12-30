using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

/**documentations used:
 * https://en.wikipedia.org/wiki/JPEG
 * http://www.impulseadventure.com/photo/jpeg-huffman-coding.html
 * http://www.roman10.net/2011/08/09/jpeg-standarda-tutorial-based-on-analysis-of-sample-picturepart-1-coding-of-a-8x8-block/
 * https://en.wikipedia.org/wiki/Discrete_cosine_transform
 * https://en.wikipedia.org/wiki/YCbCr
 * https://books.google.de/books?id=c9OoCAAAQBAJ&pg=PA279&lpg=PA279&dq=JPEG+IDCT+c%23&source=bl&ots=sFT8bt-0bW&sig=nlaP0c8RKhKlewAWiVGXxLlU6eQ&hl=de&sa=X&redir_esc=y#v=onepage&q=JPEG%20IDCT%20c%23&f=false
 **/

namespace JPEGDecoder
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Count() > 0)
            {
                var filename = args[0];

                using (var br = new BinaryReaderX(File.OpenRead(filename), ByteOrder.BigEndian))
                {
                    Huffman.DCValuesInit();

                    bool decode = false;

                    List<AppSpecifics> appSpecifics=new List<AppSpecifics>();
                    string comment;

                    var quantTable = new List<byte[]>();
                    var huffTable = new Dictionary<BitArray,int>[4];
                    var scanDetails = new List<ScanDetails>();

                    var scanData = new List<byte[]>();

                    ImageInfo imageInfo=null;

                    while (br.BaseStream.Position<br.BaseStream.Length)
                    {
                        ushort size;
                        Huffman.DCValuesInit();

                        switch (br.ReadUInt16())
                        {
                            //JPEG Image start
                            case 0xffd8:
                                break;
                            //App Specifications
                            case 0xffe0:
                            case 0xffe1:
                            case 0xffe2:
                            case 0xffe3:
                            case 0xffe4:
                            case 0xffe5:
                            case 0xffe6:
                            case 0xffe7:
                            case 0xffe8:
                            case 0xffe9:
                            case 0xffea:
                            case 0xffeb:
                            case 0xffec:
                            case 0xffed:
                            case 0xffee:
                            case 0xffef:
                                size = br.ReadUInt16();
                                appSpecifics.Add(new AppSpecifics(br.BaseStream));
                                break;
                            //comment
                            case 0xfffe:
                                size = br.ReadUInt16();
                                comment = br.ReadString(size - 2);
                                break;
                            //Quantization tables
                            case 0xffdb:
                                size = br.ReadUInt16();
                                for (int i = 0; i < (size - 2) / 0x40; i++)
                                {
                                    br.ReadByte();
                                    quantTable.Add(br.ReadBytes(0x40));
                                    quantTable[quantTable.Count() - 1] = Support.DeZigZag(quantTable.Last());
                                }
                                break;
                            //sequential JPG
                            case 0xffc0:
                            //progressive JPG
                            case 0xffc2:
                                size = br.ReadUInt16();
                                imageInfo = new ImageInfo(br.BaseStream);
                                break;
                            //Huffman Table
                            case 0xffc4:
                                long bk = br.BaseStream.Position;
                                size = br.ReadUInt16();
                                while (br.BaseStream.Position < bk + size)
                                {
                                    var id = br.ReadByte();
                                    var tmpDir = new Dictionary<byte, byte>();
                                    var bytes = br.ReadBytes(0x10);
                                    byte byteCount = 1;
                                    foreach (var part in bytes)
                                    {
                                        for (int i = 0; i < part; i++)
                                            tmpDir.Add(br.ReadByte(), byteCount);
                                        byteCount++;
                                    }
                                    huffTable[(id >> 4) * 2 + id & 0x0F] = Huffman.GetTree(tmpDir.OrderBy(e => e.Value).ToList());
                                }

                                if (decode)
                                    Huffman.Decomp(scanData.Last(), scanDetails.Last(), huffTable);
                                decode = false;
                                break;
                            //start of scan
                            case 0xffda:
                                size = br.ReadUInt16();
                                scanDetails.Add(new ScanDetails(br.BaseStream));
                                var byteTmp = new List<byte>();

                                var tmp = br.ReadUInt16();
                                while(tmp<=0xff00)
                                {
                                    if (tmp==0xff00)
                                    {
                                        byteTmp.Add(0xFF);
                                    } else
                                    {
                                        br.BaseStream.Position -= 1;
                                        byteTmp.Add((byte)(tmp>>8));
                                    }

                                    tmp = br.ReadUInt16();
                                }
                                scanData.Add(byteTmp.ToArray());

                                if (tmp == 0xffc4)
                                    decode = true;
                                else
                                    Huffman.Decomp(scanData.Last(), scanDetails.Last(), huffTable);

                                br.BaseStream.Position -= 2;
                                break;
                            //End of Image
                            case 0xffd9:
                                if (decode)
                                    Huffman.Decomp(scanData.Last(), scanDetails.Last(), huffTable);
                                break;
                        }
                    }

                    //After Huffman
                    DCT.CalculateCosines();

                    for (int i = 0; i < Huffman.YBlocks.Count(); i++)
                    {
                        //DeZigZag
                        Huffman.YBlocks[i] = Support.DeZigZag(Huffman.YBlocks[i]);

                        //Dequantization
                        Huffman.YBlocks[i] = Quantization.DeQuant(Huffman.YBlocks[i], quantTable[0]);

                        //IDCT
                        Huffman.YBlocks[i] = DCT.DeTransformation(Huffman.YBlocks[i]);

                        //+128 Shift
                        Huffman.YBlocks[i] = Support.Shift128(Huffman.YBlocks[i], true);
                    }
                    for (int i = 0; i < Huffman.CbBlocks.Count(); i++)
                    {
                        //DeZigZag
                        Huffman.CbBlocks[i] = Support.DeZigZag(Huffman.CbBlocks[i]);

                        //Dequantization
                        Huffman.CbBlocks[i] = Quantization.DeQuant(Huffman.CbBlocks[i], quantTable[1]);

                        //IDCT
                        Huffman.CbBlocks[i] = DCT.DeTransformation(Huffman.CbBlocks[i]);

                        //+128 Shift
                        Huffman.CbBlocks[i] = Support.Shift128(Huffman.CbBlocks[i], true);
                    }
                    for (int i = 0; i < Huffman.CrBlocks.Count(); i++)
                    {
                        //DeZigZag
                        Huffman.CrBlocks[i] = Support.DeZigZag(Huffman.CrBlocks[i]);

                        //Dequantization
                        Huffman.CrBlocks[i] = Quantization.DeQuant(Huffman.CrBlocks[i], quantTable[1]);

                        //IDCT
                        Huffman.CrBlocks[i] = DCT.DeTransformation(Huffman.CrBlocks[i]);

                        //+128 Shift
                        Huffman.CrBlocks[i] = Support.Shift128(Huffman.CrBlocks[i], true);
                    }

                    //Create Bitmap
                    Bitmap bmp = new Bitmap(imageInfo.nrOfSamples, imageInfo.nrOfLines);

                    int x = 0, y = 0;
                    for (int i=0;i<Huffman.YBlocks.Count();i++)
                    {
                        for (int j=0;j<8;j++)
                        {
                            for (int k = 0; k < 8; k++)
                            {
                                var r = Huffman.YBlocks[i][j * 8 + k] + 1.402 * (Huffman.CrBlocks[i][j * 8 + k] - 128);
                                var g = Huffman.YBlocks[i][j * 8 + k] - 0.344136 * (Huffman.CbBlocks[i][j * 8 + k] - 128) - 0.714136 * (Huffman.CrBlocks[i][j * 8 + k] - 128);
                                var b = Huffman.YBlocks[i][j * 8 + k]+1.772 * (Huffman.CbBlocks[i][j * 8 + k] - 128);
                                bmp.SetPixel(x + k, y + j, Color.FromArgb((int)Math.Round(r), (int)Math.Round(g), (int)Math.Round(b)));
                            }
                        }
                        if (x + 8 == imageInfo.nrOfSamples)
                        {
                            x = 0;
                            y += 8;
                        }
                        else
                            x += 8;
                    }

                    bmp.Save("test.bmp");
                }
            }
        }
    }
}
