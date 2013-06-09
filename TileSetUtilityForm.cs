using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Xml;
using Ionic.Zlib;
using SimplePsd;

namespace TileSetUtility
{
    //[DllImport("msvcrt.dll")]
    public partial class TileSetUtilityForm : Form
    {
        #region Constructors

        public TileSetUtilityForm()
        {
            InitializeComponent();

            this.FileBrowser = new OpenFileDialog();
            this.FileBrowser.Filter = "Bitmap file|*.bmp";
        }

        #endregion

        #region Attributes

        private OpenFileDialog _fileBrowser;
        private string _savePath;
        private Bitmap _sourceBitmap;
        private List<Bitmap> _tileBitmaps;

        #endregion

        #region Methods

        void CreateTiles()
        {
            this.SourceBitmap = new Bitmap(this.SavePath);
            this.TileBitmaps = new List<Bitmap>();

            Size mapSize = this.SourceBitmap.Size;
            Rectangle tileArea = new Rectangle(0, 0, 32, 32);
            
            //create blank tile
            Bitmap blankTile = new Bitmap(tileArea.Width, tileArea.Height);
            using (Graphics gfx = Graphics.FromImage(blankTile))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(248, 18, 234)))
            {
                gfx.FillRectangle(brush, 0, 0, tileArea.Width, tileArea.Height);
                blankTile.MakeTransparent(Color.FromArgb(248, 18, 234));
            }
            
            XmlDocument xmlDocument = new XmlDocument();

            XmlElement dataNode = xmlDocument.CreateElement("data");
            xmlDocument.AppendChild(dataNode);
            
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            int imageCount = 1;
            for (int yCoordinate = 0; yCoordinate <= mapSize.Height - tileArea.Height; yCoordinate += tileArea.Width)
            {
                for (int xCoordinate = 0; xCoordinate <= mapSize.Width - tileArea.Width; xCoordinate += tileArea.Height)
                {
                    Rectangle copyArea = new Rectangle(xCoordinate, yCoordinate, tileArea.Width, tileArea.Height);

                    Bitmap tile = this.SourceBitmap.Clone(copyArea, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    tile.MakeTransparent(Color.FromArgb(248, 18, 234));

                    XmlElement tileNode = xmlDocument.CreateElement("tile");
                    dataNode.AppendChild(tileNode);

                    if (CompareMemCmp(blankTile, tile) == true)
                    {
                        tileNode.SetAttribute("gid", "0");
                        writer.Write(0);
                    }
                    else
                    {
                        bool alreadyExists = false;
                        foreach (Bitmap bitmap in this.TileBitmaps)
                        {
                            if (CompareMemCmp(bitmap, tile) == true)
                            {
                                alreadyExists = true;

                                tileNode.SetAttribute("gid", (this.TileBitmaps.IndexOf(bitmap) + 1).ToString());
                                writer.Write(this.TileBitmaps.IndexOf(bitmap) + 1);
                                break;
                            }
                        }
                        if (alreadyExists == false)
                        {
                            this.TileBitmaps.Add(tile);

                            tileNode.SetAttribute("gid", (this.TileBitmaps.IndexOf(tile) + 1).ToString());
                            writer.Write(this.TileBitmaps.IndexOf(tile) + 1);

                            string fullSavePath = Path.Combine(Path.GetDirectoryName(this.SavePath), this.nameTextBox.Text + imageCount.ToString("D3") + ".png");

                            tile.Save(fullSavePath, System.Drawing.Imaging.ImageFormat.Png);

                            imageCount++;
                        }
                    }
                }
                PsdFile test = new PsdFile();
                test.Load("test";
                Layer test2 = new Layer(test);
                LayerInfo test3 = new LayerInfo();
                La
            }
            
            byte[] compressed = ZlibStream.CompressBuffer(stream.ToArray());
            string final = Convert.ToBase64String(compressed);

            string zlibFileName = Path.Combine(Path.GetDirectoryName(this.SavePath), this.nameTextBox.Text + "_zlib.txt");

            using (StreamWriter outfile = new StreamWriter(zlibFileName))
            {
                outfile.Write(final);
            }
            
            //write xml data
            string xmlFileName = Path.Combine(Path.GetDirectoryName(this.SavePath), this.nameTextBox.Text + "_xml.txt");
            xmlDocument.Save(xmlFileName);
        }

        [DllImport("msvcrt.dll", CallingConvention=CallingConvention.Cdecl)]
        private static extern int memcmp(IntPtr b1, IntPtr b2, long count);

        public static bool CompareMemCmp(Bitmap b1, Bitmap b2)
        {
            if ((b1 == null) != (b2 == null)) return false;
            if (b1.Size != b2.Size) return false;

            var bd1 = b1.LockBits(new Rectangle(new Point(0, 0), b1.Size),  ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var bd2 = b2.LockBits(new Rectangle(new Point(0, 0), b2.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                IntPtr bd1scan0 = bd1.Scan0;
                IntPtr bd2scan0 = bd2.Scan0;

                int stride = bd1.Stride;
                int len = stride * b1.Height;

                return memcmp(bd1scan0, bd2scan0, len) == 0;
            }
            finally
            {
                b1.UnlockBits(bd1);
                b2.UnlockBits(bd2);
            }
        }

        #endregion

        #region Event methods

        private void browseButton_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = this.FileBrowser.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                this.SavePath = this.FileBrowser.FileName;
                this.generateTilesButton.Enabled = true;
            }
        }

        private void generateTilesButton_Click(object sender, EventArgs e)
        {
            this.CreateTiles();
        }

        #endregion

        #region Accessors

        public OpenFileDialog FileBrowser
        {
            get
            {
                return this._fileBrowser;
            }
            set
            {
                this._fileBrowser = value;
            }
        }

        public string SavePath
        {
            get
            {
                return this._savePath;
            }
            set
            {
                this.pathBox.Text = value;
                this._savePath = value;
            }
        }

        public Bitmap SourceBitmap
        {
            get
            {
                return this._sourceBitmap;
            }
            set
            {
                this._sourceBitmap = value;
            }
        }

        public List<Bitmap> TileBitmaps
        {
            get
            {
                return this._tileBitmaps;
            }
            set
            {
                this._tileBitmaps = value;
            }
        }


        #endregion

    }
}
