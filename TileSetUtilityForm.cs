using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;
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
        private StatusStrip _statusStrip;
        private ToolStripProgressBar _progressBar;
        private ToolStripLabel _statusLabel;

        private List<Bitmap> _uniqueTiles;

        #endregion

        #region Constructors

        public TileSetUtilityForm()
        {
            this._uniqueTiles = new List<Bitmap>();

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
            this._statusStrip = new StatusStrip();
            this._progressBar = new ToolStripProgressBar();
            this._statusLabel = new ToolStripLabel();
        }

        #endregion

        #region Methods

        private void GenerateTiles()
        {
            PsdFile psdFile = new PsdFile();
            psdFile.Load(this._pathTextBox.Text);
            Size tileSize = new Size((int)this._tileSizeNumericUpDown.Value, (int)this._tileSizeNumericUpDown.Value);

            this.FindUniqueTiles(psdFile, tileSize);
            this.WriteMapDataAndTiles(psdFile, tileSize);
        }

        private void FindUniqueTiles(PsdFile psdFile, Size tileSize)
        {
            //scan through all layers of photoshop file to find unique, non-transparent tiles
            foreach (Layer psdLayer in psdFile.Layers)
            {
                Bitmap layerBitmap = ImageDecoder.DecodeImage(psdLayer);
                for (int yCoordinate = 0; yCoordinate <= layerBitmap.Height - tileSize.Height; yCoordinate += tileSize.Height)
                {
                    for (int xCoordinate = 0; xCoordinate <= layerBitmap.Width - tileSize.Width; xCoordinate += tileSize.Width)
                    {
                        Rectangle tileRectangle = new Rectangle(xCoordinate, yCoordinate, tileSize.Width, tileSize.Height);

                        Bitmap newTile = new Bitmap(tileSize.Width, tileSize.Height);
                        using (Graphics g = Graphics.FromImage(newTile))
                        {
                            g.DrawImage(layerBitmap, 0, 0, tileRectangle, GraphicsUnit.Pixel);
                        }

                        //discard tile if it is transparent
                        if (!this.IsBitmapTransparent(newTile))
                        {
                            //check if new tile has already been added to unique tile collection
                            bool tileExists = false;
                            foreach (Bitmap existingTile in this._uniqueTiles)
                            {
                                if (this.AreBitmapsEqual(newTile, existingTile))
                                {
                                    tileExists = true;
                                    break;
                                }
                            }
                            //add tile if it has not already been added
                            if (!tileExists)
                            {
                                this._uniqueTiles.Add(newTile);
                            }
                        }
                    }
                }
            }
        }

        private void WriteMapDataAndTiles(PsdFile psdFile, Size tileSize)
        {
            string psdPath = Path.GetDirectoryName(this._pathTextBox.Text);
            Size psdSize = ImageDecoder.DecodeImage(psdFile).Size;
            string tmxFilename = this._tilePrefixTextBox.Text + ".tmx";
            string tilesetName = this._tilePrefixTextBox.Text + "Tiles";
            string tilesetImageName = tilesetName + ".png";
            
            //create xml document for storing map meta data
            XmlDocument xmlDocument = new XmlDocument();
            XmlDeclaration declarationNode = xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDocument.AppendChild(declarationNode);
            //create map element
            XmlElement mapElement = xmlDocument.CreateElement("map");
            xmlDocument.AppendChild(mapElement);
            mapElement.SetAttribute("version", "1.0");
            mapElement.SetAttribute("orientation", "orthogonal");
            mapElement.SetAttribute("width", (psdSize.Width / tileSize.Width).ToString());
            mapElement.SetAttribute("height", (psdSize.Height / tileSize.Height).ToString());
            mapElement.SetAttribute("tilewidth", tileSize.Width.ToString());
            mapElement.SetAttribute("tileheight", tileSize.Height.ToString());
            //create tileset element
            XmlElement tilesetElement = xmlDocument.CreateElement("tileset");
            mapElement.AppendChild(tilesetElement);
            tilesetElement.SetAttribute("firstgid", "1");
            tilesetElement.SetAttribute("name", tilesetName);
            tilesetElement.SetAttribute("tilewidth", tileSize.Width.ToString());
            tilesetElement.SetAttribute("tileheight", tileSize.Height.ToString());
            tilesetElement.SetAttribute("spacing", "4");
            tilesetElement.SetAttribute("margin", "3");
            //create image element
            XmlElement imageElement = xmlDocument.CreateElement("image");
            imageElement.SetAttribute("source", tilesetImageName);

            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);

                //create folder for tiles
                string tilePath = Path.Combine(psdPath, this._tilePrefixTextBox.Text + "Tiles");
                Directory.CreateDirectory(tilePath);

                foreach (Layer psdLayer in psdFile.Layers)
                {
                    XmlElement layerElement = xmlDocument.CreateElement("layer");
                    mapElement.AppendChild(layerElement);
                    layerElement.SetAttribute("name", psdLayer.Name);
                    layerElement.SetAttribute("width", (psdSize.Width / tileSize.Width).ToString());
                    layerElement.SetAttribute("height", (psdSize.Height / tileSize.Height).ToString());
                    
                    XmlElement dataElement = xmlDocument.CreateElement("data");
                    layerElement.AppendChild(dataElement);
                    dataElement.SetAttribute("encoding", "base64");
                    dataElement.SetAttribute("compression", "zlib");

                    Bitmap layerBitmap = ImageDecoder.DecodeImage(psdLayer);

                    for (int yCoordinate = 0; yCoordinate <= layerBitmap.Height - tileSize.Height; yCoordinate += tileSize.Height)
                    {
                        for (int xCoordinate = 0; xCoordinate <= layerBitmap.Width - tileSize.Width; xCoordinate += tileSize.Width)
                        {
                            Rectangle tileRectangle = new Rectangle(xCoordinate, yCoordinate, tileSize.Width, tileSize.Height);

                            Bitmap tile = new Bitmap(tileSize.Width, tileSize.Height);
                            using (Graphics g = Graphics.FromImage(tile))
                            {
                                g.DrawImage(layerBitmap, 0, 0, tileRectangle, GraphicsUnit.Pixel);
                            }

                            for (int i = 0; i < this._uniqueTiles.Count; i++)
                            {
                                //write tile index to layer byte stream for encoding
                                if (this.IsBitmapTransparent(this._uniqueTiles[i]))
                                {
                                    //empty tiles in .tmx format are encoded with 0; ignore transparent tiles for faster rendering
                                    writer.Write(0);
                                }
                                else
                                {
                                    //tiles in .tmx tilesets maps are referenced reading from left to right, top to bottom starting from 1
                                    if (AreBitmapsEqual(tile, this._uniqueTiles[i]))
                                    {
                                        int tileIndex = i + 1;
                                        writer.Write(tileIndex);

                                        //calculate the number of leading zeros needed so tiles will be in order when sorted by name
                                        int suffixLength = (int)Math.Floor(Math.Log10(this._uniqueTiles.Count) + 1);
                                        this._uniqueTiles[i].Save(Path.Combine(tilePath, this._tilePrefixTextBox.Text + tileIndex.ToString("D" + suffixLength.ToString())) + ".png", ImageFormat.Png);
                                    }
                                }
                            }

                            //compress array of integer tileset ids for storage as text in .tmx file
                            byte[] compressedLayerData = ZlibStream.CompressBuffer(stream.ToArray());
                            string textLayerData = Convert.ToBase64String(compressedLayerData);

                            XmlText layerDataXml = xmlDocument.CreateTextNode(textLayerData);
                            dataElement.AppendChild(layerDataXml);
                        }
                    }
                }
            }

            XmlWriterSettings writerSettings = new XmlWriterSettings();
            
            using (XmlTextWriter xmlWriter = new XmlTextWriter(Path.Combine(psdPath, tmxFilename), Encoding.UTF8))
            {
                xmlWriter.Formatting = Formatting.Indented;
                xmlDocument.WriteTo(xmlWriter);
            }
        }

        [DllImport("msvcrt.dll", CallingConvention=CallingConvention.Cdecl)]
        private static extern int memcmp(IntPtr bitmap1, IntPtr bitmap2, long count);
        protected bool AreBitmapsEqual(Bitmap bitmap1, Bitmap bitmap2)
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

        protected bool IsBitmapTransparent(Bitmap bitmap)
        {
            //assume the bitmap is transparent until a non-transparent pixel is found
            bool isTransparent = true;
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            //check alpha component of every pixel to see whether it is zero
            unsafe
            {
                byte* bitmapDataScan0 = (byte*)bitmapData.Scan0;
                int numberOfPixels = bitmap.Width * bitmap.Height;

                for (int i = 0; i < numberOfPixels * 4; i += 4)
                {
                    if (bitmapDataScan0[i + 3] != 0)
                    {
                        isTransparent = false;
                        break;
                    }
                }
            }

            bitmap.UnlockBits(bitmapData);
            return isTransparent;
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
            this.Icon = Resources.FireAndIceIcon;
            this.Text = "TileSetUtility";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;

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
            this._tileSizeNumericUpDown.ValueChanged += new EventHandler(TileSizeNumericUpDown_ValueChanged);
            this._tileSizeNumericUpDown.Anchor = AnchorStyles.Left;
            this._tileSizeNumericUpDown.Value = Settings.Default.TileSize;
            this._tileSizeNumericUpDown.Maximum = 1000;
            this._tileSizeNumericUpDown.Minimum = 1;

            //status strip
            this._statusStrip.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            this._statusStrip.ShowItemToolTips = true;
            this._statusStrip.SizingGrip = false;
            this._statusStrip.Items.Add(this._statusLabel);
            this._statusStrip.Items.Add(this._progressBar);
            
            //status label
            this._statusLabel.Text = "Ready";

            //add controls
            this.Controls.Add(this._tableLayoutPanel);

            //table layout panel
            this._tableLayoutPanel.Margin = new Padding(0);
            this._tableLayoutPanel.Controls.Add(this._flowLayoutPanel);
            this._tableLayoutPanel.Controls.Add(this._statusStrip);

            //flow layout panel
            this._flowLayoutPanel.Controls.Add(this._browseButton);
            this._flowLayoutPanel.Controls.Add(this._pathTextBox);
            this._flowLayoutPanel.Controls.Add(this._tilePrefixLabel);
            this._flowLayoutPanel.Controls.Add(this._tilePrefixTextBox);
            this._flowLayoutPanel.Controls.Add(this._generateButton);
            this._flowLayoutPanel.SetFlowBreak(this._generateButton, true);
            this._flowLayoutPanel.Controls.Add(this._tileSizeLabel);
            this._flowLayoutPanel.Controls.Add(this._tileSizeNumericUpDown);

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
            this.GenerateTiles();
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