namespace TileSetUtility
{
    partial class TileSetUtilityForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.browseButton = new System.Windows.Forms.Button();
            this.pathBox = new System.Windows.Forms.TextBox();
            this.generateTilesButton = new System.Windows.Forms.Button();
            this.fileNameLabel = new System.Windows.Forms.Label();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // browseButton
            // 
            this.browseButton.Location = new System.Drawing.Point(12, 9);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(75, 23);
            this.browseButton.TabIndex = 0;
            this.browseButton.Text = "Browse";
            this.browseButton.UseVisualStyleBackColor = true;
            this.browseButton.Click += new System.EventHandler(this.browseButton_Click);
            // 
            // pathBox
            // 
            this.pathBox.Location = new System.Drawing.Point(93, 13);
            this.pathBox.Name = "pathBox";
            this.pathBox.Size = new System.Drawing.Size(415, 20);
            this.pathBox.TabIndex = 1;
            // 
            // generateTilesButton
            // 
            this.generateTilesButton.Enabled = false;
            this.generateTilesButton.Location = new System.Drawing.Point(659, 11);
            this.generateTilesButton.Name = "generateTilesButton";
            this.generateTilesButton.Size = new System.Drawing.Size(98, 23);
            this.generateTilesButton.TabIndex = 2;
            this.generateTilesButton.Text = "Generate Tiles";
            this.generateTilesButton.UseVisualStyleBackColor = true;
            this.generateTilesButton.Click += new System.EventHandler(this.generateTilesButton_Click);
            // 
            // fileNameLabel
            // 
            this.fileNameLabel.AutoSize = true;
            this.fileNameLabel.Location = new System.Drawing.Point(514, 16);
            this.fileNameLabel.Name = "fileNameLabel";
            this.fileNameLabel.Size = new System.Drawing.Size(35, 13);
            this.fileNameLabel.TabIndex = 3;
            this.fileNameLabel.Text = "Name";
            // 
            // nameTextBox
            // 
            this.nameTextBox.Location = new System.Drawing.Point(555, 13);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(98, 20);
            this.nameTextBox.TabIndex = 4;
            // 
            // TileSetUtilityForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(792, 69);
            this.Controls.Add(this.nameTextBox);
            this.Controls.Add(this.fileNameLabel);
            this.Controls.Add(this.generateTilesButton);
            this.Controls.Add(this.pathBox);
            this.Controls.Add(this.browseButton);
            this.Name = "TileSetUtilityForm";
            this.Text = "TileSet Utility";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.TextBox pathBox;
        private System.Windows.Forms.Button generateTilesButton;
        private System.Windows.Forms.Label fileNameLabel;
        private System.Windows.Forms.TextBox nameTextBox;
    }
}

