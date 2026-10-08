namespace CreatorFlow.AI
{
    partial class AiTestForm
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
            this.components = new System.ComponentModel.Container();
            this.lblPrompt = new System.Windows.Forms.Label();
            this.txtPrompt = new System.Windows.Forms.TextBox();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.txtResult = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // lblPrompt
            this.lblPrompt.AutoSize = true;
            this.lblPrompt.Location = new System.Drawing.Point(30, 20);
            this.lblPrompt.Name = "lblPrompt";
            this.lblPrompt.Text = "Nhập yêu cầu";
            // txtPrompt
            this.txtPrompt.Location = new System.Drawing.Point(30, 50);
            this.txtPrompt.Multiline = true;
            this.txtPrompt.Name = "txtPrompt";
            this.txtPrompt.Size = new System.Drawing.Size(740, 100);
            this.txtPrompt.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.txtPrompt.TabIndex = 0;
            // btnGenerate
            this.btnGenerate.Location = new System.Drawing.Point(30, 165);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(140, 35);
            this.btnGenerate.Text = "Tạo nội dung";
            this.btnGenerate.TabIndex = 1;
            this.btnGenerate.UseVisualStyleBackColor = true;
            this.btnGenerate.Click += this.btnGenerate_Click;
            // btnCancel
            this.btnCancel.Location = new System.Drawing.Point(185, 165);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(90, 35);
            this.btnCancel.Text = "Hủy";
            this.btnCancel.Enabled = false;
            this.btnCancel.TabIndex = 2;
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += this.btnCancel_Click;
            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(295, 175);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Text = "Sẵn sàng";
            // txtResult
            this.txtResult.Location = new System.Drawing.Point(30, 220);
            this.txtResult.Multiline = true;
            this.txtResult.ReadOnly = true;
            this.txtResult.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtResult.Name = "txtResult";
            this.txtResult.Size = new System.Drawing.Size(740, 250);
            this.txtResult.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.txtResult.TabIndex = 3;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 500);
            this.MinimumSize = new System.Drawing.Size(650, 400);
            this.Controls.Add(this.lblPrompt);
            this.Controls.Add(this.txtPrompt);
            this.Controls.Add(this.btnGenerate);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.txtResult);
            this.Name = "AiTestForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Kiểm thử AI";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblPrompt = null!;
        private System.Windows.Forms.TextBox txtPrompt = null!;
        private System.Windows.Forms.Button btnGenerate = null!;
        private System.Windows.Forms.Button btnCancel = null!;
        private System.Windows.Forms.Label lblStatus = null!;
        private System.Windows.Forms.TextBox txtResult = null!;
    }
}
