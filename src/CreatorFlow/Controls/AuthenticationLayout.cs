namespace CreatorFlow.Controls;

internal static class AuthenticationLayout
{
    public static void ConfigureResponsiveLayout(
        TableLayoutPanel columns,
        Panel branding,
        Panel formArea,
        Control formColumn)
    {
        Form? owner = columns.FindForm();
        bool? previousWideLayout = null;

        void PositionFormColumn()
        {
            float scale = columns.DeviceDpi / 96f;
            int availableWidth = Math.Max(1, formArea.ClientSize.Width - formArea.Padding.Horizontal -
                SystemInformation.VerticalScrollBarWidth);
            int targetWidth = Math.Min((int)Math.Round(440 * scale), availableWidth);
            if (formColumn.MinimumSize.Width != targetWidth)
            {
                formColumn.MinimumSize = new Size(targetWidth, 0);
                formColumn.MaximumSize = new Size(targetWidth, 0);
            }

            formColumn.Left = Math.Max(formArea.Padding.Left,
                (formArea.ClientSize.Width - formColumn.Width) / 2);
            formColumn.Top = formArea.Padding.Top;
        }

        void UpdateLayout()
        {
            float scale = columns.DeviceDpi / 96f;
            bool wide = columns.ClientSize.Width >= (int)Math.Round(1000 * scale);
            if (previousWideLayout != wide)
            {
                previousWideLayout = wide;
                branding.Visible = wide;
                columns.ColumnStyles[0].SizeType = wide ? SizeType.Percent : SizeType.Absolute;
                columns.ColumnStyles[0].Width = wide ? 42 : 0;
                columns.ColumnStyles[1].Width = wide ? 58 : 100;
                columns.PerformLayout();
            }

            PositionFormColumn();
        }

        if (owner is not null) owner.Resize += (_, _) => UpdateLayout();
        formArea.Layout += (_, _) => PositionFormColumn();
        UpdateLayout();
    }
}
