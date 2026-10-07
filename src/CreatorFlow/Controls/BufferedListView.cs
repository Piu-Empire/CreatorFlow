namespace CreatorFlow.Controls;

/// <summary>ListView bật double-buffer để vẽ tay (OwnerDraw) không bị nhấp nháy.</summary>
public class BufferedListView : ListView
{
    public BufferedListView()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
    }
}