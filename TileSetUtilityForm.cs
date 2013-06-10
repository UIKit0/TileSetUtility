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
        private Label _tilePrefixLabel;
        private TextBox _tilePrefixTextBox;
        private Label _tileSizeLabel;
        private NumericUpDown _tileSizeNumericUpDown;
        private TextBox _xmlTextBox;
        private StatusStrip _statusStrip;

        #endregion

        #region Constructors

        public TileSetUtilityForm()
        {
            this.Load += new EventHandler(TileSetUtilityForm_Load);
            this.FormClosing += new FormClosingEventHandler(TileSetUtilityForm_FormClosing);

            this._tableLayoutPanel = new TableLayoutPanel();
            this._flowLayoutPanel = new FlowLayoutPanel();
            this._browseButton = new Button();
            this._pathTextBox = new TextBox();
            this._generateButton = new Button();
            this._tilePrefixLabel = new Label();
            this._tilePrefixTextBox = new TextBox();
            this._tileSizeLabel = new Label();
            this._tileSizeNumericUpDown = new NumericUpDown();
            this._xmlTextBox = new TextBox();
            this._statusStrip = new StatusStrip();
        }

        #endregion

        #region Methods
    
        void GenerateTiles()
        {
            string psdPath = Path.GetDirectoryName(this._pathTextBox.Text);
            PsdFile psdFile = new PsdFile();
            psdFile.Load(this._pathTextBox.Text);

            List<Bitmap> uniqueTiles = new List<Bitmap>();
            Size tileSize = new Size((int)this._tileSizeNumericUpDown.Value, (int)this._tileSizeNumericUpDown.Value);

            foreach (Layer psdLayer in psdFile.Layers)
            {
                Bitmap layerBitmap = ImageDecoder.DecodeImage(psdLayer);
                for (int yCoordinate = 0; yCoordinate <= layerBitmap.Height - tileSize.Height; yCoordinate += tileSize.Height)
                {
                    for (int xCoordinate = 0; xCoordinate <= layerBitmap.Width - tileSize.Width; xCoordinate += tileSize.Width)
                    {
                        Rectangle tileRectangle = new Rectangle(xCoordinate, yCoordinate, tileSize.Width, tileSize.Height);
                        Bitmap newTile = layerBitmap.Clone(tileRectangle, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        
                        foreach (Bitmap existingTile in uniqueTiles)
                        {
                            if (!this.CompareBitmapEquality(newTile, existingTile))
                            {
                                uniqueTiles.Add(newTile);
                            }
                        }
                    }
                }
            }

            foreach (Layer psdLayer in psdFile.Layers)
            {
                XmlDocument xmlDocument = new XmlDocument();
                XmlElement layerNode = xmlDocument.CreateElement("layer");
                xmlDocument.AppendChild(layerNode);
                layerNode.SetAttribute("name", psdLayer.Name);
                layerNode.SetAttribute("width", ((int)(psdLayer.Rect.Width / tileSize.Width)).ToString());
                layerNode.SetAttribute("height", ((int)(psdLayer.Rect.Height / tileSize.Height)).ToString());

                XmlElement dataNode = xmlDocument.CreateElement("data");
                layerNode.AppendChild(dataNode);
                dataNode.SetAttribute("encoding", "base64");
                dataNode.SetAttribute("compression", "zlib");

                Bitmap layerBitmap = ImageDecoder.DecodeImage(psdLayer);


            }
                        
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
        }

        [DllImport("msvcrt.dll", CallingConvention=CallingConvention.Cdecl)]
        private static extern int memcmp(IntPtr bitmap1, IntPtr bitmap2, long count);
        protected bool CompareBitmapEquality(Bitmap bitmap1, Bitmap bitmap2)
        {
            if ((bitmap1 == null) != (bitmap2 == null)) return false;
            if (bitmap1.Size != bitmap2.Size) return false;

            BitmapData firstBitmapData = bitmap1.LockBits(new Rectangle(new Point(0, 0), bitmap1.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData secondBitmapData = bitmap2.LockBits(new Rectangle(new Point(0, 0), bitmap2.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                IntPtr bitmapData1Scan0 = firstBitmapData.Scan0;
                IntPtr bitmapData2Scan0 = secondBitmapData.Scan0;

                int stride = firstBitmapData.Stride;
                int len = stride * bitmap1.Height;

                return memcmp(bitmapData1Scan0, bitmapData2Scan0, len) == 0;
            }
            finally
            {
                bitmap1.UnlockBits(firstBitmapData);
                bitmap2.UnlockBits(secondBitmapData);
            }
        }

        private void UpdateGenerateEnable()
        {
            bool enableGenerate = !String.IsNullOrEmpty(this._pathTextBox.Text) && !String.IsNullOrEmpty(this._tilePrefixTextBox.Text);
            this._generateButton.Enabled = enableGenerate;
        }

        #endregion

        #region Event methods

        protected void TileSetUtilityForm_Load(object sender, EventArgs e)
        {
            this.SuspendLayout();
            this._tableLayoutPanel.SuspendLayout();
            this._flowLayoutPanel.SuspendLayout();

            //form
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

            //flow layout panel
            this._flowLayoutPanel.Controls.Add(this._browseButton);
            this._flowLayoutPanel.Controls.Add(this._pathTextBox);
            this._flowLayoutPanel.Controls.Add(this._tilePrefixLabel);
            this._flowLayoutPanel.Controls.Add(this._tilePrefixTextBox);
            this._flowLayoutPanel.Controls.Add(this._generateButton);
            this._flowLayoutPanel.SetFlowBreak(this._generateButton, true);
            this._flowLayoutPanel.Controls.Add(this._tileSizeLabel);
            this._flowLayoutPanel.Controls.Add(this._tileSizeNumericUpDown);
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
            this._browseButton.Click += new EventHandler(this.BrowseButton_Click);

            //path text box
            this._pathTextBox.TextChanged += new EventHandler(this.PathTextBox_TextChanged);
            this._pathTextBox.ReadOnly = true;
            this._pathTextBox.Anchor = AnchorStyles.Left;
            this._pathTextBox.Width = 300;
            this._pathTextBox.Text = Settings.Default.PsdPath;

            //generate button
            this._generateButton.Text = "Generate";
            this._generateButton.Anchor = AnchorStyles.Left;
            this._generateButton.Click += new EventHandler(this.GenerateButton_Click);

            //tile prefix label
            this._tilePrefixLabel.Text = "Tile prefix";
            this._tilePrefixLabel.Anchor = AnchorStyles.Left;
            this._tilePrefixLabel.AutoSize = true;
            this._tilePrefixLabel.TextAlign = ContentAlignment.MiddleLeft;

            //tile prefix textbox
            this._tilePrefixTextBox.TextChanged += new EventHandler(this.TilePrefixTextBox_TextChanged);
            this._tilePrefixTextBox.Anchor = AnchorStyles.Left;
            this._tilePrefixTextBox.Text = Settings.Default.TilePrefix;

            //tile size label
            this._tileSizeLabel.Text = "Tile size (px)";
            this._tileSizeLabel.Anchor = AnchorStyles.Left;
            this._tileSizeLabel.AutoSize = true;
            this._tileSizeLabel.TextAlign = ContentAlignment.MiddleLeft;

            //tile size numeric up down
            this._tileSizeNumericUpDown.Anchor = AnchorStyles.Left;
            this._tileSizeNumericUpDown.Value = Settings.Default.TileSize;
            this._tileSizeNumericUpDown.Minimum = 1;

            //xml text box
            this._xmlTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            this._xmlTextBox.Dock = DockStyle.Top;
            this._xmlTextBox.ReadOnly = true;
            this._xmlTextBox.Multiline = true;
            this._xmlTextBox.MaximumSize = new Size(Int32.MaxValue, 500);
            this._xmlTextBox.Visible = false;

            this.ResumeLayout();
            this._tableLayoutPanel.ResumeLayout();
            this._flowLayoutPanel.ResumeLayout();
        }

        protected void TileSetUtilityForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Default.Save();
        }

        protected void BrowseButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Photoshop file|*.psd";

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                this._pathTextBox.Text = fileDialog.FileName;
            }
        }

        protected void GenerateButton_Click(object sender, EventArgs e)
        {
            this.CreateTiles();
            Debug.WriteLine(this._tilePrefixTextBox.Text);
        }

        protected void TileSizeNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            Settings.Default.TileSize = (int)this._tileSizeNumericUpDown.Value;
        }

        protected void TilePrefixTextBox_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.TilePrefix = this._tilePrefixTextBox.Text;
            this.UpdateGenerateEnable();
        }

        protected void PathTextBox_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.PsdPath = this._pathTextBox.Text;
            this.UpdateGenerateEnable();
        }

        #endregion
    }
}