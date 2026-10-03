namespace CreatorFlow.Controls;

partial class ActivityTimelineControl
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.ListView listViewActivity;
    private System.Windows.Forms.ColumnHeader columnTime;
    private System.Windows.Forms.ColumnHeader columnActor;
    private System.Windows.Forms.ColumnHeader columnDescription;

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
        this.listViewActivity = new System.Windows.Forms.ListView();
        this.columnTime = new System.Windows.Forms.ColumnHeader();
        this.columnActor = new System.Windows.Forms.ColumnHeader();
        this.columnDescription = new System.Windows.Forms.ColumnHeader();
        this.SuspendLayout();
        //
        // listViewActivity
        //
        this.listViewActivity.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnTime,
            this.columnActor,
            this.columnDescription});
        this.listViewActivity.Dock = System.Windows.Forms.DockStyle.Fill;
        this.listViewActivity.FullRowSelect = true;
        this.listViewActivity.GridLines = true;
        this.listViewActivity.HideSelection = false;
        this.listViewActivity.Location = new System.Drawing.Point(0, 0);
        this.listViewActivity.MultiSelect = false;
        this.listViewActivity.Name = "listViewActivity";
        this.listViewActivity.Size = new System.Drawing.Size(400, 200);
        this.listViewActivity.TabIndex = 0;
        this.listViewActivity.UseCompatibleStateImageBehavior = false;
        this.listViewActivity.View = System.Windows.Forms.View.Details;
        //
        // columnTime
        //
        this.columnTime.Text = "Thời gian";
        this.columnTime.Width = 110;
        //
        // columnActor
        //
        this.columnActor.Text = "Người thực hiện";
        this.columnActor.Width = 120;
        //
        // columnDescription
        //
        this.columnDescription.Text = "Nội dung";
        this.columnDescription.Width = 260;
        //
        // ActivityTimelineControl
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this.listViewActivity);
        this.Name = "ActivityTimelineControl";
        this.Size = new System.Drawing.Size(400, 200);
        this.ResumeLayout(false);
    }
}