using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using EveOPreview.Configuration;

namespace EveOPreview.View
{
	sealed class CropSelectionControl : Control
	{
		private Image _image;
		private CropRegion _selectedRegion;
		private bool _isDragging;
		private PointF _dragStart;

		public CropSelectionControl(Image image, CropRegion initialRegion)
		{
			this._image = image ?? throw new ArgumentNullException(nameof(image));
			this._selectedRegion = initialRegion?.IsValid == true
				? new CropRegion(initialRegion.X, initialRegion.Y, initialRegion.Width, initialRegion.Height)
				: null;

			this.BackColor = Color.Black;
			this.Cursor = Cursors.Cross;
			this.DoubleBuffered = true;
			this.SetStyle(ControlStyles.ResizeRedraw, true);
		}

		public CropRegion SelectedRegion => this._selectedRegion;

		public Rectangle ImageBounds => this.GetImageBounds();

		public void SetImage(Image image)
		{
			this._image = image ?? throw new ArgumentNullException(nameof(image));
			this.Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			Rectangle imageBounds = this.GetImageBounds();
			if (imageBounds.Width <= 0 || imageBounds.Height <= 0)
			{
				return;
			}

			e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			e.Graphics.DrawImage(this._image, imageBounds);

			if (this._selectedRegion?.IsValid != true)
			{
				using Pen imageBorder = new Pen(Color.DimGray, 1.0f);
				e.Graphics.DrawRectangle(imageBorder, imageBounds);
				return;
			}

			Rectangle selection = this.ToClientRectangle(this._selectedRegion, imageBounds);
			using (SolidBrush shade = new SolidBrush(Color.FromArgb(155, Color.Black)))
			{
				e.Graphics.FillRectangle(shade, imageBounds.Left, imageBounds.Top, imageBounds.Width, Math.Max(0, selection.Top - imageBounds.Top));
				e.Graphics.FillRectangle(shade, imageBounds.Left, selection.Bottom, imageBounds.Width, Math.Max(0, imageBounds.Bottom - selection.Bottom));
				e.Graphics.FillRectangle(shade, imageBounds.Left, selection.Top, Math.Max(0, selection.Left - imageBounds.Left), selection.Height);
				e.Graphics.FillRectangle(shade, selection.Right, selection.Top, Math.Max(0, imageBounds.Right - selection.Right), selection.Height);
			}

			using Pen selectionBorder = new Pen(Color.LimeGreen, 2.0f);
			e.Graphics.DrawRectangle(selectionBorder, selection);

			string dimensions = $"{Math.Round(this._selectedRegion.Width * 100.0)}% × {Math.Round(this._selectedRegion.Height * 100.0)}%";
			TextRenderer.DrawText(
				e.Graphics,
				dimensions,
				this.Font,
				new Point(selection.Left + 5, selection.Top + 5),
				Color.White,
				Color.FromArgb(170, Color.Black));
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.Button != MouseButtons.Left || !this.TryGetNormalizedPoint(e.Location, out PointF point))
			{
				return;
			}

			this._isDragging = true;
			this._dragStart = point;
			this._selectedRegion = null;
			this.Capture = true;
			this.Invalidate();
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (!this._isDragging)
			{
				return;
			}

			PointF current = this.GetClampedNormalizedPoint(e.Location);
			this.SetSelection(this._dragStart, current);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (e.Button != MouseButtons.Left || !this._isDragging)
			{
				return;
			}

			this._isDragging = false;
			this.Capture = false;
			PointF current = this.GetClampedNormalizedPoint(e.Location);
			this.SetSelection(this._dragStart, current);

			if (this._selectedRegion != null &&
				(this._selectedRegion.Width < 0.002 || this._selectedRegion.Height < 0.002))
			{
				this._selectedRegion = null;
				this.Invalidate();
			}
		}

		private void SetSelection(PointF first, PointF second)
		{
			double left = Math.Min(first.X, second.X);
			double top = Math.Min(first.Y, second.Y);
			double right = Math.Max(first.X, second.X);
			double bottom = Math.Max(first.Y, second.Y);

			this._selectedRegion = new CropRegion(left, top, right - left, bottom - top);
			this.Invalidate();
		}

		private bool TryGetNormalizedPoint(Point location, out PointF point)
		{
			Rectangle imageBounds = this.GetImageBounds();
			if (!imageBounds.Contains(location))
			{
				point = PointF.Empty;
				return false;
			}

			point = new PointF(
				(float)(location.X - imageBounds.Left) / imageBounds.Width,
				(float)(location.Y - imageBounds.Top) / imageBounds.Height);
			return true;
		}

		private PointF GetClampedNormalizedPoint(Point location)
		{
			Rectangle imageBounds = this.GetImageBounds();
			float x = Math.Clamp((float)(location.X - imageBounds.Left) / imageBounds.Width, 0.0f, 1.0f);
			float y = Math.Clamp((float)(location.Y - imageBounds.Top) / imageBounds.Height, 0.0f, 1.0f);
			return new PointF(x, y);
		}

		private Rectangle GetImageBounds()
		{
			if (this.ClientSize.Width <= 0 || this.ClientSize.Height <= 0)
			{
				return Rectangle.Empty;
			}

			double scale = Math.Min(
				(double)this.ClientSize.Width / this._image.Width,
				(double)this.ClientSize.Height / this._image.Height);
			int width = Math.Max(1, (int)Math.Round(this._image.Width * scale));
			int height = Math.Max(1, (int)Math.Round(this._image.Height * scale));
			return new Rectangle(
				(this.ClientSize.Width - width) / 2,
				(this.ClientSize.Height - height) / 2,
				width,
				height);
		}

		private Rectangle ToClientRectangle(CropRegion region, Rectangle imageBounds)
		{
			int left = imageBounds.Left + (int)Math.Round(region.X * imageBounds.Width);
			int top = imageBounds.Top + (int)Math.Round(region.Y * imageBounds.Height);
			int right = imageBounds.Left + (int)Math.Round((region.X + region.Width) * imageBounds.Width);
			int bottom = imageBounds.Top + (int)Math.Round((region.Y + region.Height) * imageBounds.Height);
			return Rectangle.FromLTRB(left, top, right, bottom);
		}
	}
}
