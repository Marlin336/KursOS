namespace KursOS
{
    partial class FLog
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FLog));
            this.LLogin = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.TBLog = new System.Windows.Forms.TextBox();
            this.TBPass = new System.Windows.Forms.TextBox();
            this.BOK = new System.Windows.Forms.Button();
            this.BRem = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // LLogin
            // 
            this.LLogin.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.LLogin.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.LLogin.Location = new System.Drawing.Point(-1, 1);
            this.LLogin.Margin = new System.Windows.Forms.Padding(0);
            this.LLogin.Name = "LLogin";
            this.LLogin.Size = new System.Drawing.Size(251, 25);
            this.LLogin.TabIndex = 0;
            this.LLogin.Text = "Логин";
            this.LLogin.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label2.Location = new System.Drawing.Point(3, 54);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(247, 27);
            this.label2.TabIndex = 1;
            this.label2.Text = "Пароль";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // TBLog
            // 
            this.TBLog.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.TBLog.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.TBLog.Location = new System.Drawing.Point(52, 29);
            this.TBLog.Name = "TBLog";
            this.TBLog.Size = new System.Drawing.Size(148, 23);
            this.TBLog.TabIndex = 0;
            // 
            // TBPass
            // 
            this.TBPass.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.TBPass.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.TBPass.Location = new System.Drawing.Point(52, 84);
            this.TBPass.Name = "TBPass";
            this.TBPass.PasswordChar = '*';
            this.TBPass.Size = new System.Drawing.Size(148, 23);
            this.TBPass.TabIndex = 1;
            // 
            // BOK
            // 
            this.BOK.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.BOK.Location = new System.Drawing.Point(12, 120);
            this.BOK.Name = "BOK";
            this.BOK.Size = new System.Drawing.Size(100, 30);
            this.BOK.TabIndex = 2;
            this.BOK.Text = "Вход";
            this.BOK.UseVisualStyleBackColor = true;
            this.BOK.Click += new System.EventHandler(this.BOK_Click);
            // 
            // BRem
            // 
            this.BRem.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.BRem.Location = new System.Drawing.Point(137, 119);
            this.BRem.Name = "BRem";
            this.BRem.Size = new System.Drawing.Size(100, 30);
            this.BRem.TabIndex = 5;
            this.BRem.TabStop = false;
            this.BRem.Text = "Очистить";
            this.BRem.UseVisualStyleBackColor = true;
            this.BRem.Click += new System.EventHandler(this.BRem_Click);
            // 
            // FLog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(249, 161);
            this.Controls.Add(this.BRem);
            this.Controls.Add(this.BOK);
            this.Controls.Add(this.TBPass);
            this.Controls.Add(this.TBLog);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.LLogin);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(265, 200);
            this.MinimumSize = new System.Drawing.Size(265, 200);
            this.Name = "FLog";
            this.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Вход";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label LLogin;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox TBLog;
        private System.Windows.Forms.TextBox TBPass;
        private System.Windows.Forms.Button BOK;
        private System.Windows.Forms.Button BRem;
    }
}

