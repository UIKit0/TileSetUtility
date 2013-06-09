using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Xml;
using Ionic.Zlib;
using Photoshop;
using TileSetUtility.Properties;

namespace TileSetUtility
{
    [System.ComponentModel.DesignerCategory("")]
    public class TileSetUtilityForm : Form
    {
        #region Private fields

        //graphical interface
        private TableLayoutPanel _tableLayoutPanel;
        private FlowLayoutPanel _flowLayoutPanel;
        private Button _browseButton;
        private TextBox _pathTextBox;
        private Button _generateButton;
        private Label _tileNameLabel;
        private TextBox _tileNameTextBox;
        private Label _tileSizeLabel;
        private NumericUpDown _tileSizeNumericUpDown;
        private TextBox _xmlTextBox;

        private string _psdFileName;
        private string _pathName;

        #endregion

       #region Constructors

        public TileSetUtilityForm()
        {
            this.Load += new EventHandler(TileSetUtilityForm_Load);
            Application.ApplicationExit += new EventHandler(Application_ApplicationExit);

            this._tableLayoutPanel = new TableLayoutPanel();
            this._flowLayoutPanel = new FlowLayoutPanel();
            this._browseButton = new Button();
            this._pathTextBox = new TextBox();
            this._generateButton = new Button();
            this._tileNameLabel = new Label();
            this._tileNameTextBox = new TextBox();
            this._tileSizeLabel = new Label();
            this._tileSizeNumericUpDown = new NumericUpDown();
            this._xmlTextBox = new TextBox();
        }

        #endregion

        #region Methods


        void CreateTiles()
        {
            /*
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

            PsdFile test = new PsdFile();

            test.Load("C:\\Users\\mtvilim\\Documents\\FireAndIce-iOS\\Assets\\TileMaps\\PackThemBags\\test.psd");

            Bitmap bmp = ImageDecoder.DecodeImage(test.Layers[1]);

            bmp.Save("C:\\Users\\mtvilim\\Documents\\FireAndIce-iOS\\Assets\\TileMaps\\PackThemBags\\test.png", ImageFormat.Png);

            */
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

        void TileSetUtilityForm_Load(object sender, EventArgs e)
        {
            this.SuspendLayout();
            this._flowLayoutPanel.SuspendLayout();

            //set form properties
            this.Text = "TileSetUtility";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.ShowIcon = false;
            this.MaximizeBox = false;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            //add controls
            this.Controls.Add(this._tableLayoutPanel);
            
            //table layout panel
            this._tableLayoutPanel.Controls.Add(this._flowLayoutPanel);

            this._flowLayoutPanel.Controls.Add(this._browseButton);
            this._flowLayoutPanel.Controls.Add(this._pathTextBox);
            this._flowLayoutPanel.Controls.Add(this._tileNameLabel);
            this._flowLayoutPanel.Controls.Add(this._tileNameTextBox);
            this._flowLayoutPanel.Controls.Add(this._generateButton);
            //break
            this._flowLayoutPanel.SetFlowBreak(this._generateButton, true);
            this._flowLayoutPanel.Controls.Add(this._tileSizeLabel);
            this._flowLayoutPanel.Controls.Add(this._tileSizeNumericUpDown);
            //break
            this._tableLayoutPanel.Controls.Add(this._xmlTextBox);

            //table layout panel
            this._tableLayoutPanel.AutoSize = true;
            this._tableLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this._tableLayoutPanel.GrowStyle = TableLayoutPanelGrowStyle.AddRows;

            //flow layout panel
            this._flowLayoutPanel.AutoSize = true;
            this._flowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            //browse button
            this._browseButton.Text = "Browse";
            this._browseButton.Anchor = AnchorStyles.Left;
            this._browseButton.Click += new EventHandler(this.browseButton_Click);

            //path text box
            this._pathTextBox.Anchor = AnchorStyles.Left;
            this._pathTextBox.Width = 300;

            //generate button
            this._generateButton.Text = "Generate";
            this._generateButton.Anchor = AnchorStyles.Left;
            this._generateButton.Click += new EventHandler(this.generateButton_Click);

            //tile name label
            this._tileNameLabel.Text = "Tile name";
            this._tileNameLabel.Anchor = AnchorStyles.Left;
            this._tileNameLabel.AutoSize = true;
            this._tileNameLabel.TextAlign = ContentAlignment.MiddleLeft;

            //tile name textbox
            this._tileNameTextBox.Anchor = AnchorStyles.Left;

            //tile size label
            this._tileSizeLabel.Text = "Tile size (px)";
            this._tileSizeLabel.Anchor = AnchorStyles.Left;
            this._tileSizeLabel.AutoSize = true;
            this._tileSizeLabel.TextAlign = ContentAlignment.MiddleLeft;

            //tile size numeric up down
            this._tileSizeNumericUpDown.Anchor = AnchorStyles.Left;

            //xml text box
            this._xmlTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
          //  this._xmlTextBox.Dock = DockStyle.Fill;
            this._xmlTextBox.ReadOnly = true;
            this._xmlTextBox.Multiline = true;
            this._xmlTextBox.AutoSize = true;
            this._xmlTextBox.Text = "testdt\n\nksdfkdkfsdk";
            this._xmlTextBox.Height = 400;

            this.ResumeLayout();
            this._flowLayoutPanel.ResumeLayout();
        }

        void Application_ApplicationExit(object sender, EventArgs e)
        {
            Settings.Default.Save();
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Photoshop file|*.psd";

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                this._psdFileName = fileDialog.FileName;
                this._pathName = Path.GetDirectoryName(fileDialog.FileName);
            }
        }

        private void generateButton_Click(object sender, EventArgs e)
        {
            this.CreateTiles();
        }

        #endregion
    }
}
