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
            this.startButton = new System.Windows.Forms.Button();
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
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(354, 46);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(92, 65);
            this.startButton.TabIndex = 2;
            this.startButton.Text = "START";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // VisionApp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(489, 179);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.Camera_Selection_Right);
            this.Controls.Add(this.Camera_Selection_Left);
            this.Name = "VisionApp";
            this.Text = "VisionApp";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox Camera_Selection_Left;
        private System.Windows.Forms.ComboBox Camera_Selection_Right;
        private System.Windows.Forms.Button startButton;
    }
}

