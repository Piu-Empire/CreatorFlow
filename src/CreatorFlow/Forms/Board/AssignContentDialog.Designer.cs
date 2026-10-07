#nullable disable
using CreatorFlow.Controls;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Board;

partial class AssignContentDialog
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Label lblHeader;
    private System.Windows.Forms.Label lblSub;
    private System.Windows.Forms.Panel pnlDivider;
    private System.Windows.Forms.Label lblCreator;
    private System.Windows.Forms.Label lblCreatorNote;
    private System.Windows.Forms.Panel pnlCreators;
    private System.Windows.Forms.Label _lblError;
    private System.Windows.Forms.Panel pnlDivider2;
    private CreatorFlow.Controls.RoundedButton btnCancel;
    private CreatorFlow.Controls.RoundedButton btnSubmit;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.lblHeader = new System.Windows.Forms.Label();
        this.lblSub = new System.Windows.Forms.Label();
        this.pnlDivider = new System.Windows.Forms.Panel();
        this.lblCreator = new System.Windows.Forms.Label();
        this.lblCreatorNote = new System.Windows.Forms.Label();
        this.pnlCreators = new System.Windows.Forms.Panel();
        this._lblError = new System.Windows.Forms.Label();
        this.pnlDivider2 = new System.Windows.Forms.Panel();
        this.btnCancel = new CreatorFlow.Controls.RoundedButton();
        this.btnSubmit = new CreatorFlow.Controls.RoundedButton();
        this.SuspendLayout();
        // 
        // lblHeader
        // 
        this.lblHeader.AutoSize = true;
        this.lblHeader.Font = UITheme.FontH2;
        this.lblHeader.ForeColor = UITheme.Ink;
        this.lblHeader.Location = new System.Drawing.Point(28, 24);
        this.lblHeader.Name = "lblHeader";
        this.lblHeader.TabIndex = 0;
        this.lblHeader.Text = "Giao việc cho Creator";
        // 
        // lblSub  (mã + tiêu đề Content, gán trong code-behind)
        // 
        this.lblSub.AutoEllipsis = true;
        this.lblSub.Font = UITheme.FontBody;
        this.lblSub.ForeColor = UITheme.Neutral600;
        this.lblSub.Location = new System.Drawing.Point(28, 52);
        this.lblSub.Name = "lblSub";
        this.lblSub.Size = new System.Drawing.Size(464, 22);
        this.lblSub.TabIndex = 1;
        this.lblSub.UseMnemonic = false;
        // 
        // pnlDivider
        // 
        this.pnlDivider.BackColor = UITheme.Neutral200;
        this.pnlDivider.Location = new System.Drawing.Point(28, 86);
        this.pnlDivider.Name = "pnlDivider";
        this.pnlDivider.Size = new System.Drawing.Size(464, 1);
        this.pnlDivider.TabIndex = 2;
        // 
        // lblCreator
        // 
        this.lblCreator.AutoSize = true;
        this.lblCreator.Font = UITheme.FontLabelBold;
        this.lblCreator.ForeColor = UITheme.Neutral700;
        this.lblCreator.Location = new System.Drawing.Point(28, 102);
        this.lblCreator.Name = "lblCreator";
        this.lblCreator.TabIndex = 3;
        this.lblCreator.Text = "Creator nhận việc *";
        // 
        // lblCreatorNote
        // 
        this.lblCreatorNote.Font = UITheme.FontLabel;
        this.lblCreatorNote.ForeColor = UITheme.Neutral600;
        this.lblCreatorNote.Location = new System.Drawing.Point(28, 122);
        this.lblCreatorNote.Name = "lblCreatorNote";
        this.lblCreatorNote.Size = new System.Drawing.Size(464, 36);
        this.lblCreatorNote.TabIndex = 4;
        this.lblCreatorNote.Text = "Tick một hoặc nhiều Creator thuộc Project này. Mỗi người có deadline riêng; bỏ tick ô ngày = giữ deadline hiện tại (hoặc chưa đặt).";
        this.lblCreatorNote.UseMnemonic = false;
        // 
        // pnlCreators  (danh sách Creator: mỗi dòng gồm tick chọn + deadline riêng, được dựng trong code-behind)
        // 
        this.pnlCreators.AutoScroll = true;
        this.pnlCreators.BackColor = UITheme.White;
        this.pnlCreators.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this.pnlCreators.Location = new System.Drawing.Point(28, 164);
        this.pnlCreators.Name = "pnlCreators";
        this.pnlCreators.Size = new System.Drawing.Size(464, 188);
        this.pnlCreators.TabIndex = 5;
        // 
        // _lblError  (lỗi ngay dưới danh sách, đỏ #DC2626, 11px đậm — style guide mục 13)
        // 
        this._lblError.Font = UITheme.FontLabelBold;
        this._lblError.ForeColor = UITheme.Danger;
        this._lblError.Location = new System.Drawing.Point(28, 358);
        this._lblError.Name = "_lblError";
        this._lblError.Size = new System.Drawing.Size(464, 36);
        this._lblError.TabIndex = 6;
        this._lblError.UseMnemonic = false;
        // 
        // pnlDivider2
        // 
        this.pnlDivider2.BackColor = UITheme.Neutral200;
        this.pnlDivider2.Location = new System.Drawing.Point(28, 402);
        this.pnlDivider2.Name = "pnlDivider2";
        this.pnlDivider2.Size = new System.Drawing.Size(464, 1);
        this.pnlDivider2.TabIndex = 7;
        // 
        // btnCancel
        // 
        this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnCancel.Location = new System.Drawing.Point(272, 418);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(90, 36);
        this.btnCancel.Style = RoundButtonStyle.Secondary;
        this.btnCancel.TabIndex = 9;
        this.btnCancel.Text = "Huỷ";
        this.btnCancel.UseVisualStyleBackColor = true;
        // 
        // btnSubmit
        // 
        this.btnSubmit.Location = new System.Drawing.Point(372, 418);
        this.btnSubmit.Name = "btnSubmit";
        this.btnSubmit.Size = new System.Drawing.Size(120, 36);
        this.btnSubmit.Style = RoundButtonStyle.Primary;
        this.btnSubmit.TabIndex = 8;
        this.btnSubmit.Text = "Giao việc";
        this.btnSubmit.UseVisualStyleBackColor = true;
        this.btnSubmit.Click += new System.EventHandler(this.btnSubmit_Click);
        // 
        // AssignContentDialog
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
        this.BackColor = UITheme.White;
        this.ClientSize = new System.Drawing.Size(520, 478);
        this.Controls.Add(this.lblHeader);
        this.Controls.Add(this.lblSub);
        this.Controls.Add(this.pnlDivider);
        this.Controls.Add(this.lblCreator);
        this.Controls.Add(this.lblCreatorNote);
        this.Controls.Add(this.pnlCreators);
        this.Controls.Add(this._lblError);
        this.Controls.Add(this.pnlDivider2);
        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnSubmit);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "AssignContentDialog";
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Giao việc cho Creator";
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}