using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace RemoteDesktopViewer.CustomControls
{
    public class LayoutBox : GroupBox
    {
        private Color _borderColor = DefaultBackColor;
        private ButtonBorderStyle _borderStyle = ButtonBorderStyle.Solid;

        [Category("Layout Border")]
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                Invalidate();
            }
        }
        [Category("Layout Border")]
        public ButtonBorderStyle BorderStyle
        {
            get => _borderStyle;
            set
            {
                _borderStyle = value;
                Invalidate();
            }
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            InvokePaintBackground(this, e);
            var graphics = e.Graphics;
            var pen = new Pen(_borderColor);
            graphics.DrawLines(pen, new [] {
                new PointF(Margin.Left, Margin.Top),
                new PointF(Width - Margin.Right, Margin.Top),
                new PointF(Width - Margin.Right, Height - Margin.Bottom),
                new PointF(Margin.Left, Height - Margin.Bottom),
                new PointF(Margin.Left, Margin.Top)
            });
        }
    }
}