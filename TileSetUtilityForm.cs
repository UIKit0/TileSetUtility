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

        //controls
        private TableLayoutPanel _tableLayoutPanel = new TableLayoutPanel();
        private FlowLayoutPanel _flowLayoutPanel = new FlowLayoutPanel();
        private Button _browseButton = new Button();
        private TextBox _pathTextBox = new TextBox();
        private Button _generateButton = new Button();
        private Label _tilePrefixLabel = new Label();
        private TextBox _tilePrefixTextBox = new TextBox();
        private Label _tileSizeLabel = new Label();
        private NumericUpDown _tileSizeNumericUpDown = new NumericUpDown();
        private StatusStrip _statusStrip = new StatusStrip();
        private ToolStripProgressBar _progressBar = new ToolStripProgressBar();

        private PsdFile _psdFile = new PsdFile();
        private Size _tileSize;
        private List<Bitmap> _uniqueTiles = new List<Bitmap>();
        private BackgroundWorker _backgroundWorker = new BackgroundWorker();

        #endregion

        #region Constructors

        public TileSetUtilityForm()
        {
            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.WorkerSupportsCancellation = true;
            _backgroundWorker.DoWork += new DoWorkEventHandler(this.BackgroundWorker_DoWork);
            _backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(this.BackgroundWorker_ProgressChanged);
            _backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.BackgroundWorker_RunWorkerCompleted);
            
            this.InitializeControls();
            this.UpdateGenerateEnable();
        }

        private void InitializeControls()
        {
            this.SuspendLayout();
            _tableLayoutPanel.SuspendLayout();
            _flowLayoutPanel.SuspendLayout();

            //form
            this.FormClosing += new FormClosingEventHandler(this.TileSetUtilityForm_FormClosing);
            this.Icon = Resources.FireAndIceIcon;
            this.Text = "TileSetUtility";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            //table layout panel
            _tableLayoutPanel.AutoSize = true;
            _tableLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _tableLayoutPanel.GrowStyle = TableLayoutPanelGrowStyle.AddRows;

            //flow layout panel
            _flowLayoutPanel.AutoSize = true;
            _flowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            //browse button
            _browseButton.Text = "Browse";
            _browseButton.Anchor = AnchorStyles.Left;
            _browseButton.Click += new EventHandler(this.BrowseButton_Click);

            //path text box
            if (File.Exists(Settings.Default.PsdPath))
            {
                _pathTextBox.Text = Settings.Default.PsdPath;
            }
            else
            {
                _pathTextBox.Text = "";
            }
            _pathTextBox.TextChanged += new EventHandler(this.PathTextBox_TextChanged);
            _pathTextBox.ReadOnly = true;
            _pathTextBox.Anchor = AnchorStyles.Left;
            _pathTextBox.Width = 300;

            //generate button
            _generateButton.Text = Resources.GenerateText;
            _generateButton.Anchor = AnchorStyles.Left;
            _generateButton.Click += new EventHandler(this.GenerateButton_Click);

            //tile prefix label
            _tilePrefixLabel.Text = "Tile prefix";
            _tilePrefixLabel.Anchor = AnchorStyles.Left;
            _tilePrefixLabel.AutoSize = true;
            _tilePrefixLabel.TextAlign = ContentAlignment.MiddleLeft;

            //tile prefix textbox
            _tilePrefixTextBox.Text = Settings.Default.TilePrefix;
            _tilePrefixTextBox.TextChanged += new EventHandler(this.TilePrefixTextBox_TextChanged);
            _tilePrefixTextBox.Anchor = AnchorStyles.Left;

            //tile size label
            _tileSizeLabel.Text = "Tile size (px)";
            _tileSizeLabel.Anchor = AnchorStyles.Left;
            _tileSizeLabel.AutoSize = true;
            _tileSizeLabel.TextAlign = ContentAlignment.MiddleLeft;

            //tile size numeric up down
            _tileSizeNumericUpDown.ValueChanged += new EventHandler(this.TileSizeNumericUpDown_ValueChanged);
            _tileSizeNumericUpDown.Value = Settings.Default.TileSize;
            _tileSizeNumericUpDown.Anchor = AnchorStyles.Left;
            _tileSizeNumericUpDown.Maximum = 1000;
            _tileSizeNumericUpDown.Minimum = 1;

            //status strip
            _statusStrip.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _statusStrip.ShowItemToolTips = true;
            _statusStrip.SizingGrip = false;
            _statusStrip.Items.Add(_progressBar);

            //add controls
            this.Controls.Add(_tableLayoutPanel);

            //table layout panel
            _tableLayoutPanel.Margin = new Padding(0);
            _tableLayoutPanel.Controls.Add(_flowLayoutPanel);
            _tableLayoutPanel.Controls.Add(_statusStrip);

            //flow layout panel
            _flowLayoutPanel.Controls.Add(_browseButton);
            _flowLayoutPanel.Controls.Add(_pathTextBox);
            _flowLayoutPanel.Controls.Add(_tilePrefixLabel);
            _flowLayoutPanel.Controls.Add(_tilePrefixTextBox);
            _flowLayoutPanel.Controls.Add(_generateButton);
            _flowLayoutPanel.SetFlowBreak(_generateButton, true);
            _flowLayoutPanel.Controls.Add(_tileSizeLabel);
            _flowLayoutPanel.Controls.Add(_tileSizeNumericUpDown);

            this.ResumeLayout();
            _tableLayoutPanel.ResumeLayout();
            _flowLayoutPanel.ResumeLayout();
        }

        #endregion

        #region Methods

        private bool FindUniqueTiles(BackgroundWorker backgroundWorker, DoWorkEventArgs e)
        {
            //scan through all layers of photoshop file to find unique, non-transparent tiles
            foreach (Layer psdLayer in _psdFile.Layers)
            {
                Bitmap layerBitmap = ImageDecoder.DecodeImage(psdLayer);
                if (layerBitmap != null)
                {
                    for (int yCoordinate = 0; yCoordinate <= layerBitmap.Height - _tileSize.Height; yCoordinate += _tileSize.Height)
                    {
                        for (int xCoordinate = 0; xCoordinate <= layerBitmap.Width - _tileSize.Width; xCoordinate += _tileSize.Width)
                        {
                            if (!backgroundWorker.CancellationPending)
                            {
                                Rectangle tileRectangle = new Rectangle(xCoordinate, yCoordinate, _tileSize.Width, _tileSize.Height);

                                Bitmap newTile = new Bitmap(_tileSize.Width, _tileSize.Height);
                                using (Graphics g = Graphics.FromImage(newTile))
                                {
                                    g.DrawImage(layerBitmap, 0, 0, tileRectangle, GraphicsUnit.Pixel);
                                }

                                //discard tile if it is transparent
                                if (!this.IsBitmapTransparent(newTile))
                                {
                                    //check if new tile has already been added to unique tile collection
                                    bool tileExists = false;
                                    foreach (Bitmap existingTile in _uniqueTiles)
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
                                        _uniqueTiles.Add(newTile);
                                    }
                                }
                            }
                            else
                            {
                                e.Cancel = true;
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        private bool WriteMapAndTileData(BackgroundWorker backgroundWorker, DoWorkEventArgs e)
        {
            string psdPath = Path.GetDirectoryName(_pathTextBox.Text);
            Size psdSize = ImageDecoder.DecodeImage(_psdFile).Size;
            string tmxFilename = _tilePrefixTextBox.Text + ".tmx";
            string tilesetName = _tilePrefixTextBox.Text + "Tiles";
            string tilesetImageName = _tilePrefixTextBox.Text + ".png";
            
            //create xml document for storing map meta data
            XmlDocument xmlDocument = new XmlDocument();
            XmlDeclaration declarationNode = xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDocument.AppendChild(declarationNode);
            //create map element
            XmlElement mapElement = xmlDocument.CreateElement("map");
            xmlDocument.AppendChild(mapElement);
            mapElement.SetAttribute("version", "1.0");
            mapElement.SetAttribute("orientation", "orthogonal");
            mapElement.SetAttribute("width", (psdSize.Width / _tileSize.Width).ToString());
            mapElement.SetAttribute("height", (psdSize.Height / _tileSize.Height).ToString());
            mapElement.SetAttribute("tilewidth", _tileSize.Width.ToString());
            mapElement.SetAttribute("tileheight", _tileSize.Height.ToString());
            //create tileset element
            XmlElement tilesetElement = xmlDocument.CreateElement("tileset");
            mapElement.AppendChild(tilesetElement);
            tilesetElement.SetAttribute("firstgid", "1");
            tilesetElement.SetAttribute("name", tilesetName);
            tilesetElement.SetAttribute("tilewidth", _tileSize.Width.ToString());
            tilesetElement.SetAttribute("tileheight", _tileSize.Height.ToString());
            tilesetElement.SetAttribute("spacing", "4");
            tilesetElement.SetAttribute("margin", "3");
            //create image element
            XmlElement imageElement = xmlDocument.CreateElement("image");
            imageElement.SetAttribute("source", tilesetImageName);
            imageElement.SetAttribute("width", "");
            imageElement.SetAttribute("height", "");
            tilesetElement.AppendChild(imageElement);

            //create folder for tiles
            string tilePath = Path.Combine(psdPath, _tilePrefixTextBox.Text + "Tiles");
            Directory.CreateDirectory(tilePath);

            //calculate the number of leading zeros needed so tiles will be in order when sorted by name
            int suffixLength = (int)Math.Floor(Math.Log10(_uniqueTiles.Count) + 1);

            foreach (Layer psdLayer in _psdFile.Layers)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(stream);

                    XmlElement layerElement = xmlDocument.CreateElement("layer");
                    mapElement.AppendChild(layerElement);
                    layerElement.SetAttribute("name", psdLayer.Name);
                    layerElement.SetAttribute("width", (psdSize.Width / _tileSize.Width).ToString());
                    layerElement.SetAttribute("height", (psdSize.Height / _tileSize.Height).ToString());
                    
                    XmlElement dataElement = xmlDocument.CreateElement("data");
                    layerElement.AppendChild(dataElement);
                    dataElement.SetAttribute("encoding", "base64");
                    dataElement.SetAttribute("compression", "zlib");

                    Bitmap layerBitmap = ImageDecoder.DecodeImage(psdLayer);
                    if (layerBitmap != null)
                    {
                        for (int yCoordinate = 0; yCoordinate <= layerBitmap.Height - _tileSize.Height; yCoordinate += _tileSize.Height)
                        {
                            for (int xCoordinate = 0; xCoordinate <= layerBitmap.Width - _tileSize.Width; xCoordinate += _tileSize.Width)
                            {
                                if (!backgroundWorker.CancellationPending)
                                {
                                    Rectangle tileRectangle = new Rectangle(xCoordinate, yCoordinate, _tileSize.Width, _tileSize.Height);

                                    Bitmap tile = new Bitmap(_tileSize.Width, _tileSize.Height);
                                    using (Graphics g = Graphics.FromImage(tile))
                                    {
                                        g.DrawImage(layerBitmap, 0, 0, tileRectangle, GraphicsUnit.Pixel);
                                    }

                                    for (int i = 0; i < _uniqueTiles.Count; i++)
                                    {
                                        //write tile index to layer byte stream for encoding
                                        if (this.IsBitmapTransparent(_uniqueTiles[i]))
                                        {
                                            //empty tiles in .tmx format are encoded with 0; ignore transparent tiles for faster rendering
                                            writer.Write(0);
                                        }
                                        else
                                        {
                                            //tiles in .tmx tilesets maps are referenced reading from left to right, top to bottom starting from 1
                                            if (AreBitmapsEqual(tile, _uniqueTiles[i]))
                                            {
                                                int tileIndex = i + 1;
                                                writer.Write(tileIndex);

                                                _uniqueTiles[i].Save(Path.Combine(tilePath, _tilePrefixTextBox.Text + tileIndex.ToString("D" + suffixLength.ToString())) + ".png", ImageFormat.Png);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    e.Cancel = true;
                                    return false;
                                }
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

            //write results to xml file
            XmlWriterSettings writerSettings = new XmlWriterSettings();
            using (XmlTextWriter xmlWriter = new XmlTextWriter(Path.Combine(psdPath, tmxFilename), Encoding.UTF8))
            {
                xmlWriter.Formatting = Formatting.Indented;
                xmlDocument.WriteTo(xmlWriter);
            }
            return true;
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
            bool enableGenerate = !String.IsNullOrEmpty(_pathTextBox.Text) && !String.IsNullOrEmpty(_tilePrefixTextBox.Text);
            _generateButton.Enabled = enableGenerate;
        }

        #endregion

        #region Event methods

        private void TileSetUtilityForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_backgroundWorker.IsBusy)
            {
                if (MessageBox.Show("Cancel tile generation before closing", "Tiles are being generated", MessageBoxButtons.OK) == DialogResult.OK)
                {
                    e.Cancel = true;
                }
            }
            else
            {
                Settings.Default.Save();
            }
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker backgroundWorker = sender as BackgroundWorker;
            backgroundWorker.ReportProgress(0);

            _psdFile.Load(Settings.Default.PsdPath);

            if (this.FindUniqueTiles(backgroundWorker, e))
            {
                backgroundWorker.ReportProgress(50);
            }
            else
            {
                return;
            }
            if (this.WriteMapAndTileData(backgroundWorker, e))
            {
                backgroundWorker.ReportProgress(100);
            }
            else
            {
                return;
            }
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this._progressBar.Value = e.ProgressPercentage;
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _progressBar.Value = 0;
            }
            this._generateButton.Text = Resources.GenerateText;
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Photoshop file|*.psd";

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                _pathTextBox.Text = fileDialog.FileName;
            }
        }

        private void GenerateButton_Click(object sender, EventArgs e)
        {
            if (!_backgroundWorker.IsBusy)
            {
                _backgroundWorker.RunWorkerAsync();
                _generateButton.Text = Resources.CancelText;
            }
            else
            {
                if (_backgroundWorker.WorkerSupportsCancellation)
                {
                    _backgroundWorker.CancelAsync();
                    _generateButton.Text = Resources.GenerateText;
                }
            }
        }

        private void TileSizeNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            _tileSize = new Size((int)_tileSizeNumericUpDown.Value, (int)_tileSizeNumericUpDown.Value);
            Settings.Default.TileSize = (int)_tileSizeNumericUpDown.Value;
        }

        private void TilePrefixTextBox_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.TilePrefix = _tilePrefixTextBox.Text;
            this.UpdateGenerateEnable();
        }

        private void PathTextBox_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.PsdPath = _pathTextBox.Text;
            this.UpdateGenerateEnable();
        }

        #endregion
    }
}