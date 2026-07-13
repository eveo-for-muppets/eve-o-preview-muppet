using System;
using System.Drawing;

namespace EveOPreview.Configuration
{
	/// <summary>
	/// A source crop stored as normalized coordinates so it survives source
	/// window resolution and DPI changes.
	/// </summary>
	public sealed class CropRegion
	{
		public CropRegion()
		{
		}

		public CropRegion(double x, double y, double width, double height)
		{
			this.X = x;
			this.Y = y;
			this.Width = width;
			this.Height = height;
		}

		public double X { get; set; }
		public double Y { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }

		public bool IsValid =>
			double.IsFinite(this.X) &&
			double.IsFinite(this.Y) &&
			double.IsFinite(this.Width) &&
			double.IsFinite(this.Height) &&
			this.X >= 0.0 &&
			this.Y >= 0.0 &&
			this.Width > 0.0 &&
			this.Height > 0.0 &&
			this.X + this.Width <= 1.0 &&
			this.Y + this.Height <= 1.0;

		public RectangleF ToRectangleF()
		{
			return new RectangleF(
				(float)this.X,
				(float)this.Y,
				(float)this.Width,
				(float)this.Height);
		}
	}
}
