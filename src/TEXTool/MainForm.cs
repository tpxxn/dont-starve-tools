﻿#region License
/*
Klei Studio is licensed under the MIT license.
Copyright © 2013 Matt Stevens

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion License

using System;
using System.Reflection;
using System.Windows.Forms;

using System.ComponentModel;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml;
using System.IO;
using System.Drawing.Imaging;

namespace TEXTool
{
    public partial class MainForm : Form
    {
        public TEXTool Tool;
        public ProgressForm ProgressForm;

        GraphicsPath graphicsPath;
        float offsetX = 0, offsetY = 0, scaleX = 1, scaleY = 1;

        public MainForm()
        {
            Tool = new TEXTool();
            Tool.FileOpened += new FileOpenedEventHandler(TEXTool_FileOpened);
            Tool.FileRawImage += new FileRawImageEventHandler(tool_FileRawImage);
            Tool.OnProgressUpdate += tool_OnProgressUpdate;

            InitializeComponent();
            FillZoomLevelComboBox();
            versionToolStripLabel.Text = string.Format("Version: {0}", Assembly.GetEntryAssembly().GetName().Version);

            foreach (PropertyInfo prop in typeof(Color).GetProperties())
            {
                if (prop.PropertyType.FullName == "System.Drawing.Color")
                {
                    atlasElementBorderColors.ComboBox.Items.Add(prop.Name);
                }
            }
            atlasElementBorderColors.ComboBox.SelectedItem = "Black";
            
            atlasElementsListToolStripComboBox.ComboBox.DisplayMember = "Name";
        }

        #region

        void tool_FileRawImage(object sender, FileRawImageEventArgs e)
        {
            atlasElementsCountIntToolStripLabel.Text = e.AtlasElements.Count.ToString();
            atlasElementsListToolStripComboBox.ComboBox.SelectedIndex = -1;
            atlasElementsListToolStripComboBox.ComboBox.Items.Clear();

            graphicsPath = null;
            atlasElementsListToolStripComboBox.Enabled = atlasElementBorderColors.Enabled = false;
            atlasElementWidthToolStrip.Text = atlasElementHeightToolStrip.Text = atlasElementXToolStrip.Text = atlasElementYToolStrip.Text = "0";

            if (e.AtlasElements.Count > 0)
            {
                graphicsPath = new GraphicsPath();
                atlasElementsListToolStripComboBox.Enabled = atlasElementBorderColors.Enabled = true;
                foreach (KleiTextureAtlasElement el in e.AtlasElements)
                {
                    atlasElementsListToolStripComboBox.Items.Add(el);
                }
            }
            
            imageBox.Image = e.Image;
            zoomLevelToolStripComboBox.Text = string.Format("{0}%", imageBox.Zoom);
        }

        private void TEXTool_FileOpened(object sender, FileOpenedEventArgs e)
        {
            this.Text = String.Format("Klei Studio - TEXTool - [{0}]", e.FileName);
            this.formatToolStripStatusLabel.Text = String.Format("Format: {0}", e.Format);
            this.sizeToolStripStatusLabel.Text = String.Format("Size: {0}", e.Size);
            this.mipmapsToolStripStatusLabel.Text = String.Format("Mipmaps: {0}", e.Mipmaps);
            this.platformToolStripStatusLabel.Text = String.Format("Platform: {0}", e.Platform);
            this.textureTypeToolStripStatusLabel.Text = String.Format("Texture Type: {0}", e.TexType);

            if (e.PreCave)
                MessageBox.Show(@"Error, this is a pre 'Cave Update' TEX file. If you want to convert this, please use an older version of TEXTool or 'update' the file using the converter found in the offical thread.");
        }

        public void OpenExternalFile(string filename)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.FileName = filename;
                ProgressForm = new ProgressForm { StartPosition = FormStartPosition.CenterParent };
                backgroundWorker.RunWorkerAsync(dialog);
            }
        }

        private void OpenFileDialog()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Klei Texture Files (*.tex)|*.tex|(Klei Xml File)|*.xml|All Files (*.*)|*.*";
                dialog.DefaultExt = "tex";

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    string[] file = dialog.FileName.Split('.');
                    if(file[file.Length-1]=="xml")
                    {
                      
                        TEXTool tool = new TEXTool();
                        XmlDocument anxml = new XmlDocument();
                        anxml.LoadXml(File.ReadAllText( dialog.FileName));
                        XmlNode atlas = anxml.GetElementsByTagName("Atlas")[0];
                        XmlNode texture = atlas.SelectSingleNode("Texture");
                        string texname = texture.Attributes["filename"].Value;
                        string dictor = Path.GetDirectoryName(dialog.FileName) + @"\";
                        string xmlpath =dictor + texname;
                        Console.WriteLine(xmlpath);

                        tool.OpenFile(dialog.SafeFileName, new FileStream(xmlpath, FileMode.Open));
                        int alterwidth = tool.CurrentFileRaw.Width;
                        int alterheight = tool.CurrentFileRaw.Height;
                        XmlNode Elements = atlas.SelectSingleNode("Elements");
                        XmlNodeList elements= Elements.SelectNodes("Element");
                        foreach(XmlNode node in elements)
                        {
                            string name = node.Attributes["name"].Value.Split('.')[0] + ".png";
                            float u1 = Convert.ToSingle(node.Attributes["u1"].Value);
                            float u2 = Convert.ToSingle(node.Attributes["u2"].Value);
                            float v1 = Convert.ToSingle(node.Attributes["v1"].Value);
                            float v2 = Convert.ToSingle(node.Attributes["v2"].Value);
                            int imageheight= (int)(alterheight * v2 - alterheight * v1);
                            int imagewidth = (int)(alterwidth * u2 - alterwidth * u1);
                            Console.WriteLine("(" + (int)(alterwidth * u1) + "," + (int)(alterheight - (alterheight * v1) - imageheight) + ")  (" + (int)(alterwidth * u2) + "," + (int)(alterheight - alterheight * v2 + imageheight) + ")");
                            Rectangle cloneRect = new Rectangle((int)(alterwidth * u1), (int)(alterheight - (alterheight * v1) - imageheight), imagewidth, imageheight);
                            //Rectangle cloneRect = new Rectangle(0, (int)(alterheight - (alterheight * v1) - imageheight), (int)(alterwidth * u2), (int)(alterheight - alterheight * v2 + imageheight));
                            System.Drawing.Imaging.PixelFormat format =tool.CurrentFileRaw.PixelFormat;
                            Bitmap cloneBitmap = tool.CurrentFileRaw.Clone(cloneRect, format);
                            FileStream wimage = new FileStream(dictor + name,FileMode.Create);
                            cloneBitmap.Save(wimage,ImageFormat.Png);
                            wimage.Close();
                        }
                        System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("Explorer.exe");
                        psi.Arguments = "/e,/select," + dialog.FileName;
                        System.Diagnostics.Process.Start(psi);
                        return;
                    }
                    ProgressForm = new ProgressForm();
                    ProgressForm.StartPosition = FormStartPosition.CenterParent;
                    backgroundWorker.RunWorkerAsync(dialog);
                    ProgressForm.ShowDialog(this);
                }
            }
        }

        private void SaveFileDialog()
        {
            if (Tool.CurrentFile == null)
                return;

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "All Supported Images (*.bmp;*.dib;*.rle;*.gif;*.jpg;*.png)|*.bmp;*.dib;*.rle;*.gif;*.jpg;*.png|Bitmaps (*.bmp;*.dib;*.rle)|*.bmp;*.dib;*.rle|Graphics Interchange Format (*.gif)|*.gif|Joint Photographic Experts (*.jpg)|*.jpg|Portable Network Graphics (*.png)|*.png|All Files (*.*)|*.*";
                dialog.DefaultExt = "png";

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    Tool.SaveFile(dialog.FileName);
                }
            }
        }

        private void FillZoomLevelComboBox()
        {
            zoomLevelToolStripComboBox.Items.Clear();

            foreach (int zoom in imageBox.ZoomLevels)
                zoomLevelToolStripComboBox.Items.Add(string.Format("{0}%", zoom));
        }

        #endregion

        #region ToolStrip Buttons

        private void openToolStripButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog();
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog();
        }

        private void fitToolStripButton_Click(object sender, EventArgs e)
        {
            this.imageBox.ZoomToFit();
        }

        private void zoomInToolStripButton_Click(object sender, EventArgs e)
        {
            this.imageBox.ZoomIn();
        }

        private void zoomOutToolStripButton_Click(object sender, EventArgs e)
        {
            this.imageBox.ZoomOut();
        }

        private void infoToolStripButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.dev-zilla.net/kleistudio/");
        }

        #endregion

        #region Misc Form Event Handlers

        private void imageBox_ZoomLevelsChanged(object sender, EventArgs e)
        {
            FillZoomLevelComboBox();
        }

        private void imageBox_ZoomChanged(object sender, EventArgs e)
        {
            zoomLevelToolStripComboBox.Text = string.Format("{0}%", imageBox.Zoom);
        }

        #endregion

        #region Hotkeys

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData) {
                case Keys.Control | Keys.O:
                    OpenFileDialog();
                    break;
                case Keys.Control | Keys.S:
                    SaveFileDialog();
                    break;
                case Keys.Control | Keys.Add:
                    imageBox.ZoomIn();
                    break;
                case Keys.Control | Keys.Subtract:
                    imageBox.ZoomOut();
                    break;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }

            return true;
        }

        #endregion

        #region Dev Custom Functions

        private void DrawRectangle(KleiTextureAtlasElement element)
        {
            int x, y, width, height;
            x = element.ImgHmin;
            y = element.ImgVmin;

            /* INVERT THE Y-AXIS */
            if (element.ImgVmin > element.ImgVmax)
            {
                y = element.ImgVmax;
            }

            width = element.ImgHmax - element.ImgHmin;
            height = Math.Abs(element.ImgVmax - element.ImgVmin);

            graphicsPath = new GraphicsPath();
            graphicsPath.AddRectangle(new Rectangle(x, y, width, height));

            atlasElementWidthToolStrip.Text = width.ToString();
            atlasElementHeightToolStrip.Text = height.ToString();
            atlasElementXToolStrip.Text = x.ToString();
            atlasElementYToolStrip.Text = y.ToString();

            imageBox.Invalidate();
        }

        #endregion

        #region Dev Event Handlers

        private void zoomLevelToolStripComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (imageBox.Image != null)
            {
                int z = int.Parse(zoomLevelToolStripComboBox.SelectedItem.ToString().Replace("%", ""));
                imageBox.Zoom = z;
            }
        }
        
        private void atlasElementsListToolStripComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var element = (KleiTextureAtlasElement)atlasElementsListToolStripComboBox.ComboBox.SelectedItem;
            if (element != null)
            {
                DrawRectangle(element);
            }
        }

        private void atlasElementBorderColors_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (graphicsPath != null)
            {
                imageBox.Refresh();
            }
        }

        private void imageBox_Paint(object sender, PaintEventArgs e)
        {
            if (graphicsPath != null)
            {
                scaleX = imageBox.Zoom / 100f;
                scaleY = imageBox.Zoom / 100f;
                offsetX = ((imageBox.ClientSize.Width - imageBox.PreferredSize.Width) / 2f);
                offsetY = ((imageBox.ClientSize.Height - imageBox.PreferredSize.Height) / 2f);

                if (offsetX < 0)
                {
                    offsetX = -imageBox.HorizontalScroll.Value;
                }
                if (offsetY < 0)
                {
                    offsetY = -imageBox.VerticalScroll.Value;
                }

                e.Graphics.TranslateTransform(offsetX, offsetY);
                e.Graphics.ScaleTransform(scaleX, scaleY);

                Color color = Color.FromName(atlasElementBorderColors.ComboBox.SelectedItem.ToString());
                Pen pen = new Pen(new SolidBrush(color), 5f);
                e.Graphics.DrawPath(pen, graphicsPath);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void mipmapsToolStripStatusLabel_Click(object sender, EventArgs e)
        {

        }

        private void tool_OnProgressUpdate(int value)
        {
            base.Invoke((Action)delegate
            {
                ProgressForm.ReportProgress(value);
            });
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var dialog = (OpenFileDialog)e.Argument;
            Tool.OpenFile(dialog.FileName, dialog.OpenFile());
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (ProgressForm != null)
            {
                ProgressForm.Close();
                ProgressForm.Dispose();
            }
        }
        
        #endregion
        
    }
}
