namespace WPILibInstaller
{
    partial class AdminChecker
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
            this.label1 = new System.Windows.Forms.Label();
            this.allUsersButton = new System.Windows.Forms.Button();
            this.currentUserButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(80, 56);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(597, 25);
            this.label1.TabIndex = 0;
            this.label1.Text = "Would you like to install for all users, or just the current user?";
            // 
            // allUsersButton
            // 
            this.allUsersButton.Location = new System.Drawing.Point(12, 136);
            this.allUsersButton.Name = "allUsersButton";
            this.allUsersButton.Size = new System.Drawing.Size(350, 100);
            this.allUsersButton.TabIndex = 1;
            this.allUsersButton.Text = "All Users";
            this.allUsersButton.UseVisualStyleBackColor = true;
            this.allUsersButton.Click += new System.EventHandler(this.allUsersButton_Click);
            // 
            // currentUserButton
            // 
            this.currentUserButton.Location = new System.Drawing.Point(412, 136);
            this.currentUserButton.Name = "currentUserButton";
            this.currentUserButton.Size = new System.Drawing.Size(350, 100);
            this.currentUserButton.TabIndex = 2;
            this.currentUserButton.Text = "Current User";
            this.currentUserButton.UseVisualStyleBackColor = true;
            this.currentUserButton.Click += new System.EventHandler(this.currentUserButton_Click);
            // 
            // AdminChecker
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(774, 248);
            this.Controls.Add(this.currentUserButton);
            this.Controls.Add(this.allUsersButton);
            this.Controls.Add(this.label1);
            this.Name = "AdminChecker";
            this.Text = "AdminChecker";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button allUsersButton;
        private System.Windows.Forms.Button currentUserButton;
    }
}