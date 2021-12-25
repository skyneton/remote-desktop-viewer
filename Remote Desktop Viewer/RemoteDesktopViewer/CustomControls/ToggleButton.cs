using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RemoteDesktopViewer.CustomControls
{
    public class ToggleButton : CheckBox
    {
        private Color _onBackColor = Color.LightSteelBlue;
        private Color _onToggleColor = Color.DodgerBlue;
        private string _onText = string.Empty;
        private Color _onForeColor = Color.Black;
        private Font _onFont = DefaultFont;
        
        private Color _offBackColor = Color.Gray;
        private Color _offToggleColor = Color.Gainsboro;
        private string _offText = string.Empty;
        private Color _offForeColor = Color.White;
        private Font _offFont = DefaultFont;

        [Category("Toggle On")]
        public Color OnBackColor
        {
            get => _onBackColor;
            set
            {
                _onBackColor = value;
                Invalidate();
            }
        }

        [Category("Toggle On")]
        public Color OnToggleColor
        {
            get => _onToggleColor;
            set
            {
                _onToggleColor = value;
                Invalidate();
            }
        }

        [Category("Toggle On")]
        public string OnText
        {
            get => _onText;
            set
            {
                _onText = value;
                Invalidate();
            }
        }

        [Category("Toggle On")]
        public Color OnForeColor
        {
            get => _onForeColor;
            set
            {
                _onForeColor = value;
                Invalidate();
            }
        }

        [Category("Toggle On")]
        public Font OnFont
        {
            get => _onFont;
            set
            {
                _onFont = value;
                Invalidate();
            }
        }

        [Category("Toggle Off")]
        public Color OffBackColor
        {
            get => _offBackColor;
            set
            {
                _offBackColor = value;
                Invalidate();
            }
        }

        [Category("Toggle Off")]
        public Color OffToggleColor
        {
            get => _offToggleColor;
            set
            {
                _offToggleColor = value;
                Invalidate();
            }
        }

        [Category("Toggle Off")]
        public string OffText
        {
            get => _offText;
            set
            {
                _offText = value;
                Invalidate();
            }
        }

        [Category("Toggle Off")]
        public Color OffForeColor
        {
            get => _offForeColor;
            set
            {
                _offForeColor = value;
                Invalidate();
            }
        }

        [Category("Toggle Off")]
        public Font OffFont
        {
            get => _offFont;
            set
            {
                _offFont = value;
                Invalidate();
            }
        }

        public GraphicsPath GetFigurePath(int x, int y, int width, int height)
        {
            var size = height - 1;
            var leftArc = new Rectangle(x, y, size, size);
            var rightArc = new Rectangle(x + width - size - 2, y, size, size);

            var path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(leftArc, 90, 180);
            path.AddArc(rightArc, 270, 180);
            path.CloseFigure();

            return path;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            InvokePaintBackground(this, pevent);
            
            var height = string.IsNullOrEmpty(Text) ? Height : Height - FontHeight;
            var y = string.IsNullOrEmpty(Text) ? 0 : FontHeight;
            var toggleSize = height - 5;
            
            pevent.Graphics.DrawString(Text, Font, new SolidBrush(ForeColor), new RectangleF(0, 0, Width, FontHeight));
            
            if (Checked)
            {
                var size = TextRenderer.MeasureText(_onText, _onFont);
                var padding = new Size((Width - size.Width) / 2, (height - size.Height) / 2);
                pevent.Graphics.FillPath(new SolidBrush(_onBackColor), GetFigurePath(0, y, Width, height));
                pevent.Graphics.FillEllipse(new SolidBrush(_onToggleColor), new Rectangle(Width - height + 1, y + 2, toggleSize, toggleSize));
                pevent.Graphics.DrawString(OnText, OnFont, new SolidBrush(OnForeColor),
                    new RectangleF(padding.Width, y + padding.Height, Width - padding.Width, height - padding.Height));
            }
            else
            {
                var size = TextRenderer.MeasureText(_offText, _offFont);
                var padding = new Size((Width - size.Width) / 2, (height - size.Height) / 2);
                pevent.Graphics.FillPath(new SolidBrush(_offBackColor), GetFigurePath(0, y, Width, height));
                pevent.Graphics.FillEllipse(new SolidBrush(_offToggleColor), new Rectangle(2, y + 2, toggleSize, toggleSize));
                pevent.Graphics.DrawString(OffText, OffFont, new SolidBrush(OffForeColor),
                    new RectangleF(padding.Width, y + padding.Height, Width - padding.Width, height - padding.Height));
            }
        }
    }
}