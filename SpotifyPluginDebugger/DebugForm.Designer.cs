namespace SpotifyPluginDebugger
{
    partial class DebugForm
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
            this.labDebug = new System.Windows.Forms.Label();
            this.picDebug = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.picDebug)).BeginInit();
            this.SuspendLayout();
            // 
            // labDebug
            // 
            this.labDebug.AutoSize = true;
            this.labDebug.Location = new System.Drawing.Point(332, 13);
            this.labDebug.Name = "labDebug";
            this.labDebug.Size = new System.Drawing.Size(27, 13);
            this.labDebug.TabIndex = 0;
            this.labDebug.Text = "LAB";
            // 
            // picDebug
            // 
            this.picDebug.Location = new System.Drawing.Point(13, 13);
            this.picDebug.Name = "picDebug";
            this.picDebug.Size = new System.Drawing.Size(300, 300);
            this.picDebug.TabIndex = 1;
            this.picDebug.TabStop = false;
            // 
            // DebugForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(676, 363);
            this.Controls.Add(this.picDebug);
            this.Controls.Add(this.labDebug);
            this.DoubleBuffered = true;
            this.Name = "DebugForm";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.picDebug)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labDebug;
        private System.Windows.Forms.PictureBox picDebug;
    }
}

