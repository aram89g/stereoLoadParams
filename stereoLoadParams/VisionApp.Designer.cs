namespace stereoLoadParams
{
    partial class VisionApp
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
            this.Camera_Selection_Left = new System.Windows.Forms.ComboBox();
            this.Camera_Selection_Right = new System.Windows.Forms.ComboBox();
            this.StartButton = new System.Windows.Forms.Button();
            this.newCalib = new System.Windows.Forms.CheckBox();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.calibrationImagesPath = new System.Windows.Forms.TextBox();
            this.browseBtn = new System.Windows.Forms.Button();
            this.loadCalib = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // Camera_Selection_Left
            // 
            this.Camera_Selection_Left.FormattingEnabled = true;
            this.Camera_Selection_Left.Location = new System.Drawing.Point(12, 46);
            this.Camera_Selection_Left.Name = "Camera_Selection_Left";
            this.Camera_Selection_Left.Size = new System.Drawing.Size(272, 21);
            this.Camera_Selection_Left.TabIndex = 0;
            // 
            // Camera_Selection_Right
            // 
            this.Camera_Selection_Right.FormattingEnabled = true;
            this.Camera_Selection_Right.Location = new System.Drawing.Point(12, 90);
            this.Camera_Selection_Right.Name = "Camera_Selection_Right";
            this.Camera_Selection_Right.Size = new System.Drawing.Size(272, 21);
            this.Camera_Selection_Right.TabIndex = 1;
            // 
            // StartButton
            // 
            this.StartButton.FlatAppearance.BorderSize = 4;
            this.StartButton.Location = new System.Drawing.Point(373, 67);
            this.StartButton.Name = "StartButton";
            this.StartButton.Size = new System.Drawing.Size(92, 65);
            this.StartButton.TabIndex = 2;
            this.StartButton.Text = "START";
            this.StartButton.UseVisualStyleBackColor = true;
            this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
            // 
            // newCalib
            // 
            this.newCalib.AutoSize = true;
            this.newCalib.Location = new System.Drawing.Point(12, 151);
            this.newCalib.Name = "newCalib";
            this.newCalib.Size = new System.Drawing.Size(100, 17);
            this.newCalib.TabIndex = 3;
            this.newCalib.Text = "New Calibration";
            this.newCalib.UseVisualStyleBackColor = true;
            this.newCalib.CheckedChanged += new System.EventHandler(this.newCalib_CheckedChanged);
            // 
            // calibrationImagesPath
            // 
            this.calibrationImagesPath.Location = new System.Drawing.Point(12, 174);
            this.calibrationImagesPath.Name = "calibrationImagesPath";
            this.calibrationImagesPath.Size = new System.Drawing.Size(272, 20);
            this.calibrationImagesPath.TabIndex = 4;
            // 
            // browseBtn
            // 
            this.browseBtn.Location = new System.Drawing.Point(101, 200);
            this.browseBtn.Name = "browseBtn";
            this.browseBtn.Size = new System.Drawing.Size(75, 23);
            this.browseBtn.TabIndex = 5;
            this.browseBtn.Text = "Browse";
            this.browseBtn.UseVisualStyleBackColor = true;
            this.browseBtn.Click += new System.EventHandler(this.browseBtn_Click);
            // 
            // loadCalib
            // 
            this.loadCalib.AutoSize = true;
            this.loadCalib.Location = new System.Drawing.Point(182, 151);
            this.loadCalib.Name = "loadCalib";
            this.loadCalib.Size = new System.Drawing.Size(102, 17);
            this.loadCalib.TabIndex = 6;
            this.loadCalib.Text = "Load Calibration";
            this.loadCalib.UseVisualStyleBackColor = true;
            this.loadCalib.CheckedChanged += new System.EventHandler(this.loadCalib_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(408, 209);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "label1";
            // 
            // VisionApp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(508, 252);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.loadCalib);
            this.Controls.Add(this.browseBtn);
            this.Controls.Add(this.calibrationImagesPath);
            this.Controls.Add(this.newCalib);
            this.Controls.Add(this.StartButton);
            this.Controls.Add(this.Camera_Selection_Right);
            this.Controls.Add(this.Camera_Selection_Left);
            this.Name = "VisionApp";
            this.Text = "VisionApp";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox Camera_Selection_Left;
        private System.Windows.Forms.ComboBox Camera_Selection_Right;
        private System.Windows.Forms.Button StartButton;
        private System.Windows.Forms.CheckBox newCalib;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.TextBox calibrationImagesPath;
        private System.Windows.Forms.Button browseBtn;
        private System.Windows.Forms.CheckBox loadCalib;
        private System.Windows.Forms.Label label1;
    }
}

