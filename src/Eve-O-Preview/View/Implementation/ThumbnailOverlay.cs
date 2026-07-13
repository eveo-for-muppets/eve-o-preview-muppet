using EveOPreview.Configuration;
using EveOPreview.Services;
using System;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Windows.Shapes;
using Rectangle = System.Drawing.Rectangle;

namespace EveOPreview.View
{
	public partial class ThumbnailOverlay : Form
	{
		#region Private fields
		private readonly Action<object, EventArgs> _areaMouseEnterAction;
		private readonly Action<object, EventArgs> _areaMouseLeaveAction;
		private readonly Action<object, MouseEventArgs> _areaMouseDownAction;
		private readonly Action<object, MouseEventArgs> _areaMouseUpAction;
		private readonly Action<object, MouseEventArgs> _areaMouseMoveAction;
		private bool _showOverlayText = true;
		private string _solarSystemText;
		private Font _solarSystemFont;
		private Color _solarSystemColor;
		private ZoomAnchor _solarSystemAnchor;
		private readonly Label _temporaryGroupLabel;
		private int? _temporaryGroup;
		#endregion

		public ThumbnailOverlay(Form owner,
			Action<object, EventArgs> areaMouseEnterAction,
			Action<object, EventArgs> areaMouseLeaveAction,
			Action<object, MouseEventArgs> areaMouseDownAction,
			Action<object, MouseEventArgs> areaMouseUpAction,
			Action<object, MouseEventArgs> areaMouseMoveAction
			)
		{
			this.Owner = owner;
			this._areaMouseEnterAction = areaMouseEnterAction;
			this._areaMouseLeaveAction = areaMouseLeaveAction;
			this._areaMouseDownAction = areaMouseDownAction;
			this._areaMouseUpAction = areaMouseUpAction;
			this._areaMouseMoveAction = areaMouseMoveAction;

			InitializeComponent();

			this._temporaryGroupLabel = new Label
			{
				AutoSize = false,
				Size = new Size(28, 19),
				TextAlign = ContentAlignment.MiddleCenter,
				Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 8.0f, FontStyle.Bold),
				ForeColor = Color.White,
				Visible = false
			};
			this._temporaryGroupLabel.MouseEnter += this.OverlayArea_MouseEnter;
			this._temporaryGroupLabel.MouseLeave += this.OverlayArea_MouseLeave;
			this._temporaryGroupLabel.MouseDown += this.OverlayArea_MouseDown;
			this._temporaryGroupLabel.MouseUp += this.OverlayArea_MouseUp;
			this._temporaryGroupLabel.MouseMove += this.OverlayArea_MouseMove;
			this.Controls.Add(this._temporaryGroupLabel);
			this._temporaryGroupLabel.BringToFront();
			this.Resize += (sender, args) => this.PositionTemporaryGroupLabel();
		}

		private void OverlayArea_MouseEnter(object sender, EventArgs e)
		{
			this._areaMouseEnterAction(this, e);
		}
		private void OverlayArea_MouseLeave(object sender, EventArgs e)
		{
			this._areaMouseLeaveAction(this, e);
		}
		private void OverlayArea_MouseDown(object sender, MouseEventArgs e)
		{
			this._areaMouseDownAction(this, e);
		}
		private void OverlayArea_MouseUp(object sender, MouseEventArgs e)
		{
			this._areaMouseUpAction(this, e);
		}
		private void OverlayArea_MouseMove(object sender, MouseEventArgs e)
		{
			this._areaMouseMoveAction(this, e);
		}

		public void SetOverlayLabel(string label)
		{
			this.OverlayLabel.Text = label;
		}

		public void SetSolarSystemLabel(string solarSystem, Font font, Color color, ZoomAnchor anchor)
		{
			this._solarSystemText = solarSystem?.Trim();
			this._solarSystemFont = font;
			this._solarSystemColor = color;
			this._solarSystemAnchor = anchor;
		}
		public void SetCycleGroupIndicator(bool displayCycleGroup, ZoomAnchor anchor)
		{
			if (displayCycleGroup)
			{
				this.CycleGroupIndicator.Visible = true;
				int margin = 2;
				int size = Math.Min(Math.Min(this.Height - margin, this.Width - margin), 40);

				this.CycleGroupIndicator.BackColor = this.OverlayAreaPictureBox.BackColor;
				this.CycleGroupIndicator.Width = size;
				this.CycleGroupIndicator.Height = size;

				this.CycleGroupIndicator.Top = 1;
				this.CycleGroupIndicator.Left = this.Width - size - 2;
				switch (anchor)
				{
					case ZoomAnchor.NW:
						this.CycleGroupIndicator.Left = margin;
						this.CycleGroupIndicator.Top = margin;
						break;
					case ZoomAnchor.N:
						this.CycleGroupIndicator.Left = (this.Width / 2) - (this.CycleGroupIndicator.Width / 2);
						this.CycleGroupIndicator.Top = margin;
						break;
					case ZoomAnchor.NE:
						this.CycleGroupIndicator.Left = this.Width - this.CycleGroupIndicator.Width - margin;
						this.CycleGroupIndicator.Top = margin;
						break;
					case ZoomAnchor.W:
						this.CycleGroupIndicator.Left = margin;
						this.CycleGroupIndicator.Top = (this.Height / 2) - (this.CycleGroupIndicator.Height / 2);
						break;
					case ZoomAnchor.C:
						this.CycleGroupIndicator.Left = (this.Width / 2) - (this.CycleGroupIndicator.Width / 2);
						this.CycleGroupIndicator.Top = (this.Height / 2) - (this.CycleGroupIndicator.Height / 2);
						break;
					case ZoomAnchor.E:
						this.CycleGroupIndicator.Left = this.Width - this.CycleGroupIndicator.Width - margin;
						this.CycleGroupIndicator.Top = (this.Height / 2) - (this.CycleGroupIndicator.Height / 2);
						break;
					case ZoomAnchor.SW:
						this.CycleGroupIndicator.Left = margin;
						this.CycleGroupIndicator.Top = this.Height - this.CycleGroupIndicator.Height - margin;
						break;
					case ZoomAnchor.S:
						this.CycleGroupIndicator.Left = (this.Width / 2) - (this.CycleGroupIndicator.Width / 2);
						this.CycleGroupIndicator.Top = this.Height - this.CycleGroupIndicator.Height - margin;
						break;
					case ZoomAnchor.SE:
						this.CycleGroupIndicator.Left = this.Width - this.CycleGroupIndicator.Width - margin;
						this.CycleGroupIndicator.Top = this.Height - this.CycleGroupIndicator.Height - margin;
						break;
				}


			}
			else
			{
				this.CycleGroupIndicator.Visible = false;
			}
		}

		public void SetPropertiesOverlayLabel(Font f, System.Drawing.Color c, ZoomAnchor anchor)
		{
			if (
				this.OverlayLabel.Font.Size != f.Size ||
				this.OverlayLabel.Font.FontFamily != f.FontFamily ||
				this.OverlayLabel.Font.Italic != f.Italic ||
				this.OverlayLabel.Font.Bold != f.Bold
				)
			{
				this.OverlayLabel.Font = f;
			}
			this.OverlayLabel.ForeColor = c;

			int margin = 5;

			switch (anchor)
			{
				case ZoomAnchor.NW:
					this.OverlayLabel.Left = margin;
					this.OverlayLabel.Top = margin;
					this.OverlayLabel.TextAlign = System.Drawing.ContentAlignment.TopLeft;
					break;
				case ZoomAnchor.N:
					this.OverlayLabel.Left = (this.Width / 2) - (this.OverlayLabel.Width / 2);
					this.OverlayLabel.Top = margin;
					this.OverlayLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
					break;
				case ZoomAnchor.NE:
					this.OverlayLabel.Left = this.Width - this.OverlayLabel.Width - margin;
					this.OverlayLabel.Top = margin;
					this.OverlayLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
					break;
				case ZoomAnchor.W:
					this.OverlayLabel.Left = margin;
					this.OverlayLabel.Top = (this.Height / 2) - (this.OverlayLabel.Height / 2);
					this.OverlayLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
					break;
				case ZoomAnchor.C:
					this.OverlayLabel.Left = (this.Width / 2) - (this.OverlayLabel.Width / 2);
					this.OverlayLabel.Top = (this.Height / 2) - (this.OverlayLabel.Height / 2);
					this.OverlayLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
					break;
				case ZoomAnchor.E:
					this.OverlayLabel.Left = this.Width - this.OverlayLabel.Width - margin;
					this.OverlayLabel.Top = (this.Height / 2) - (this.OverlayLabel.Height / 2);
					this.OverlayLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
					break;
				case ZoomAnchor.SW:
					this.OverlayLabel.Left = margin;
					this.OverlayLabel.Top = this.Height - this.OverlayLabel.Height - margin;
					this.OverlayLabel.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
					break;
				case ZoomAnchor.S:
					this.OverlayLabel.Left = (this.Width / 2) - (this.OverlayLabel.Width / 2);
					this.OverlayLabel.Top = this.Height - this.OverlayLabel.Height - margin;
					this.OverlayLabel.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
					break;
				case ZoomAnchor.SE:
					this.OverlayLabel.Left = this.Width - this.OverlayLabel.Width - margin;
					this.OverlayLabel.Top = this.Height - this.OverlayLabel.Height - margin;
					this.OverlayLabel.TextAlign = System.Drawing.ContentAlignment.BottomRight;
					break;
			}
		}

		public void EnableOverlayLabel(bool enable)
		{
			//this.OverlayLabel.Visible = enable;
			this._showOverlayText = enable;
		}
		public void EnableFakePreview(bool enable, bool resizeForHighlight, int highlightSize, Color bgColor)
		{
			bool IsLocationUpdateRequired(Point currentLocation, int left, int top)
			{
				return (currentLocation.X != left) || (currentLocation.Y != top);
			}

			bool IsSizeUpdateRequired(Size currentSize, int width, int height)
			{
				return (currentSize.Width != width) || (currentSize.Height != height);
			}


			if (!enable)
			{
				OverlayAreaPictureBox.BackColor = Color.Transparent;
				OverlayLabel.BackColor = Color.Transparent;
				OverlayAreaPictureBox.Dock = DockStyle.Fill;
			}
			else
			{
				OverlayAreaPictureBox.BackColor = bgColor;
				OverlayLabel.BackColor = Color.Transparent;
				OverlayAreaPictureBox.Dock = DockStyle.None;
			}

			var left = 0 + highlightSize;
			var top = 0 + highlightSize;
			if (IsLocationUpdateRequired(OverlayAreaPictureBox.Location, left, top))
			{
				OverlayAreaPictureBox.Location = new Point(left, top);
			}
			var width = this.ClientSize.Width - (highlightSize * 2);
			var height = this.ClientSize.Height - (highlightSize * 2);
			if (IsSizeUpdateRequired(OverlayAreaPictureBox.Size, width, height))
			{
				OverlayAreaPictureBox.Size = new Size(width, height);
			}
		}

		private void PaintDrawText(PaintEventArgs e, System.Windows.Forms.Label l)
		{
			var flags = TextFormatFlags.Right;
			if (l.TextAlign == ContentAlignment.TopLeft || l.TextAlign == ContentAlignment.BottomLeft || l.TextAlign == ContentAlignment.MiddleLeft) flags = TextFormatFlags.Left;
			flags = flags | TextFormatFlags.WordBreak;

			e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

			TextRenderer.DrawText(e.Graphics, l.Text, l.Font, new Rectangle(l.Left, l.Top, l.Width, l.Height), l.ForeColor, flags);
		}

		private void OverlayAreaPictureBox_Paint(object sender, PaintEventArgs e)
		{
			if (!this._showOverlayText)
			{
				return;
			}

			PaintDrawText(e, OverlayLabel);
			this.PaintSolarSystem(e);
		}

		public void SetTemporaryCycleGroupIndicator(int? group)
		{
			this._temporaryGroup = group is >= 1 and <= 5 ? group : null;
			this._temporaryGroupLabel.Visible = this._temporaryGroup.HasValue;
			if (!this._temporaryGroup.HasValue)
			{
				return;
			}

			Color[] colors =
			{
				Color.RoyalBlue,
				Color.SeaGreen,
				Color.DarkOrange,
				Color.MediumPurple,
				Color.Crimson
			};
			this._temporaryGroupLabel.Text = $"T{this._temporaryGroup.Value}";
			this._temporaryGroupLabel.BackColor = colors[this._temporaryGroup.Value - 1];
			this.PositionTemporaryGroupLabel();
			this._temporaryGroupLabel.BringToFront();
		}

		private void PositionTemporaryGroupLabel()
		{
			const int margin = 3;
			this._temporaryGroupLabel.Left = Math.Max(margin, this.ClientSize.Width - this._temporaryGroupLabel.Width - margin);
			this._temporaryGroupLabel.Top = margin;
		}

		private void PaintSolarSystem(PaintEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(this._solarSystemText) || this._solarSystemFont == null)
			{
				return;
			}

			const int margin = 5;
			TextFormatFlags flags = TextFormatFlags.VerticalCenter |
				TextFormatFlags.SingleLine |
				TextFormatFlags.EndEllipsis |
				TextFormatFlags.NoPadding |
				TextFormatFlags.NoPrefix;
			Size measured = TextRenderer.MeasureText(e.Graphics, this._solarSystemText, this._solarSystemFont, Size.Empty, flags);
			// TextRenderer can underestimate the final glyph bounds by a pixel or two
			// under DPI scaling. Extra vertical room prevents the baseline being clipped.
			int height = Math.Max(1, measured.Height + 4);
			int canvasWidth = this.OverlayAreaPictureBox.ClientSize.Width;
			int canvasHeight = this.OverlayAreaPictureBox.ClientSize.Height;
			int availableWidth = Math.Max(1, canvasWidth - (margin * 2));
			int width = Math.Min(availableWidth, Math.Max(1, measured.Width + 4));
			int left;
			if (this._solarSystemAnchor == ZoomAnchor.N ||
				this._solarSystemAnchor == ZoomAnchor.C ||
				this._solarSystemAnchor == ZoomAnchor.S)
			{
				left = Math.Max(margin, (canvasWidth - width) / 2);
				flags |= TextFormatFlags.HorizontalCenter;
			}
			else if (this._solarSystemAnchor == ZoomAnchor.NE ||
				this._solarSystemAnchor == ZoomAnchor.E ||
				this._solarSystemAnchor == ZoomAnchor.SE)
			{
				left = Math.Max(margin, canvasWidth - width - margin);
				flags |= TextFormatFlags.Right;
			}
			else
			{
				left = margin;
				flags |= TextFormatFlags.Left;
			}

			int top;
			if (this._solarSystemAnchor == ZoomAnchor.W ||
				this._solarSystemAnchor == ZoomAnchor.C ||
				this._solarSystemAnchor == ZoomAnchor.E)
			{
				top = Math.Max(margin, (canvasHeight - height) / 2);
			}
			else if (this._solarSystemAnchor == ZoomAnchor.SW ||
				this._solarSystemAnchor == ZoomAnchor.S ||
				this._solarSystemAnchor == ZoomAnchor.SE)
			{
				top = Math.Max(margin, canvasHeight - height - margin);
			}
			else
			{
				top = margin;
			}

			Rectangle textRectangle = new Rectangle(left, top, width, height);
			Rectangle shadowRectangle = textRectangle;
			shadowRectangle.Offset(1, 1);
			TextRenderer.DrawText(e.Graphics, this._solarSystemText, this._solarSystemFont, shadowRectangle, Color.Black, flags);
			TextRenderer.DrawText(e.Graphics, this._solarSystemText, this._solarSystemFont, textRectangle, this._solarSystemColor, flags);
		}

		protected override CreateParams CreateParams
		{
			get
			{
				var Params = base.CreateParams;
				Params.ExStyle |= (int)InteropConstants.WS_EX_TOOLWINDOW;
				return Params;
			}
		}
	}
}
