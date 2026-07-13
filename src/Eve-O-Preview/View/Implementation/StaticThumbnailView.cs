using System;
using System.Drawing;
using System.Windows.Forms;
using EveOPreview.Configuration;
using EveOPreview.Services;

namespace EveOPreview.View
{
	sealed class StaticThumbnailView : ThumbnailView
	{
		#region Private fields
		private readonly PictureBox _thumbnail;
		private IThumbnailConfiguration _config;
		#endregion

		public StaticThumbnailView(IWindowManager windowManager, IThumbnailConfiguration config, IThumbnailManager thumbnailManager, ICharacterLocationTracker characterLocationTracker)
			: base(windowManager, config, thumbnailManager, characterLocationTracker)
		{
			this._thumbnail = new StaticThumbnailImage
			{
				TabStop = false,
				SizeMode = PictureBoxSizeMode.StretchImage,
				Location = new Point(0, 0),
				Size = new Size(this.ClientSize.Width, this.ClientSize.Height)
			};
			this.Controls.Add(this._thumbnail);
			this._config = config;
		}

		protected override void RefreshThumbnail(bool forceRefresh)
		{
			if (!forceRefresh || this.IsPreventPreviews())
			{
				return;
			}

			var thumbnail = this.WindowManager.GetStaticThumbnail(this.Id);
			if (thumbnail != null)
			{
				CropRegion cropRegion = this._config.GetCropRegion(this.Title);
				if (cropRegion != null)
				{
					Image fullThumbnail = thumbnail;
					thumbnail = CropImage(fullThumbnail, cropRegion);
					fullThumbnail.Dispose();
				}

				var oldImage = this._thumbnail.Image;
				this._thumbnail.Image = thumbnail;
				oldImage?.Dispose();
			}
		}

		private static Image CropImage(Image source, CropRegion cropRegion)
		{
			RectangleF normalized = cropRegion.ToRectangleF();
			int left = Math.Clamp((int)Math.Floor(normalized.Left * source.Width), 0, source.Width - 1);
			int top = Math.Clamp((int)Math.Floor(normalized.Top * source.Height), 0, source.Height - 1);
			int right = Math.Clamp((int)Math.Ceiling(normalized.Right * source.Width), left + 1, source.Width);
			int bottom = Math.Clamp((int)Math.Ceiling(normalized.Bottom * source.Height), top + 1, source.Height);
			Rectangle sourceRectangle = Rectangle.FromLTRB(left, top, right, bottom);

			Bitmap cropped = new Bitmap(sourceRectangle.Width, sourceRectangle.Height);
			using (Graphics graphics = Graphics.FromImage(cropped))
			{
				graphics.DrawImage(
					source,
					new Rectangle(0, 0, cropped.Width, cropped.Height),
					sourceRectangle,
					GraphicsUnit.Pixel);
			}

			return cropped;
		}

		protected override void ResizeThumbnail(int baseWidth, int baseHeight, int highlightWidthTop, int highlightWidthRight, int highlightWidthBottom, int highlightWidthLeft)
		{
			var left = 0 + highlightWidthLeft;
			var top = 0 + highlightWidthTop;
			if (this.IsLocationUpdateRequired(this._thumbnail.Location, left, top))
			{
				this._thumbnail.Location = new Point(left, top);
			}

			var width = baseWidth - highlightWidthLeft - highlightWidthRight;
			var height = baseHeight - highlightWidthTop - highlightWidthBottom;
			if (this.IsSizeUpdateRequired(this._thumbnail.Size, width, height))
			{
				this._thumbnail.Size = new Size(width, height);
			}
		}

		private bool IsLocationUpdateRequired(Point currentLocation, int left, int top)
		{
			return (currentLocation.X != left) || (currentLocation.Y != top);
		}

		private bool IsSizeUpdateRequired(Size currentSize, int width, int height)
		{
			return (currentSize.Width != width) || (currentSize.Height != height);
		}
	}
}
