using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using EveOPreview.Configuration;
using EveOPreview.Services;

namespace EveOPreview.View
{
	sealed class CropRegionSelectorForm : Form
	{
		private Image _sourceImage;
		private readonly CropSelectionControl _selectionControl;
		private readonly IWindowManager _windowManager;
		private readonly IntPtr _sourceHandle;

		public CropRegionSelectorForm(
			string clientTitle,
			Image sourceImage,
			IWindowManager windowManager,
			IntPtr sourceHandle,
			CropRegion initialRegion)
		{
			this._sourceImage = sourceImage ?? throw new ArgumentNullException(nameof(sourceImage));
			this._windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
			this._sourceHandle = sourceHandle;

			this.Text = $"Select crop region - {clientTitle}";
			this.StartPosition = FormStartPosition.CenterScreen;
			this.Size = new Size(1000, 700);
			this.MinimumSize = new Size(640, 480);
			this.FormBorderStyle = FormBorderStyle.Sizable;
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.KeyPreview = true;
			this.Shown += this.CropRegionSelectorForm_Shown;

			Label instructions = new Label
			{
				Dock = DockStyle.Top,
				Height = 42,
				Padding = new Padding(10, 8, 10, 4),
				Text = "Drag over the part of this client that its thumbnail should display.",
				TextAlign = ContentAlignment.MiddleLeft
			};

			this._selectionControl = new CropSelectionControl(this._sourceImage, initialRegion)
			{
				Dock = DockStyle.Fill,
				Margin = new Padding(0)
			};

			FlowLayoutPanel buttons = new FlowLayoutPanel
			{
				Dock = DockStyle.Bottom,
				Height = 54,
				FlowDirection = FlowDirection.RightToLeft,
				Padding = new Padding(8),
				WrapContents = false
			};

			Button applyButton = new Button
			{
				Text = "Apply crop",
				AutoSize = true,
				Height = 34
			};
			applyButton.Click += this.ApplyButton_Click;

			Button cancelButton = new Button
			{
				Text = "Cancel",
				AutoSize = true,
				Height = 34,
				DialogResult = DialogResult.Cancel
			};

			buttons.Controls.Add(applyButton);
			buttons.Controls.Add(cancelButton);
			this.Controls.Add(this._selectionControl);
			this.Controls.Add(buttons);
			this.Controls.Add(instructions);

			this.AcceptButton = applyButton;
			this.CancelButton = cancelButton;
		}

		public CropRegion SelectedRegion => this._selectionControl.SelectedRegion;

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				this._sourceImage.Dispose();
			}

			base.Dispose(disposing);
		}

		private void CropRegionSelectorForm_Shown(object sender, EventArgs e)
		{
			// Run after ShowDialog has created and displayed the top-level window.
			// DWM can then render the source client into this form even when the
			// source is covered, minimized, or backed by a hardware surface that
			// cannot be copied correctly through GetDC/BitBlt.
			this.BeginInvoke((Action)this.CaptureDwmPreview);
		}

		private void CaptureDwmPreview()
		{
			Rectangle imageBounds = this._selectionControl.ImageBounds;
			if (imageBounds.Width <= 0 || imageBounds.Height <= 0)
			{
				this._selectionControl.Visible = true;
				return;
			}

			Rectangle destination = new Rectangle(
				this._selectionControl.Left + imageBounds.Left,
				this._selectionControl.Top + imageBounds.Top,
				imageBounds.Width,
				imageBounds.Height);

			IDwmThumbnail thumbnail = null;
			bool previousTopMost = this.TopMost;
			try
			{
				this._selectionControl.Visible = false;
				this.TopMost = true;
				this.BringToFront();
				this.Activate();
				this.Refresh();

				thumbnail = this._windowManager.GetLiveThumbnail(this.Handle, this._sourceHandle);
				thumbnail.Move(destination.Left, destination.Top, destination.Right, destination.Bottom);
				thumbnail.SetSourceRegion(null);
				thumbnail.Update();

				// Allow the desktop compositor to publish at least one completed frame.
				Application.DoEvents();
				Thread.Sleep(100);
				Application.DoEvents();

				Bitmap capturedImage = new Bitmap(destination.Width, destination.Height);
				using (Graphics graphics = Graphics.FromImage(capturedImage))
				{
					graphics.CopyFromScreen(
						this.PointToScreen(destination.Location),
						Point.Empty,
						destination.Size,
						CopyPixelOperation.SourceCopy);
				}

				Image oldImage = this._sourceImage;
				this._sourceImage = capturedImage;
				this._selectionControl.SetImage(capturedImage);
				oldImage.Dispose();
			}
			catch (Exception)
			{
				// Keep the original static image as a compatibility fallback.
			}
			finally
			{
				thumbnail?.Unregister();
				this._selectionControl.Visible = true;
				this.TopMost = previousTopMost;
				this.BringToFront();
				this.Activate();
			}
		}

		private void ApplyButton_Click(object sender, EventArgs e)
		{
			if (this.SelectedRegion?.IsValid != true)
			{
				MessageBox.Show(
					this,
					"Drag a crop rectangle over the client image first.",
					"No crop selected",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				return;
			}

			this.DialogResult = DialogResult.OK;
			this.Close();
		}
	}
}
