namespace KursOS
{
    partial class MainWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.BEnter = new System.Windows.Forms.Button();
            this.TBOut = new System.Windows.Forms.TextBox();
            this.TBIn = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // BEnter
            // 
            this.BEnter.Location = new System.Drawing.Point(575, 310);
            this.BEnter.Name = "BEnter";
            this.BEnter.Size = new System.Drawing.Size(82, 20);
            this.BEnter.TabIndex = 1;
            this.BEnter.Text = "BEnter";
            this.BEnter.UseVisualStyleBackColor = true;
            this.BEnter.Click += new System.EventHandler(this.BEnter_Click);
            // 
            // TBOut
            // 
            this.TBOut.AcceptsReturn = true;
            this.TBOut.AcceptsTab = true;
            this.TBOut.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TBOut.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.TBOut.Location = new System.Drawing.Point(12, 12);
            this.TBOut.MaxLength = 0;
            this.TBOut.Multiline = true;
            this.TBOut.Name = "TBOut";
            this.TBOut.ReadOnly = true;
            this.TBOut.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.TBOut.Size = new System.Drawing.Size(645, 292);
            this.TBOut.TabIndex = 1;
            this.TBOut.TabStop = false;
            // 
            // TBIn
            // 
            this.TBIn.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TBIn.Location = new System.Drawing.Point(12, 310);
            this.TBIn.Name = "TBIn";
            this.TBIn.Size = new System.Drawing.Size(556, 20);
            this.TBIn.TabIndex = 0;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(669, 354);
            this.Controls.Add(this.TBIn);
            this.Controls.Add(this.TBOut);
            this.Controls.Add(this.BEnter);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "BeeOS";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainWindow_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button BEnter;
        private System.Windows.Forms.TextBox TBOut;
        private System.Windows.Forms.TextBox TBIn;
    }
}