using EveOPreview.Configuration;
using EveOPreview.Configuration.Implementation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
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

		private sealed class LanguageOption
		{
			public string Code { get; init; }
			public string DisplayName { get; init; }
			public override string ToString() => this.DisplayName;
		}

		#region Private fields
		private readonly ApplicationContext _context;
		private readonly Dictionary<ViewZoomAnchor, RadioButton> _zoomAnchorMap;
		private ViewZoomAnchor _cachedThumbnailZoomAnchor;
		private bool _suppressEvents;
		private Size _minimumSize;
		private Size _maximumSize;
		private string _iconName;
		private bool _hotkeyCaptureActive = false;
		private Dictionary<string, string> _configurationFilenames = new Dictionary<string, string>();
		private CropPresetsPanel _cropPresetsPanel;
		private CycleGroupsPanel _cycleGroupsPanel;
		private System.Windows.Forms.ComboBox _cycleCropPresetCombo;
		private System.Windows.Forms.Button _cycleCropApplyButton;
		private System.Windows.Forms.Button _cycleCropClearButton;
		private OverlaySettingsPanel _overlaySettingsPanel;
		#endregion

		public MainForm(ApplicationContext context)
		{
			this._context = context;
			this._zoomAnchorMap = new Dictionary<ViewZoomAnchor, RadioButton>();
			this._cachedThumbnailZoomAnchor = ViewZoomAnchor.NW;
			this._suppressEvents = false;
			this._minimumSize = new Size(20, 20);
			this._maximumSize = new Size(20, 20);

			InitializeComponent();
			this.InitializeCropPresetsTab();
			this.InitializeCycleGroupsPanel();
			this.InitializeOverlaySettingsPanel();

			this.ThumbnailsList.DisplayMember = "Title";

			SetupConfigList();

			this.InitZoomAnchorMap();
			this.InitFormSize();
			// WinForms performs one more font/DPI scaling pass when the handle is
			// shown. Re-assert the management size after that pass as well.
			this.Shown += (sender, args) => this.EnsureManagementMinimumSize();

			this.AnimationStyleCombo.Format += this.EnumCombo_Format;
			this.CaptionOnClientsStyleCombo.Format += this.EnumCombo_Format;
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
			get => (this.LanguageCombo.SelectedItem as LanguageOption)?.Code ?? "en-US";
			set
			{
				LanguageOption option = this.LanguageCombo.Items
					.OfType<LanguageOption>()
					.FirstOrDefault(item => string.Equals(item.Code, value, StringComparison.OrdinalIgnoreCase));
				if (option != null)
				{
					this.LanguageCombo.SelectedItem = option;
				}
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
			get => this._overlaySettingsPanel.CharacterLabelAnchor;
			set => this._overlaySettingsPanel.CharacterLabelAnchor = value;
		}

		public ViewZoomAnchor SolarSystemLabelAnchor
		{
			get => this._overlaySettingsPanel.SolarSystemLabelAnchor;
			set => this._overlaySettingsPanel.SolarSystemLabelAnchor = value;
		}

		public ViewZoomAnchor CycleGroupIndicatorAnchor
		{
			get => this._overlaySettingsPanel.CycleGroupIndicatorAnchor;
			set => this._overlaySettingsPanel.CycleGroupIndicatorAnchor = value;
		}

		public bool ShowThumbnailOverlays
		{
			get => this._overlaySettingsPanel.ShowThumbnailOverlays;
			set => this._overlaySettingsPanel.ShowThumbnailOverlays = value;
		}

		public bool ShowSolarSystemOverlay
		{
			get => this._overlaySettingsPanel.ShowSolarSystemOverlay;
			set => this._overlaySettingsPanel.ShowSolarSystemOverlay = value;
		}

		public bool ShowThumbnailFrames
		{
			get => this._overlaySettingsPanel.ShowThumbnailFrames;
			set => this._overlaySettingsPanel.ShowThumbnailFrames = value;
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
			get => this._overlaySettingsPanel.EnableActiveClientHighlight;
			set => this._overlaySettingsPanel.EnableActiveClientHighlight = value;
		}

		public Color ActiveClientHighlightColor
		{
			get => this._overlaySettingsPanel.ActiveClientHighlightColor;
			set => this._overlaySettingsPanel.ActiveClientHighlightColor = value;
		}

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
			get => this._overlaySettingsPanel.CharacterLabelColor;
			set => this._overlaySettingsPanel.CharacterLabelColor = value;
		}

		public Font OverlayLabelFont
		{
			get => this._overlaySettingsPanel.CharacterLabelFont;
			set => this._overlaySettingsPanel.CharacterLabelFont = value;
		}

		public Color SolarSystemLabelColor
		{
			get => this._overlaySettingsPanel.SolarSystemLabelColor;
			set => this._overlaySettingsPanel.SolarSystemLabelColor = value;
		}

		public Font SolarSystemLabelFont
		{
			get => this._overlaySettingsPanel.SolarSystemLabelFont;
			set => this._overlaySettingsPanel.SolarSystemLabelFont = value;
		}

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
				this._cycleGroupsPanel?.SetClientOpen(view.Title, true);
			}

			this.ThumbnailsList.EndUpdate();
		}

		public void RemoveThumbnails(IList<IThumbnailDescription> thumbnails)
		{
			this.ThumbnailsList.BeginUpdate();

			foreach (IThumbnailDescription view in thumbnails)
			{
				this.ThumbnailsList.Items.Remove(view);
				this._cycleGroupsPanel?.SetClientOpen(view.Title, false);
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
		public Action FormActivated { get; set; }

		public Action FormMinimized { get; set; }

		public Action<ViewCloseRequest> FormCloseRequested { get; set; }

		public Action ApplicationSettingsChanged { get; set; }

		public Action ThumbnailsSizeChanged { get; set; }

		public Action<string> ThumbnailStateChanged { get; set; }
		public Action<string> CropPresetSelected
		{
			get => this._cropPresetsPanel.PresetSelected;
			set => this._cropPresetsPanel.PresetSelected = value;
		}

		public Action<string, string> CropPresetCreateRequested
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

		public Func<int, bool> CropCycleGroupClearRequested { get; set; }
		public Func<string, int, bool> CropTemporaryCycleGroupAssignRequested { get; set; }

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
			get => this._cycleGroupsPanel?.SelectedGroup ?? 1;
			set
			{
				if (this._cycleGroupsPanel != null) this._cycleGroupsPanel.SelectedGroup = value;
			}
		}

		public bool SelectedCycleGroupIsTemporary
		{
			get => this._cycleGroupsPanel?.IsTemporary == true;
			set { if (this._cycleGroupsPanel != null) this._cycleGroupsPanel.IsTemporary = value; }
		}

		public string CycleGroupForwardHotkeysText
		{
			get => this._cycleGroupsPanel?.ForwardHotkeysText ?? string.Empty;
			set { if (this._cycleGroupsPanel != null) this._cycleGroupsPanel.ForwardHotkeysText = value; }
		}

		public string CycleGroupBackwardHotkeysText
		{
			get => this._cycleGroupsPanel?.BackwardHotkeysText ?? string.Empty;
			set { if (this._cycleGroupsPanel != null) this._cycleGroupsPanel.BackwardHotkeysText = value; }
		}

		public void SetAvailableClients(IList<string> clients)
		{
			this._cycleGroupsPanel?.SetAvailableClients(clients);
		}

		public IList<string> GetSelectedClientsForCurrentGroup()
		{
			return this._cycleGroupsPanel?.GetOrderedMembers() ?? new List<string>();
		}

		public void SetSelectedClientsForCurrentGroup(IList<string> orderedClients)
		{
			this._cycleGroupsPanel?.SetOrderedMembers(orderedClients);
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

		private void InitializeCycleGroupsPanel()
		{
			this._cycleGroupsPanel = new CycleGroupsPanel();
			this._cycleGroupsPanel.SelectionChanged = () => this.SelectedCycleGroupChanged?.Invoke();
			this._cycleGroupsPanel.ContentsChanged = () => this.ApplicationSettingsChanged?.Invoke();
			this._cycleGroupsPanel.ValidateHotkey = this.ValidateAndMaybeWarnHotkey;
			this._cycleGroupsPanel.ApplyCropRequested = (presetId, group, temporary) =>
			{
				if (!this._cropPresetsPanel.ResolvePendingAssignments())
				{
					return false;
				}
				return temporary
					? this.CropTemporaryCycleGroupAssignRequested?.Invoke(presetId, group) == true
					: this.CropCycleGroupAssignRequested?.Invoke(presetId, group, false, false) == true;
			};
			this.CycleGroupTabPage.Controls.Clear();
			this.CycleGroupTabPage.Controls.Add(this._cycleGroupsPanel);
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
				Width = 105,
				Margin = new Padding(0, 2, 5, 2)
			};
			this._cycleCropPresetCombo.SelectedIndexChanged += (sender, args) =>
				this._cycleCropApplyButton.Enabled = this._cycleCropPresetCombo.SelectedItem is CropPresetComboItem;
			this._cycleCropApplyButton = new System.Windows.Forms.Button
			{
				Text = "Apply group",
				AutoSize = true,
				Height = 30,
				Enabled = false,
				Margin = new Padding(0, 1, 0, 1)
			};
			this._cycleCropApplyButton.Click += this.CycleCropApplyButton_Click;
			this._cycleCropClearButton = new System.Windows.Forms.Button
			{
				Text = "Clear group",
				AutoSize = true,
				Height = 30,
				Margin = new Padding(5, 1, 0, 1)
			};
			this._cycleCropClearButton.Click += this.CycleCropClearButton_Click;
			cropPanel.Controls.Add(this._cycleCropPresetCombo);
			cropPanel.Controls.Add(this._cycleCropApplyButton);
			cropPanel.Controls.Add(this._cycleCropClearButton);
			this.CycleGroupTabPage.Controls.Add(cropPanel);
			cropPanel.BringToFront();
		}

		private void InitializeOverlaySettingsPanel()
		{
			this._overlaySettingsPanel = new OverlaySettingsPanel
			{
				ContentsChanged = () => this.OptionChanged_Handler(this, EventArgs.Empty)
			};
			TabPage overlayTabPage = this.ShowThumbnailOverlaysCheckBox.Parent?.Parent as TabPage;
			if (overlayTabPage == null)
			{
				return;
			}
			overlayTabPage.Controls.Clear();
			overlayTabPage.Controls.Add(this._overlaySettingsPanel);
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
			this._cycleGroupsPanel?.SetCropPresets(presets, selectedPresetId);
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

		private void CycleCropClearButton_Click(object sender, EventArgs e)
		{
			if (!this._cropPresetsPanel.ResolvePendingAssignments())
			{
				return;
			}

			this.CropCycleGroupClearRequested?.Invoke(this.SelectedCycleGroup);
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
		private void InitFormSize()
		{
			const int BUFFER_PIXEL_AMOUNT = 8;
			// Runtime-created management panels are not included in the designer's
			// font autoscaling pass. Keep enough real client area for their controls
			// and for a useful multi-character list.
			this.EnsureManagementMinimumSize();
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

		private void EnsureManagementMinimumSize()
		{
			Size managementMinimum = new Size(720, 560);
			this.MinimumSize = managementMinimum;
			this.Width = Math.Max(this.Width, managementMinimum.Width);
			this.Height = Math.Max(this.Height, managementMinimum.Height);
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
				foreach (string code in LocalizationExtensions.GetLanguages())
				{
					string displayName;
					try
					{
						CultureInfo culture = CultureInfo.GetCultureInfo(code);
						displayName = culture.TextInfo.ToTitleCase(culture.NativeName);
					}
					catch (CultureNotFoundException)
					{
						displayName = code;
					}
					LanguageCombo.Items.Add(new LanguageOption
					{
						Code = code,
						DisplayName = $"{displayName} ({code})"
					});
				}
			}

			LocalizationExtensions.ApplyLocalization(this);
			this._cycleGroupsPanel?.ApplyLocalization();
			this._cropPresetsPanel?.ApplyLocalization();
			this._overlaySettingsPanel?.ApplyLocalization();
			this.RefreshLocalizedEnumCombo(this.AnimationStyleCombo, typeof(AnimationStyle));
			this.RefreshLocalizedEnumCombo(this.CaptionOnClientsStyleCombo, typeof(CaptionBarStyle));
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

		private void EnumCombo_Format(object sender, ListControlConvertEventArgs e)
		{
			if (e.ListItem is Enum value)
			{
				e.Value = LocalizationExtensions.GetString(
					$"Enums.{value.GetType().Name}.{value}",
					value.ToString());
			}
		}

		private void RefreshLocalizedEnumCombo(System.Windows.Forms.ComboBox combo, Type enumType)
		{
			object selectedValue = combo.SelectedItem;
			bool wasSuppressingEvents = this._suppressEvents;
			this._suppressEvents = true;
			try
			{
				// WinForms caches the value produced by the Format event. Rebinding is
				// required to discard strings from the previously selected language.
				combo.DataSource = null;
				combo.DataSource = Enum.GetValues(enumType);
				if (selectedValue != null)
				{
					combo.SelectedItem = selectedValue;
				}
				combo.Refresh();
			}
			finally
			{
				this._suppressEvents = wasSuppressingEvents;
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
			this._configurationFilenames.Clear();
			this.MenuConfigurationFile.DropDownItems.Clear();
			this.MenuConfigurationFile.DropDownItems.Add(LocalizationExtensions.GetString("MainForm.MenuConfigurationFile.Reload", "Reload Configuration"));
			this.MenuConfigurationFile.DropDownItems.Add(
				new ToolStripSeparator()
				{
				}
				);
			
			foreach (string filename in Directory.GetFiles(".", "Eve-O-Preview*.json")
				.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
			{
				string fileNameOnly = Path.GetFileName(filename);
				if (fileNameOnly.EndsWith(".last-good.json", StringComparison.OrdinalIgnoreCase) ||
					fileNameOnly.IndexOf(".before-", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					continue;
				}

				string displayName = fileNameOnly;

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
					displayName = Path.GetFileNameWithoutExtension(displayName).Substring("Eve-O-Preview-".Length);
				}

				var mi = new ToolStripMenuItem()
				{
					Text = displayName,
					Checked = (displayName == LocalizationExtensions.GetString("MainForm.MenuConfigurationFile.DEFALT", "*DEFAULT*") ? true : false),
				};

				this.MenuConfigurationFile.DropDownItems.Add(mi);
				_configurationFilenames.Add(displayName, fileNameOnly);
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
				this.LoadNewSettings?.Invoke(_configurationFilename);
			}
		}
	}
}
