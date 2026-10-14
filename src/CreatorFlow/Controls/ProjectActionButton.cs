using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

public sealed class ProjectActionButton : RoundedButton
{
    public ProjectActionButton()
    {
        BackColor = UITheme.White;
        UseVisualStyleBackColor = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Clear the complete surface so scrolling cannot leave parent text or native button pixels behind the rounded shape.
        e.Graphics.Clear(Parent?.BackColor ?? UITheme.White);
        base.OnPaint(e);
    }
}
