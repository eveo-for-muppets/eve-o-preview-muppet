using EveOPreview.Configuration;
using EveOPreview.Configuration.Implementation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace EveOPreview.View
{
	public partial class MainForm : Form, IMainFormView
	{
		private sealed class CropPresetComboItem
		{
			public string Id { get; init; }
			public string Name { get; init; }
			public override string ToString() => this.Name;
		}

		#region Private fields
		private readonly ApplicationContext _context;
		private readonly Dictionary<ViewZoomAnchor, RadioButton> _zoomAnchorMap;
		private readonly Dictionary<ViewZoomAnchor, RadioButton> _overlayLabelMap;
		private readonly Dictionary<ViewZoomAnchor, RadioButton> _cycleGroupIndicatorMap;
		private ViewZoomAnchor _cachedThumbnailZoomAnchor;
		private ViewZoomAnchor _cachedOverlayLabelAnchor;
		private ViewZoomAnchor _cachedCycleGroupIndicatorAnchor;
		private bool _suppressEvents;
		private Size _minimumSize;
		private Size _maximumSize;
		private string _iconName;
		private bool _hotkeyCaptureActive = false;
		private Dictionary<string, string> _configurationFilenames = new Dictionary<string, string>();
		private System.Windows.Forms.Button _selectCropButton;
		private System.Windows.Forms.Button _resetCropButton;
		private CropPresetsPanel _cropPresetsPanel;
		private System.Windows.Forms.ComboBox _cycleCropPresetCombo;
		private System.Windows.Forms.Button _cycleCropApplyButton;
		private System.Windows.Forms.CheckBox _showSolarSystemOverlayCheckBox;
		#endregion

		public MainForm(ApplicationContext context)
		{
			this._context = context;
			this._zoomAnchorMap = new Dictionary<ViewZoomAnchor, RadioButton>();
			this._overlayLabelMap = new Dictionary<ViewZoomAnchor, RadioButton>();
			this._cycleGroupIndicatorMap = new Dictionary<ViewZoomAnchor, RadioButton>();
			this._cachedThumbnailZoomAnchor = ViewZoomAnchor.NW;
			this._suppressEvents = false;
			this._minimumSize = new Size(20, 20);
			this._maximumSize = new Size(20, 20);

			InitializeComponent();
			this.InitializeCropPresetsTab();
			this.InitializeCycleGroupCropControls();
			this.InitializeSolarSystemOverlayControl();

			this.ThumbnailsList.DisplayMember = "Title";

			SetupConfigList();

			this.InitZoomAnchorMap();
			this.InitOverlayLabelMap();
			this.InitCycleGroupIndicatorMap();
			this.InitFormSize();

			this.AnimationStyleCombo.DataSource = Enum.GetValues(typeof(AnimationStyle));
			this.CaptionOnClientsStyleCombo.DataSource = Enum.GetValues(typeof(CaptionBarStyle));
		}

		public bool MinimizeToTray
		{
			get => this.MinimizeToTrayCheckBox.Checked;
			set => this.MinimizeToTrayCheckBox.Checked = value;
		}

		public string IconName
		{
			get => this._iconName;
			set
			{


				this._iconName = value;

				// Set Icon 
				System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
				if (this._iconName == null || ((resources.GetObject(this._iconName))) == null)
				{
					this._iconName = "IconOriginal";
				}

				// pull icon from resources
				try
				{
					var iconBytes = (byte[])resources.GetObject(this._iconName);
					using (MemoryStream ms = new MemoryStream(iconBytes))
					{
						this.Icon = new Icon(ms);
						this.NotifyIcon.Icon = this.Icon;
					}
				}
				catch (Exception ex)
				{
					// Log ?
				}

				if (value != "")
				{
					this.ApplicationSettingsChanged?.Invoke();
				}
			}
		}

		public string Language
		{
			get => this.LanguageCombo.Text;
			set
			{
				this.LanguageCombo.Text = value;
			}
		}

		public double ThumbnailOpacity
		{
			get => Math.Min(this.ThumbnailOpacityTrackBar.Value / 100.00, 1.00);
			set
			{
				int barValue = (int)(100.0 * value);
				if (barValue > 100)
				{
					barValue = 100;
				}
				else if (barValue < 10)
				{
					barValue = 10;
				}

				this.ThumbnailOpacityTrackBar.Value = barValue;
			}
		}

		public bool EnableClientLayoutTracking
		{
			get => this.EnableClientLayoutTrackingCheckBox.Checked;
			set => this.EnableClientLayoutTrackingCheckBox.Checked = value;
		}

		public bool HideActiveClientThumbnail
		{
			get => this.HideActiveClientThumbnailCheckBox.Checked;
			set => this.HideActiveClientThumbnailCheckBox.Checked = value;
		}

		public bool MinimizeInactiveClients
		{
			get => this.MinimizeInactiveClientsCheckBox.Checked;
			set => this.MinimizeInactiveClientsCheckBox.Checked = value;
		}
		public ViewCaptionBarStyle CaptionOnClientsStyle
		{
			get => (ViewCaptionBarStyle)this.CaptionOnClientsStyleCombo.SelectedItem;
			set => this.CaptionOnClientsStyleCombo.SelectedIndex = (int)value;
		}
		public ViewAnimationStyle WindowsAnimationStyle
		{
			get => (ViewAnimationStyle)this.AnimationStyleCombo.SelectedItem;
			set => this.AnimationStyleCombo.SelectedIndex = (int)value;
		}

		public bool ShowThumbnailsAlwaysOnTop
		{
			get => this.ShowThumbnailsAlwaysOnTopCheckBox.Checked;
			set => this.ShowThumbnailsAlwaysOnTopCheckBox.Checked = value;
		}
		public bool PreventPreviews
		{
			get => this.PreventPreviewsCheckBox.Checked;
			set => this.PreventPreviewsCheckBox.Checked = value;
		}

		public bool HideThumbnailsOnLostFocus
		{
			get => this.HideThumbnailsOnLostFocusCheckBox.Checked;
			set => this.HideThumbnailsOnLostFocusCheckBox.Checked = value;
		}

		public bool EnablePerClientThumbnailLayouts
		{
			get => this.EnablePerClientThumbnailsLayoutsCheckBox.Checked;
			set => this.EnablePerClientThumbnailsLayoutsCheckBox.Checked = value;
		}

		public Size ThumbnailSize
		{
			get => new Size((int)this.ThumbnailsWidthNumericEdit.Value, (int)this.ThumbnailsHeightNumericEdit.Value);
			set
			{
				this.ThumbnailsWidthNumericEdit.Value = value.Width;
				this.ThumbnailsHeightNumericEdit.Value = value.Height;
			}
		}

		public bool EnableThumbnailZoom
		{
			get => this.EnableThumbnailZoomCheckBox.Checked;
			set
			{
				this.EnableThumbnailZoomCheckBox.Checked = value;
				this.RefreshZoomSettings();
			}
		}

		public int ThumbnailZoomFactor
		{
			get => (int)this.ThumbnailZoomFactorNumericEdit.Value;
			set => this.ThumbnailZoomFactorNumericEdit.Value = value;
		}

		public ViewZoomAnchor ThumbnailZoomAnchor
		{
			get
			{
				if (this._zoomAnchorMap[this._cachedThumbnailZoomAnchor].Checked)
				{
					return this._cachedThumbnailZoomAnchor;
				}

				foreach (KeyValuePair<ViewZoomAnchor, RadioButton> valuePair in this._zoomAnchorMap)
				{
					if (!valuePair.Value.Checked)
					{
						continue;
					}

					this._cachedThumbnailZoomAnchor = valuePair.Key;
					return this._cachedThumbnailZoomAnchor;
				}

				// Default value
				return ViewZoomAnchor.NW;
			}
			set
			{
				this._cachedThumbnailZoomAnchor = value;
				this._zoomAnchorMap[this._cachedThumbnailZoomAnchor].Checked = true;
			}
		}

		public ViewZoomAnchor OverlayLabelAnchor
		{
			get
			{
				if (this._overlayLabelMap[this._cachedOverlayLabelAnchor].Checked)
				{
					return this._cachedOverlayLabelAnchor;
				}

				foreach (KeyValuePair<ViewZoomAnchor, RadioButton> valuePair in this._overlayLabelMap)
				{
					if (!valuePair.Value.Checked)
					{
						continue;
					}

					this._cachedOverlayLabelAnchor = valuePair.Key;
					return this._cachedOverlayLabelAnchor;
				}

				// Default Value
				return ViewZoomAnchor.NW;
			}
			set
			{
				this._cachedOverlayLabelAnchor = value;
				this._overlayLabelMap[this._cachedOverlayLabelAnchor].Checked = true;
			}
		}

		public ViewZoomAnchor CycleGroupIndicatorAnchor
		{
			get
			{
				if (this._cycleGroupIndicatorMap[this._cachedCycleGroupIndicatorAnchor].Checked)
				{
					return this._cachedCycleGroupIndicatorAnchor;
				}

				foreach (KeyValuePair<ViewZoomAnchor, RadioButton> valuePair in this._cycleGroupIndicatorMap)
				{
					if (!valuePair.Value.Checked)
					{
						continue;
					}

					this._cachedCycleGroupIndicatorAnchor = valuePair.Key;
					return this._cachedCycleGroupIndicatorAnchor;
				}

				// Default Value
				return ViewZoomAnchor.NW;
			}
			set
			{
				this._cachedCycleGroupIndicatorAnchor = value;
				this._cycleGroupIndicatorMap[this._cachedCycleGroupIndicatorAnchor].Checked = true;
			}
		}

		public bool ShowThumbnailOverlays
		{
			get => this.ShowThumbnailOverlaysCheckBox.Checked;
			set => this.ShowThumbnailOverlaysCheckBox.Checked = value;
		}

		public bool ShowSolarSystemOverlay
		{
			get => this._showSolarSystemOverlayCheckBox.Checked;
			set => this._showSolarSystemOverlayCheckBox.Checked = value;
		}

		public bool ShowThumbnailFrames
		{
			get => this.ShowThumbnailFramesCheckBox.Checked;
			set => this.ShowThumbnailFramesCheckBox.Checked = value;
		}
		public bool LockThumbnailLocation
		{
			get => this.LockThumbnailLocationCheckbox.Checked;
			set => this.LockThumbnailLocationCheckbox.Checked = value;
		}
		public bool ThumbnailSnapToGrid
		{
			get => this.ThumbnailSnapToGridCheckBox.Checked;
			set => this.ThumbnailSnapToGridCheckBox.Checked = value;
		}
		public int ThumbnailSnapToGridSizeX
		{
			get => (int)ThumbnailSnapToGridSizeXNumericEdit.Value;
			set => ThumbnailSnapToGridSizeXNumericEdit.Value = value;
		}
		public int ThumbnailSnapToGridSizeY
		{
			get => (int)ThumbnailSnapToGridSizeYNumericEdit.Value;
			set => ThumbnailSnapToGridSizeYNumericEdit.Value = value;
		}

		public bool EnableActiveClientHighlight
		{
			get => this.EnableActiveClientHighlightCheckBox.Checked;
			set => this.EnableActiveClientHighlightCheckBox.Checked = value;
		}

		public Color ActiveClientHighlightColor
		{
			get => this._activeClientHighlightColor;
			set
			{
				this._activeClientHighlightColor = value;
				this.ActiveClientHighlightColorButton.BackColor = value;
			}
		}
		private Color _activeClientHighlightColor;

		public Color PreventPreviewColor
		{
			get => this._preventPreviewColor;
			set
			{
				this._preventPreviewColor = value;
				this.PreventPreviewColorButton.BackColor = value;
			}
		}
		private Color _preventPreviewColor;

		public Color OverlayLabelColor
		{
			get => this._OverlayLabelColor;
			set
			{
				this._OverlayLabelColor = value;
				this.OverlayLabelColorButton.BackColor = value;
			}
		}
		private Color _OverlayLabelColor;

		public Font OverlayLabelFont
		{
			get => (Font)this._OverlayLabelFont;
			set
			{
				this._OverlayLabelFont = value;
				this.LabelOverlayLabelFont.Font = value;
			}
		}
		private Font _OverlayLabelFont;

		public new void Show()
		{
			// Registers the current instance as the application's Main Form
			this._context.MainForm = this;

			this._suppressEvents = true;
			this.FormActivated?.Invoke();
			this._suppressEvents = false;

			Application.Run(this._context);
		}

		public void SetThumbnailSizeLimitations(Size minimumSize, Size maximumSize)
		{
			this._minimumSize = minimumSize;
			this._maximumSize = maximumSize;
		}

		public void Minimize()
		{
			this.WindowState = FormWindowState.Minimized;
		}

		public void SetVersionInfo(string version)
		{
			this.VersionLabel.Text = version;
		}

		public void SetDocumentationUrl(string url)
		{
			this.DocumentationLink.Text = url;
		}

		public void AddThumbnails(IList<IThumbnailDescription> thumbnails)
		{
			this.ThumbnailsList.BeginUpdate();

			foreach (IThumbnailDescription view in thumbnails)
			{
				this.ThumbnailsList.SetItemChecked(this.ThumbnailsList.Items.Add(view), view.IsDisabled);

				if (!this.HotkeysClientsList.Items.Contains(view.Title)) this.HotkeysClientsList.Items.Add(view.Title, false);

			}

			this.ThumbnailsList.EndUpdate();
		}

		public void RemoveThumbnails(IList<IThumbnailDescription> thumbnails)
		{
			this.ThumbnailsList.BeginUpdate();

			foreach (IThumbnailDescription view in thumbnails)
			{
				this.ThumbnailsList.Items.Remove(view);
			}

			this.ThumbnailsList.EndUpdate();
		}

		public void RefreshZoomSettings()
		{
			bool enableControls = this.EnableThumbnailZoom;
			this.ThumbnailZoomFactorNumericEdit.Enabled = enableControls;
			this.ZoomAnchorPanel.Enabled = enableControls;
		}

		public Action ApplicationExitRequested { get; set; }
		public Action<string> LoadNewSettings { get; set; }
		public Action SaveSettings { get; set; }

		public Action FormActivated { get; set; }

		public Action FormMinimized { get; set; }

		public Action<ViewCloseRequest> FormCloseRequested { get; set; }

		public Action ApplicationSettingsChanged { get; set; }

		public Action ThumbnailsSizeChanged { get; set; }

		public Action<string> ThumbnailStateChanged { get; set; }
		public Action<string> CropRegionSelectionRequested { get; set; }
		public Action<string> CropRegionResetRequested { get; set; }

		public Action<string> CropPresetSelected
		{
			get => this._cropPresetsPanel.PresetSelected;
			set => this._cropPresetsPanel.PresetSelected = value;
		}

		public Action<string> CropPresetCreateRequested
		{
			get => this._cropPresetsPanel.CreatePresetRequested;
			set => this._cropPresetsPanel.CreatePresetRequested = value;
		}

		public Action<string, string> CropPresetRenameRequested
		{
			get => this._cropPresetsPanel.RenamePresetRequested;
			set => this._cropPresetsPanel.RenamePresetRequested = value;
		}

		public Action<string> CropPresetDeleteRequested
		{
			get => this._cropPresetsPanel.DeletePresetRequested;
			set => this._cropPresetsPanel.DeletePresetRequested = value;
		}

		public Action<string, string> CropPresetSelectAreaRequested
		{
			get => this._cropPresetsPanel.SelectAreaRequested;
			set => this._cropPresetsPanel.SelectAreaRequested = value;
		}

		public Func<string, IList<string>, bool> CropAssignmentsApplyRequested
		{
			get => this._cropPresetsPanel.ApplyAssignmentsRequested;
			set => this._cropPresetsPanel.ApplyAssignmentsRequested = value;
		}

		public Action<int, bool> CropCycleGroupSelectRequested
		{
			get => this._cropPresetsPanel.SelectCycleGroupRequested;
			set => this._cropPresetsPanel.SelectCycleGroupRequested = value;
		}

		public Func<string, int, bool, bool, bool> CropCycleGroupAssignRequested
		{
			get => this._cropPresetsPanel.AssignCycleGroupRequested;
			set => this._cropPresetsPanel.AssignCycleGroupRequested = value;
		}

		public Action DocumentationLinkActivated { get; set; }
		public Action SelectedCycleGroupChanged { get; set; }

		#region UI events
		private void ContentTabControl_DrawItem(object sender, DrawItemEventArgs e)
		{
			TabControl control = (TabControl)sender;
			TabPage page = control.TabPages[e.Index];
			Rectangle bounds = control.GetTabRect(e.Index);

			Graphics graphics = e.Graphics;

			Brush textBrush = new SolidBrush(SystemColors.ActiveCaptionText);
			Brush backgroundBrush = (e.State == DrawItemState.Selected)
										? new SolidBrush(SystemColors.Control)
										: new SolidBrush(SystemColors.ControlDark);
			graphics.FillRectangle(backgroundBrush, e.Bounds);

			// Use our own font
			Font font = new Font("Arial", this.Font.Size * 1.5f, FontStyle.Bold, GraphicsUnit.Pixel);

			// Draw string and center the text
			StringFormat stringFlags = new StringFormat();
			stringFlags.Alignment = StringAlignment.Center;
			stringFlags.LineAlignment = StringAlignment.Center;

			graphics.DrawString(page.Text, font, textBrush, bounds, stringFlags);
		}

		private void OptionChanged_Handler(object sender, EventArgs e)
		{
			if (this._suppressEvents)
			{
				return;
			}

			this.ApplicationSettingsChanged?.Invoke();
		}

		public int SelectedCycleGroup
		{
			get => (this.CycleGroupSelectorComboBox?.SelectedIndex ?? 0) + 1;
			set
			{
				int idx = Math.Max(0, Math.Min(4, value - 1));
				if (this.CycleGroupSelectorComboBox != null)
				{
					this.CycleGroupSelectorComboBox.SelectedIndex = idx;
				}
			}
		}

		public string CycleGroupForwardHotkeysText
		{
			get => this.HotkeysForwardListBox != null ? string.Join(",", this.HotkeysForwardListBox.Items.Cast<object>().Select(i => i.ToString())) : string.Empty;
			set
			{
				if (this.HotkeysForwardListBox == null) return;
				this.HotkeysForwardListBox.Items.Clear();
				if (string.IsNullOrWhiteSpace(value)) return;
				foreach (var part in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()))
				{
					this.HotkeysForwardListBox.Items.Add(part);
				}
			}
		}

		public string CycleGroupBackwardHotkeysText
		{
			get => this.HotkeysBackwardListBox != null ? string.Join(",", this.HotkeysBackwardListBox.Items.Cast<object>().Select(i => i.ToString())) : string.Empty;
			set
			{
				if (this.HotkeysBackwardListBox == null) return;
				this.HotkeysBackwardListBox.Items.Clear();
				if (string.IsNullOrWhiteSpace(value)) return;
				foreach (var part in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()))
				{
					this.HotkeysBackwardListBox.Items.Add(part);
				}
			}
		}

		public void SetAvailableClients(IList<string> clients)
		{
			if (this.HotkeysClientsList == null) return;
			this.HotkeysClientsList.Items.Clear();
			foreach (var c in clients)
			{
				this.HotkeysClientsList.Items.Add(c, false);
			}
		}

		public IList<string> GetSelectedClientsForCurrentGroup()
		{
			if (this.HotkeysClientsList == null) return new List<string>();
			var ordered = new List<string>();
			for (int i = 0; i < this.HotkeysClientsList.Items.Count; i++)
			{
				if (this.HotkeysClientsList.GetItemChecked(i))
				{
					ordered.Add(this.HotkeysClientsList.Items[i].ToString());
				}
			}
			return ordered;
		}

		public void SetSelectedClientsForCurrentGroup(IList<string> orderedClients)
		{
			if (this.HotkeysClientsList == null) return;
			// Reorder items so orderedClients appear first in the given order, others follow
			var all = this.HotkeysClientsList.Items.Cast<object>().Select(o => o.ToString()).ToList();
			var newOrder = new List<string>();
			if (orderedClients != null)
			{
				foreach (var s in orderedClients)
				{
					if (all.Contains(s) && !newOrder.Contains(s)) newOrder.Add(s);
				}
			}
			foreach (var a in all)
			{
				if (!newOrder.Contains(a)) newOrder.Add(a);
			}
			this.HotkeysClientsList.Items.Clear();
			foreach (var it in newOrder)
			{
				this.HotkeysClientsList.Items.Add(it, orderedClients != null && orderedClients.Contains(it));
			}
		}

		private void CycleGroupSelectorComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Notify presenter that the selected group changed
			this.SelectedCycleGroupChanged?.Invoke();
		}

		private void HotkeysForwardAddButton_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(this.HotkeyCaptureTextBox?.Text)) return;
			var keyText = this.HotkeyCaptureTextBox.Text.Trim();
			if (this.ValidateAndMaybeWarnHotkey(keyText))
			{
				if (!this.HotkeysForwardListBox.Items.Contains(keyText))
				{
					this.HotkeysForwardListBox.Items.Add(keyText);
					this.ApplicationSettingsChanged?.Invoke();
				}
			}
		}

		private void HotkeysForwardRemoveButton_Click(object sender, EventArgs e)
		{
			if (this.HotkeysForwardListBox.SelectedIndex < 0) return;
			this.HotkeysForwardListBox.Items.RemoveAt(this.HotkeysForwardListBox.SelectedIndex);
			this.ApplicationSettingsChanged?.Invoke();
		}

		private void HotkeysBackwardAddButton_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(this.HotkeyCaptureTextBox?.Text)) return;
			var keyText = this.HotkeyCaptureTextBox.Text.Trim();
			if (this.ValidateAndMaybeWarnHotkey(keyText))
			{
				if (!this.HotkeysBackwardListBox.Items.Contains(keyText))
				{
					this.HotkeysBackwardListBox.Items.Add(keyText);
					this.ApplicationSettingsChanged?.Invoke();
				}
			}
		}

		private void HotkeysBackwardRemoveButton_Click(object sender, EventArgs e)
		{
			if (this.HotkeysBackwardListBox.SelectedIndex < 0) return;
			this.HotkeysBackwardListBox.Items.RemoveAt(this.HotkeysBackwardListBox.SelectedIndex);
			this.ApplicationSettingsChanged?.Invoke();
		}

		private void HotkeyCaptureTextBox_Enter(object sender, EventArgs e)
		{
			this.HotkeyCaptureTextBox.Text = LocalizationExtensions.GetString("MainForm.ContentTabControl.CycleGroupTabPage.HotkeyCaptureButton", "Capture");
			this.HotkeyCaptureTextBox.SelectAll();
		}

		private void HotkeyCaptureTextBox_Leave(object sender, EventArgs e)
		{
			// clear placeholder if left unchanged
			if (this.HotkeyCaptureTextBox.Text == LocalizationExtensions.GetString("MainForm.ContentTabControl.CycleGroupTabPage.HotkeyCaptureButton", "Capture")) this.HotkeyCaptureTextBox.Text = string.Empty;
		}

		private void HotkeysClientUpButton_Click(object sender, EventArgs e)
		{
			int idx = this.HotkeysClientsList.SelectedIndex;
			if (idx <= 0) return;
			var item = this.HotkeysClientsList.Items[idx];
			var checkedState = this.HotkeysClientsList.GetItemChecked(idx);
			this.HotkeysClientsList.Items.RemoveAt(idx);
			this.HotkeysClientsList.Items.Insert(idx - 1, item);
			this.HotkeysClientsList.SetItemChecked(idx - 1, checkedState);
			this.HotkeysClientsList.SelectedIndex = idx - 1;
			this.ApplicationSettingsChanged?.Invoke();
		}

		private void HotkeysClientDownButton_Click(object sender, EventArgs e)
		{
			int idx = this.HotkeysClientsList.SelectedIndex;
			if (idx < 0 || idx >= this.HotkeysClientsList.Items.Count - 1) return;
			var item = this.HotkeysClientsList.Items[idx];
			var checkedState = this.HotkeysClientsList.GetItemChecked(idx);
			this.HotkeysClientsList.Items.RemoveAt(idx);
			this.HotkeysClientsList.Items.Insert(idx + 1, item);
			this.HotkeysClientsList.SetItemChecked(idx + 1, checkedState);
			this.HotkeysClientsList.SelectedIndex = idx + 1;
			this.ApplicationSettingsChanged?.Invoke();
		}

		private void HotkeySaveButton_Click(object sender, EventArgs e)
		{
			this.ApplicationSettingsChanged?.Invoke();
		}

		/// <summary>
		/// Convert the hotkey string to Keys and attempt to register/unregister to verify its validity; if invalid, display a pop-up message.		/// </summary>
		/// <param name="keyText">For example, "Control+F14"</param>
		// <returns>Returns true if valid, otherwise false</returns>
		private bool ValidateAndMaybeWarnHotkey(string keyText)
		{
			Keys parsed = Keys.None;
			try
			{
				var conv = new KeysConverter();
				var obj = conv.ConvertFromInvariantString(keyText);
				if (obj is Keys k)
				{
					parsed = k;
				}
			}
			catch
			{
				parsed = Keys.None;
			}

			// Filtering invalid values ??and modifying only keys
			if (parsed == Keys.None || parsed == Keys.ControlKey || parsed == Keys.ShiftKey || parsed == Keys.Menu || parsed == Keys.ProcessKey)
			{
				MessageBox.Show(LocalizationExtensions.GetString("Messages.InvalidHotkey", "Invalid hotkey"), LocalizationExtensions.GetString("Messages.Error", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}

			// Attempt to register and verify usage
			EveOPreview.UI.Hotkeys.HotkeyHandler tester = null;
			try
			{
				tester = new EveOPreview.UI.Hotkeys.HotkeyHandler(default(IntPtr), parsed);
				if (!tester.CanRegister())
				{
					MessageBox.Show(LocalizationExtensions.GetString("Messages.HotkeyAlreadyInUse", "Hotkey Already in use"), LocalizationExtensions.GetString("Messages.Error", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return false;
				}
			}
			finally
			{
				tester?.Dispose();
			}

			return true;
		}


		private void ThumbnailSizeChanged_Handler(object sender, EventArgs e)
		{
			if (this._suppressEvents)
			{
				return;
			}

			// Perform some View work that is not properly done in the Control
			this._suppressEvents = true;
			Size thumbnailSize = this.ThumbnailSize;
			thumbnailSize.Width = Math.Min(Math.Max(thumbnailSize.Width, this._minimumSize.Width), this._maximumSize.Width);
			thumbnailSize.Height = Math.Min(Math.Max(thumbnailSize.Height, this._minimumSize.Height), this._maximumSize.Height);
			this.ThumbnailSize = thumbnailSize;
			this._suppressEvents = false;

			this.ThumbnailsSizeChanged?.Invoke();
		}

		private void ActiveClientHighlightColorButton_Click(object sender, EventArgs e)
		{
			using (ColorDialog dialog = new ColorDialog())
			{
				dialog.Color = this.ActiveClientHighlightColor;

				if (dialog.ShowDialog() != DialogResult.OK)
				{
					return;
				}

				this.ActiveClientHighlightColor = dialog.Color;
			}

			this.OptionChanged_Handler(sender, e);
		}

		private void OverlayLabelColorButton_Click(object sender, EventArgs e)
		{
			using (ColorDialog dialog = new ColorDialog())
			{
				dialog.Color = this.OverlayLabelColor;

				if (dialog.ShowDialog() != DialogResult.OK)
				{
					return;
				}
				this.OverlayLabelColor = dialog.Color;
			}

			this.OptionChanged_Handler(sender, e);
		}

		private void ThumbnailsList_ItemCheck_Handler(object sender, ItemCheckEventArgs e)
		{
			if (!(this.ThumbnailsList.Items[e.Index] is IThumbnailDescription selectedItem))
			{
				return;
			}

			selectedItem.IsDisabled = (e.NewValue == CheckState.Checked);

			this.ThumbnailStateChanged?.Invoke(selectedItem.Title);
		}

		private void InitializeCropControls()
		{
			Control clientsPanel = this.ThumbnailsList.Parent;
			foreach (Control oldLabel in clientsPanel.Controls.Find("ThumbnailsListLabel", false))
			{
				oldLabel.Visible = false;
			}

			Label cropHeader = new Label
			{
				AutoSize = true,
				Location = new Point(8, 6),
				Text = "Thumbnails (check to force hide)"
			};

			this._selectCropButton = new System.Windows.Forms.Button
			{
				Location = new Point(8, 30),
				Size = new Size(150, 32),
				Text = "Edit assigned crop...",
				UseVisualStyleBackColor = true
			};
			this._selectCropButton.Click += this.SelectCropButton_Click;

			this._resetCropButton = new System.Windows.Forms.Button
			{
				Location = new Point(166, 30),
				Size = new Size(140, 32),
				Text = "Use full window",
				UseVisualStyleBackColor = true
			};
			this._resetCropButton.Click += this.ResetCropButton_Click;

			Label cropHint = new Label
			{
				AutoSize = true,
				Location = new Point(8, 66),
				Text = "Select a client below, or manage reusable crops in the Crops tab."
			};

			clientsPanel.Controls.Add(cropHeader);
			clientsPanel.Controls.Add(this._selectCropButton);
			clientsPanel.Controls.Add(this._resetCropButton);
			clientsPanel.Controls.Add(cropHint);

			this.ThumbnailsList.Dock = DockStyle.None;
			this.ThumbnailsList.Anchor = AnchorStyles.None;
			void LayoutClientList()
			{
				const int listTop = 90;
				this.ThumbnailsList.SetBounds(
					0,
					listTop,
					clientsPanel.ClientSize.Width,
					Math.Max(50, clientsPanel.ClientSize.Height - listTop));
			}

			clientsPanel.Resize += (sender, args) => LayoutClientList();
			LayoutClientList();
		}

		private void InitializeCropPresetsTab()
		{
			this._cropPresetsPanel = new CropPresetsPanel();
			TabControl contentTabControl = this.Controls
				.OfType<TabControl>()
				.FirstOrDefault(control => control.Name == "ContentTabControl");
			TabPage clientsTabPage = this.ThumbnailsList.Parent?.Parent as TabPage;
			if (contentTabControl == null || clientsTabPage == null)
			{
				return;
			}

			TabPage cropsTab = new TabPage
			{
				Name = "CropsTabPage",
				Text = "Crops",
				UseVisualStyleBackColor = true,
				Padding = new Padding(0)
			};
			cropsTab.Controls.Add(this._cropPresetsPanel);
			int clientsIndex = contentTabControl.TabPages.IndexOf(clientsTabPage);
			contentTabControl.TabPages.Insert(Math.Max(0, clientsIndex), cropsTab);
		}

		private void InitializeCycleGroupCropControls()
		{
			FlowLayoutPanel cropPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Bottom,
				Height = 38,
				WrapContents = false,
				Padding = new Padding(3, 2, 3, 2)
			};
			cropPanel.Controls.Add(new Label
			{
				Text = "Crop",
				AutoSize = true,
				Margin = new Padding(0, 7, 5, 0)
			});
			this._cycleCropPresetCombo = new System.Windows.Forms.ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Width = 135,
				Margin = new Padding(0, 2, 5, 2)
			};
			this._cycleCropPresetCombo.SelectedIndexChanged += (sender, args) =>
				this._cycleCropApplyButton.Enabled = this._cycleCropPresetCombo.SelectedItem is CropPresetComboItem;
			this._cycleCropApplyButton = new System.Windows.Forms.Button
			{
				Text = "Apply crop",
				AutoSize = true,
				Height = 30,
				Enabled = false,
				Margin = new Padding(0, 1, 0, 1)
			};
			this._cycleCropApplyButton.Click += this.CycleCropApplyButton_Click;
			cropPanel.Controls.Add(this._cycleCropPresetCombo);
			cropPanel.Controls.Add(this._cycleCropApplyButton);
			this.CycleGroupTabPage.Controls.Add(cropPanel);
			cropPanel.BringToFront();
		}

		private void InitializeSolarSystemOverlayControl()
		{
			this._showSolarSystemOverlayCheckBox = new System.Windows.Forms.CheckBox
			{
				Name = "ShowSolarSystemOverlayCheckBox",
				Text = "Show solar system from game logs",
				AutoSize = true,
				Checked = true,
				Location = new Point(13, 285),
				TabIndex = 48,
				UseVisualStyleBackColor = true
			};
			this._showSolarSystemOverlayCheckBox.CheckedChanged += this.OptionChanged_Handler;
			this.ShowThumbnailOverlaysCheckBox.Parent?.Controls.Add(this._showSolarSystemOverlayCheckBox);
			this._showSolarSystemOverlayCheckBox.BringToFront();
		}

		public string SelectedCropPresetId => this._cropPresetsPanel.SelectedPresetId;
		public bool HasPendingCropAssignments => this._cropPresetsPanel.HasPendingAssignments;

		public void SetCropPresets(IList<CropPreset> presets, string selectedPresetId)
		{
			this._cropPresetsPanel.SetPresets(presets, selectedPresetId);
			this.SetCycleGroupCropPresets(presets, selectedPresetId);
		}

		private void SetCycleGroupCropPresets(IList<CropPreset> presets, string selectedPresetId)
		{
			string previousId = (this._cycleCropPresetCombo.SelectedItem as CropPresetComboItem)?.Id ?? selectedPresetId;
			this._cycleCropPresetCombo.Items.Clear();
			foreach (CropPreset preset in (presets ?? new List<CropPreset>())
				.Where(preset => preset?.IsValid == true)
				.OrderBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase))
			{
				this._cycleCropPresetCombo.Items.Add(new CropPresetComboItem { Id = preset.Id, Name = preset.Name });
			}

			int selectedIndex = -1;
			for (int index = 0; index < this._cycleCropPresetCombo.Items.Count; index++)
			{
				if (string.Equals((this._cycleCropPresetCombo.Items[index] as CropPresetComboItem)?.Id, previousId, StringComparison.Ordinal))
				{
					selectedIndex = index;
					break;
				}
			}
			this._cycleCropPresetCombo.SelectedIndex = selectedIndex >= 0
				? selectedIndex
				: (this._cycleCropPresetCombo.Items.Count > 0 ? 0 : -1);
			this._cycleCropApplyButton.Enabled = this._cycleCropPresetCombo.SelectedItem is CropPresetComboItem;
		}

		private void CycleCropApplyButton_Click(object sender, EventArgs e)
		{
			if (this._cycleCropPresetCombo.SelectedItem is not CropPresetComboItem preset ||
				!this._cropPresetsPanel.ResolvePendingAssignments())
			{
				return;
			}

			this.CropCycleGroupAssignRequested?.Invoke(preset.Id, this.SelectedCycleGroup, false, false);
		}

		public void SetCropPresetRegion(CropRegion region)
		{
			this._cropPresetsPanel.SetRegion(region);
		}

		public void SetCropSourceClients(IList<string> clients)
		{
			this._cropPresetsPanel.SetSourceClients(clients);
		}

		public void SetCropClients(
			IList<string> clients,
			IDictionary<string, string> currentPresetNames,
			ISet<string> openClients,
			ISet<string> checkedClients)
		{
			this._cropPresetsPanel.SetClients(clients, currentPresetNames, openClients, checkedClients);
		}

		public void SelectCropClients(IList<string> clients, bool onlyOpen)
		{
			this._cropPresetsPanel.SelectClients(clients, onlyOpen);
		}

		private void SelectCropButton_Click(object sender, EventArgs e)
		{
			if (!this.TryGetSelectedClientTitle(out string title))
			{
				return;
			}

			this.CropRegionSelectionRequested?.Invoke(title);
		}

		private void ResetCropButton_Click(object sender, EventArgs e)
		{
			if (!this.TryGetSelectedClientTitle(out string title))
			{
				return;
			}

			this.CropRegionResetRequested?.Invoke(title);
		}

		private bool TryGetSelectedClientTitle(out string title)
		{
			if (this.ThumbnailsList.SelectedItem is IThumbnailDescription selectedItem)
			{
				title = selectedItem.Title;
				return true;
			}

			title = null;
			MessageBox.Show(
				this,
				"Select a client in the list first.",
				"No client selected",
				MessageBoxButtons.OK,
				MessageBoxIcon.Information);
			return false;
		}

		private void DocumentationLinkClicked_Handler(object sender, LinkLabelLinkClickedEventArgs e)
		{
			this.DocumentationLinkActivated?.Invoke();
		}

		private void MainFormResize_Handler(object sender, EventArgs e)
		{
			if (this.WindowState != FormWindowState.Minimized)
			{
				return;
			}

			this.FormMinimized?.Invoke();
		}

		private void MainFormClosing_Handler(object sender, FormClosingEventArgs e)
		{
			ViewCloseRequest request = new ViewCloseRequest();

			this.FormCloseRequested?.Invoke(request);

			e.Cancel = !request.Allow;
		}

		private void RestoreMainForm_Handler(object sender, EventArgs e)
		{
			// This is form's GUI lifecycle event that is invariant to the Form data
			base.Show();
			this.WindowState = FormWindowState.Normal;
			this.BringToFront();
		}

		private void ExitMenuItemClick_Handler(object sender, EventArgs e)
		{
			this.ApplicationExitRequested?.Invoke();
		}
		#endregion

		private void InitZoomAnchorMap()
		{
			this._zoomAnchorMap[ViewZoomAnchor.NW] = this.ZoomAanchorNWRadioButton;
			this._zoomAnchorMap[ViewZoomAnchor.N] = this.ZoomAanchorNRadioButton;
			this._zoomAnchorMap[ViewZoomAnchor.NE] = this.ZoomAanchorNERadioButton;
			this._zoomAnchorMap[ViewZoomAnchor.W] = this.ZoomAanchorWRadioButton;
			this._zoomAnchorMap[ViewZoomAnchor.C] = this.ZoomAanchorCRadioButton;
			this._zoomAnchorMap[ViewZoomAnchor.E] = this.ZoomAanchorERadioButton;
			this._zoomAnchorMap[ViewZoomAnchor.SW] = this.ZoomAanchorSWRadioButton;
			this._zoomAnchorMap[ViewZoomAnchor.S] = this.ZoomAanchorSRadioButton;
			this._zoomAnchorMap[ViewZoomAnchor.SE] = this.ZoomAanchorSERadioButton;
		}
		private void InitOverlayLabelMap()
		{
			this._overlayLabelMap[ViewZoomAnchor.NW] = this.OverlayLabelNWRadioButton;
			this._overlayLabelMap[ViewZoomAnchor.N] = this.OverlayLabelNRadioButton;
			this._overlayLabelMap[ViewZoomAnchor.NE] = this.OverlayLabelNERadioButton;
			this._overlayLabelMap[ViewZoomAnchor.W] = this.OverlayLabelWRadioButton;
			this._overlayLabelMap[ViewZoomAnchor.C] = this.OverlayLabelCRadioButton;
			this._overlayLabelMap[ViewZoomAnchor.E] = this.OverlayLabelERadioButton;
			this._overlayLabelMap[ViewZoomAnchor.SW] = this.OverlayLabelSWRadioButton;
			this._overlayLabelMap[ViewZoomAnchor.S] = this.OverlayLabelSRadioButton;
			this._overlayLabelMap[ViewZoomAnchor.SE] = this.OverlayLabelSERadioButton;
		}
		private void InitCycleGroupIndicatorMap()
		{
			this._cycleGroupIndicatorMap[ViewZoomAnchor.NW] = this.CycleGroupIndicatorNWRadioButton;
			this._cycleGroupIndicatorMap[ViewZoomAnchor.N] = this.CycleGroupIndicatorNRadioButton;
			this._cycleGroupIndicatorMap[ViewZoomAnchor.NE] = this.CycleGroupIndicatorNERadioButton;
			this._cycleGroupIndicatorMap[ViewZoomAnchor.W] = this.CycleGroupIndicatorWRadioButton;
			this._cycleGroupIndicatorMap[ViewZoomAnchor.C] = this.CycleGroupIndicatorCRadioButton;
			this._cycleGroupIndicatorMap[ViewZoomAnchor.E] = this.CycleGroupIndicatorERadioButton;
			this._cycleGroupIndicatorMap[ViewZoomAnchor.SW] = this.CycleGroupIndicatorSWRadioButton;
			this._cycleGroupIndicatorMap[ViewZoomAnchor.S] = this.CycleGroupIndicatorSRadioButton;
			this._cycleGroupIndicatorMap[ViewZoomAnchor.SE] = this.CycleGroupIndicatorSERadioButton;
		}

		private void InitFormSize()
		{
			const int BUFFER_PIXEL_AMOUNT = 8;
			// resize form height based on tabbed control item height
			var tabControl = (System.Windows.Forms.TabControl)this.Controls.Find("ContentTabControl", false).First();
			if (tabControl != null)
			{
				var furnitureSize = this.Height - tabControl.Height;
				var calculatedHeight = (tabControl.ItemSize.Width * tabControl.Controls.Count) + furnitureSize + BUFFER_PIXEL_AMOUNT;
				if (this.Height < calculatedHeight)
				{
					this.Height = calculatedHeight;
				}
			}
		}

		private void btnLabelFont_Click(object sender, EventArgs e)
		{
			FontDialog fontSelector = new FontDialog();
			fontSelector.Font = OverlayLabelFont;
			fontSelector.ShowColor = false;
			fontSelector.ShowApply = false;
			fontSelector.ShowHelp = false;
			if (fontSelector.ShowDialog() != DialogResult.Cancel)
			{
				OverlayLabelFont = fontSelector.Font;
				LabelOverlayLabelFont.Font = fontSelector.Font;
				this.OptionChanged_Handler(sender, e);
			}
		}

		private void PreventPreviewColorButton_Click(object sender, EventArgs e)
		{
			using (ColorDialog dialog = new ColorDialog())
			{
				dialog.Color = this.PreventPreviewColor;

				if (dialog.ShowDialog() != DialogResult.OK)
				{
					return;
				}

				this.PreventPreviewColor = dialog.Color;
			}

			this.OptionChanged_Handler(sender, e);

		}

		public void InitializeLanguageControls()
		{
			if (LanguageCombo.Items.Count == 0)
			{
				foreach (var l in LocalizationExtensions.GetLanguages())
				{
					LanguageCombo.Items.Add(l);
				}
			}

			LocalizationExtensions.ApplyLocalization(this);
			this.NotifyIcon.Text = LocalizationExtensions.GetString($"{this.Name}.NotifyIcon", this.NotifyIcon.Text);
			foreach (var v in this.TrayMenu.Items)
			{
				try
				{
					ToolStripMenuItem f = (ToolStripMenuItem)v;
					f.Text = LocalizationExtensions.GetString($"{this.Name}.{f.Name}", f.Text);
				}
				catch
				{
				}
			}
		}

		private void GeneralSettingsPanel_Paint(object sender, PaintEventArgs e)
		{

		}

		private void LanguageCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (this._suppressEvents)
			{
				return;
			}
			this.ApplicationSettingsChanged?.Invoke();
			LocalizationExtensions.SetLanguage(Language);
			InitializeLanguageControls();
		}

		private void LanguageTabPage_Click(object sender, EventArgs e)
		{

		}

		private void HotkeyCaptureButton_Click(object sender, EventArgs e)
		{
			this._hotkeyCaptureActive = true;
			if (this.HotkeyCaptureButton != null)
			{
				// if you rename the object - adjust this string
				this.HotkeyCaptureButton.Text = LocalizationExtensions.GetString("MainForm.ContentTabControl.CycleGroupTabPage.HotkeyCaptureButton_PressKey", "Press key...");
			}
			if (this.HotkeyCaptureTextBox != null)
			{
				this.HotkeyCaptureTextBox.Text = string.Empty;
				this.HotkeyCaptureTextBox.Focus();
			}
		}

		private void HotkeyCaptureTextBox_MouseDown(object sender, MouseEventArgs e)
		{
			this.HotkeyCaptureTextBox.Focus();
		}

		private void HotkeyCaptureTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (!this._hotkeyCaptureActive)
			{
				// not currently capturing
				return;
			}
			e.SuppressKeyPress = true;
			e.Handled = true;

			// Filter only modifier keys and unrecognized ones. ProcessKey
			var baseKey = e.KeyCode;
			if (baseKey == Keys.ControlKey || baseKey == Keys.ShiftKey || baseKey == Keys.Menu || baseKey == Keys.ProcessKey)
			{
				return;
			}

			// Uses Microsoft's official KeysConverter to output canonical strings (invariant regions), compatible with Keys Enum.
			var combined = e.KeyData; // Includes modifier keys

			string keyText = new KeysConverter().ConvertToInvariantString(combined);
			if (string.IsNullOrWhiteSpace(keyText))
			{
				return;
			}
			this.HotkeyCaptureTextBox.Text = keyText;

			// Done
			this._hotkeyCaptureActive = false;
			if (this.HotkeyCaptureButton != null)
			{
				// if you rename the object - adjust this string
				this.HotkeyCaptureButton.Text = LocalizationExtensions.GetString("MainForm.ContentTabControl.CycleGroupTabPage.HotkeyCaptureButton", "Capture");
			}
		}

		public void BeginUpdateUI()
		{
			this._suppressEvents = true;
		}

		public void EndUpdateUI()
		{
			this._suppressEvents = false;
		}

		public void SetupConfigList()
		{

			this.MenuConfigurationFile.DropDownItems.Clear();
			this.MenuConfigurationFile.DropDownItems.Add(LocalizationExtensions.GetString("MainForm.MenuConfigurationFile.Reload", "Reload Configuration"));
			this.MenuConfigurationFile.DropDownItems.Add(
				new ToolStripSeparator()
				{
				}
				);
			
			foreach (var filename in Directory.GetFiles(".", "Eve-O-Preview*.json"))
			{
				string displayName = filename.Replace("./", "", StringComparison.OrdinalIgnoreCase);

				displayName = displayName.Replace(".//", "", StringComparison.OrdinalIgnoreCase);
				displayName = displayName.Replace(".\\", "", StringComparison.OrdinalIgnoreCase);

				if (displayName.Equals(ConfigurationStorage.CONFIGURATION_FILE_NAME, StringComparison.OrdinalIgnoreCase))
				{
					displayName = LocalizationExtensions.GetString("MainForm.MenuConfigurationFile.DEFALT", "*DEFAULT*");
				}
				else
				{
					if (! displayName.StartsWith("Eve-O-Preview-", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					displayName = displayName.Replace("Eve-O-Preview-", "", StringComparison.OrdinalIgnoreCase);
					displayName = displayName.Replace(".json", "", StringComparison.OrdinalIgnoreCase);
				}

				var mi = new ToolStripMenuItem()
				{
					Text = displayName,
					Checked = (displayName == LocalizationExtensions.GetString("MainForm.MenuConfigurationFile.DEFALT", "*DEFAULT*") ? true : false),
				};

				this.MenuConfigurationFile.DropDownItems.Add(mi);
				_configurationFilenames.Add(displayName, filename.Replace(".//","").Replace("./",""));
			}

		}

		private void MenuConfigurationFile_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			if ( e.ClickedItem.Text.Equals(LocalizationExtensions.GetString("MainForm.MenuConfigurationFile.Reload", "Reload Configuration")))
			{
				this.LoadNewSettings?.Invoke(null);
				return;
			}

			if (_configurationFilenames.ContainsKey(e.ClickedItem.Text))
			{
				var _configurationFilename = _configurationFilenames[e.ClickedItem.Text];

				foreach (var mi in this.MenuConfigurationFile.DropDownItems)
				{

					if ( mi.GetType()  == typeof(ToolStripMenuItem) )
					{
						ToolStripMenuItem menuItem = (ToolStripMenuItem)mi;
						if (menuItem.Text.Length > 0 && menuItem.Text != LocalizationExtensions.GetString("MainForm.MenuConfigurationFile.Reload", "Reload Configuration"))
						{
							menuItem.Checked = (menuItem.Text == e.ClickedItem.Text) ? true : false;
						}
					}
				}
				this.SaveSettings?.Invoke();
				this.LoadNewSettings?.Invoke(_configurationFilename);
			}
		}
	}
}
