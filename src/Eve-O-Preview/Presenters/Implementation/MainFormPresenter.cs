using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using EveOPreview.Configuration;
using EveOPreview.Configuration.Implementation;
using EveOPreview.Mediator.Messages;
using EveOPreview.Services;
using EveOPreview.View;
using MediatR;
using System.Windows.Forms;

namespace EveOPreview.Presenters
{
	public class MainFormPresenter : Presenter<IMainFormView>, IMainFormPresenter
	{
		#region Private constants
		private const string FORUM_URL = @"https://forums.eveonline.com/t/eve-o-preview-v8-0-2-0";
		#endregion

		#region Private fields
		private readonly IMediator _mediator;
		private readonly IThumbnailConfiguration _configuration;
		private readonly IConfigurationStorage _configurationStorage;
		private readonly IThumbnailManager _thumbnailManager;
		private readonly ICharacterLocationTracker _characterLocationTracker;
		private readonly IDictionary<string, IThumbnailDescription> _descriptionsCache;
		private bool _suppressSizeNotifications;

		private bool _exitApplication;
		private bool _isLoadingUi;
		private bool _configurationSwitchInProgress;
		private readonly SemaphoreSlim _saveSettingsLock = new SemaphoreSlim(1, 1);
		private int _currentGroup = 1;
		private bool _currentGroupIsTemporary;
		#endregion

		public MainFormPresenter(
			IApplicationController controller,
			IMainFormView view,
			IMediator mediator,
			IThumbnailConfiguration configuration,
			IConfigurationStorage configurationStorage,
			IThumbnailManager thumbnailManager,
			ICharacterLocationTracker characterLocationTracker)
			: base(controller, view)
		{
			this._mediator = mediator;
			this._configuration = configuration;
			this._configurationStorage = configurationStorage;
			this._thumbnailManager = thumbnailManager;
			this._characterLocationTracker = characterLocationTracker;

			this._descriptionsCache = new Dictionary<string, IThumbnailDescription>();

			this._suppressSizeNotifications = false;
			this._exitApplication = false;

			this.View.FormActivated = this.Activate;
			this.View.FormMinimized = this.Minimize;
			this.View.FormCloseRequested = this.Close;
			this.View.ApplicationSettingsChanged = this.SaveApplicationSettings;
			this.View.ThumbnailsSizeChanged = this.UpdateThumbnailsSize;
			this.View.ThumbnailStateChanged = this.UpdateThumbnailState;
			this.View.CropPresetSelected = this.OnCropPresetSelected;
			this.View.CropPresetCreateRequested = this.CreateCropPreset;
			this.View.CropPresetRenameRequested = this.RenameCropPreset;
			this.View.CropPresetDeleteRequested = this.DeleteCropPreset;
			this.View.CropPresetSelectAreaRequested = this.SelectCropPresetArea;
			this.View.CropAssignmentsApplyRequested = this.ApplyCropAssignments;
			this.View.CropCycleGroupSelectRequested = this.SelectCropCycleGroup;
			this.View.CropCycleGroupAssignRequested = this.AssignCropToCycleGroup;
			this.View.CropCycleGroupClearRequested = this.ClearCropFromCycleGroup;
			this.View.CropTemporaryCycleGroupAssignRequested = this.AssignCropToTemporaryCycleGroup;
			this.View.DocumentationLinkActivated = this.OpenDocumentationLink;
			this.View.ApplicationExitRequested = this.ExitApplication;
			this.View.LoadNewSettings = this.LoadNewSettings;
			this.View.IconName = this._configuration.IconName;
			this._thumbnailManager.TemporaryCycleGroupsChanged += this.OnTemporaryCycleGroupsChanged;
		}

		private void Activate()
		{
			this._suppressSizeNotifications = true;
			this.LoadApplicationSettings();
			this.View.SetDocumentationUrl(MainFormPresenter.FORUM_URL);
			this.View.SetVersionInfo(this.GetApplicationVersion());
			if (this._configuration.MinimizeToTray)
			{
				this.View.Minimize();
			}

			this._mediator.Send(new StartService());
			this._suppressSizeNotifications = false;
		}

		private void Minimize()
		{
			if (!this._configuration.MinimizeToTray)
			{
				return;
			}

			this.View.Hide();
		}

		private void Close(ViewCloseRequest request)
		{
			if (this._exitApplication || !this.View.MinimizeToTray)
			{
				this._mediator.Send(new StopService()).Wait();

				this._configurationStorage.Save();
				request.Allow = true;
				return;
			}

			request.Allow = false;
			this.View.Minimize();
		}

		private async void UpdateThumbnailsSize()
		{
			if (!this._suppressSizeNotifications)
			{
				this.SaveApplicationSettings();
				await this._mediator.Publish(new ThumbnailConfiguredSizeUpdated());
			}
		}

		private void LoadApplicationSettings()
		{

			this._configurationStorage.Load();
			this._characterLocationTracker.ReloadConfiguration();
			this._isLoadingUi = true;

			if (!string.IsNullOrEmpty(this._configuration.Language) && this._configuration.Language != "en-US")
			{
				LocalizationExtensions.SetLanguage(this._configuration.Language);
			}
			this.View.InitializeLanguageControls();

			this.View.Language = this._configuration.Language;	

			this.View.MinimizeToTray = this._configuration.MinimizeToTray;

			this.View.ThumbnailOpacity = this._configuration.ThumbnailOpacity;

			this.View.EnableClientLayoutTracking = this._configuration.EnableClientLayoutTracking;
			this.View.HideActiveClientThumbnail = this._configuration.HideActiveClientThumbnail;
			this.View.MinimizeInactiveClients = this._configuration.MinimizeInactiveClients;
			this.View.CaptionOnClientsStyle = ViewCaptionBarStyleConverter.Convert(this._configuration.CaptionOnClientsStyle);
			this.View.WindowsAnimationStyle = ViewAnimationStyleConverter.Convert(this._configuration.WindowsAnimationStyle);
			this.View.ShowThumbnailsAlwaysOnTop = this._configuration.ShowThumbnailsAlwaysOnTop;
			this.View.PreventPreviews = this._configuration.PreventPreviews;
			this.View.HideThumbnailsOnLostFocus = this._configuration.HideThumbnailsOnLostFocus;
			this.View.EnablePerClientThumbnailLayouts = this._configuration.EnablePerClientThumbnailLayouts;

			this.View.SetThumbnailSizeLimitations(this._configuration.ThumbnailMinimumSize, this._configuration.ThumbnailMaximumSize);
			this.View.ThumbnailSize = this._configuration.ThumbnailSize;

			this.View.EnableThumbnailZoom = this._configuration.ThumbnailZoomEnabled;
			this.View.ThumbnailZoomFactor = this._configuration.ThumbnailZoomFactor;
			this.View.ThumbnailZoomAnchor = ViewZoomAnchorConverter.Convert(this._configuration.ThumbnailZoomAnchor);
			this.View.OverlayLabelAnchor = ViewZoomAnchorConverter.Convert(this._configuration.OverlayLabelAnchor);
			this.View.SolarSystemLabelAnchor = ViewZoomAnchorConverter.Convert(this._configuration.SolarSystemLabelAnchor);
			this.View.CycleGroupIndicatorAnchor = ViewZoomAnchorConverter.Convert(this._configuration.CycleGroupIndicatorAnchor);

			this.View.ShowThumbnailOverlays = this._configuration.ShowThumbnailOverlays;
			this.View.ShowSolarSystemOverlay = this._configuration.ShowSolarSystemOverlay;
			this.View.ShowThumbnailFrames = this._configuration.ShowThumbnailFrames;
			this.View.LockThumbnailLocation = this._configuration.LockThumbnailLocation;
			this.View.ThumbnailSnapToGrid = this._configuration.ThumbnailSnapToGrid;
			this.View.ThumbnailSnapToGridSizeX = this._configuration.ThumbnailSnapToGridSizeX;
			this.View.ThumbnailSnapToGridSizeY = this._configuration.ThumbnailSnapToGridSizeY;
			this.View.EnableActiveClientHighlight = this._configuration.EnableActiveClientHighlight;
			this.View.ActiveClientHighlightColor = this._configuration.ActiveClientHighlightColor;
			this.View.PreventPreviewColor = this._configuration.PreventPreviewColor;

			this.View.OverlayLabelColor = this._configuration.OverlayLabelColor;
			this.View.OverlayLabelFont = this._configuration.OverlayLabelFont;
			this.View.SolarSystemLabelColor = this._configuration.SolarSystemLabelColor;
			this.View.SolarSystemLabelFont = this._configuration.SolarSystemLabelFont;

			this.View.IconName = this._configuration.IconName;

			// Hotkeys tab: populate clients and default group
			var configuredClients = this._configuration.GetAllKnownClients()
				.Where(client => !IsPlaceholderClient(client))
				.ToList();
			this.View.SetAvailableClients(configuredClients);
			this._currentGroup = 1;
			this._currentGroupIsTemporary = false;
			this.View.SelectedCycleGroupIsTemporary = false;
			this.View.SelectedCycleGroup = this._currentGroup;
			this.LoadGroupToView(this._currentGroup);
			this.RefreshCropsView();

			// Wire group changed handler
			this.View.SelectedCycleGroupChanged = this.OnSelectedCycleGroupChanged;
			this.View.EndUpdateUI();
			this._isLoadingUi = false;
		}

		private void OnSelectedCycleGroupChanged()
		{
			if (this._isLoadingUi) return;
			this.SaveCurrentCycleGroupFromView();
			this._currentGroup = this.View.SelectedCycleGroup;
			this._currentGroupIsTemporary = this.View.SelectedCycleGroupIsTemporary;
			this.View.BeginUpdateUI();
			this.LoadCurrentCycleGroupToView();
			this.View.EndUpdateUI();
			this._configurationStorage.Save();
		}

		private void LoadCurrentCycleGroupToView()
		{
			if (this._currentGroupIsTemporary)
			{
				this.LoadTemporaryGroupToView(this._currentGroup);
			}
			else
			{
				this.LoadGroupToView(this._currentGroup);
			}
		}

		private void SaveCurrentCycleGroupFromView()
		{
			if (this._currentGroupIsTemporary)
			{
				this.SaveTemporaryGroupFromView(this._currentGroup);
			}
			else
			{
				this.SaveGroupFromView(this._currentGroup);
			}
		}

		private void LoadGroupToView(int group)
		{
			// Set forward/backward hotkeys CSV
			var fwd = GetForwardHotkeys(group) ?? new List<string>();
			var bwd = GetBackwardHotkeys(group) ?? new List<string>();
			this.View.CycleGroupForwardHotkeysText = string.Join(",", fwd);
			this.View.CycleGroupBackwardHotkeysText = string.Join(",", bwd);
			// Set clients order
			var orderDict = GetClientsOrder(group) ?? new Dictionary<string, int>();
			var ordered = orderDict.OrderBy(kv => kv.Value)
				.Select(kv => kv.Key)
				.Where(client => !IsPlaceholderClient(client))
				.ToList();
			this.View.SetSelectedClientsForCurrentGroup(ordered);
		}

		private void SaveGroupFromView(int group)
		{
			// Save forward/backward hotkeys
			var fwdCsv = this.View.CycleGroupForwardHotkeysText ?? string.Empty;
			var bwdCsv = this.View.CycleGroupBackwardHotkeysText ?? string.Empty;
			SetForwardHotkeys(group, ParseCsv(fwdCsv));
			SetBackwardHotkeys(group, ParseCsv(bwdCsv));
			// Save clients order (only selected/checked ones, in current order)
			var selected = this.View.GetSelectedClientsForCurrentGroup() ?? new List<string>();
			var dict = new Dictionary<string, int>();
			for (int i = 0; i < selected.Count; i++)
			{
				dict[selected[i]] = i + 1;
			}
			SetClientsOrder(group, dict);
		}

		private static List<string> ParseCsv(string csv)
		{
			var list = new List<string>();
			if (!string.IsNullOrWhiteSpace(csv))
			{
				foreach (var part in csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
				{
					var s = part.Trim();
					if (!string.IsNullOrEmpty(s)) list.Add(s);
				}
			}
			return list;
		}

		private List<string> GetForwardHotkeys(int g) => g switch
		{
			1 => this._configuration.CycleGroup1ForwardHotkeys,
			2 => this._configuration.CycleGroup2ForwardHotkeys,
			3 => this._configuration.CycleGroup3ForwardHotkeys,
			4 => this._configuration.CycleGroup4ForwardHotkeys,
			5 => this._configuration.CycleGroup5ForwardHotkeys,
			_ => this._configuration.CycleGroup1ForwardHotkeys,
		};
		private List<string> GetBackwardHotkeys(int g) => g switch
		{
			1 => this._configuration.CycleGroup1BackwardHotkeys,
			2 => this._configuration.CycleGroup2BackwardHotkeys,
			3 => this._configuration.CycleGroup3BackwardHotkeys,
			4 => this._configuration.CycleGroup4BackwardHotkeys,
			5 => this._configuration.CycleGroup5BackwardHotkeys,
			_ => this._configuration.CycleGroup1BackwardHotkeys,
		};
		private Dictionary<string, int> GetClientsOrder(int g) => g switch
		{
			1 => this._configuration.CycleGroup1ClientsOrder,
			2 => this._configuration.CycleGroup2ClientsOrder,
			3 => this._configuration.CycleGroup3ClientsOrder,
			4 => this._configuration.CycleGroup4ClientsOrder,
			5 => this._configuration.CycleGroup5ClientsOrder,
			_ => this._configuration.CycleGroup1ClientsOrder,
		};
		private void SetForwardHotkeys(int g, List<string> v)
		{
			switch (g)
			{
				case 1: this._configuration.CycleGroup1ForwardHotkeys = v; break;
				case 2: this._configuration.CycleGroup2ForwardHotkeys = v; break;
				case 3: this._configuration.CycleGroup3ForwardHotkeys = v; break;
				case 4: this._configuration.CycleGroup4ForwardHotkeys = v; break;
				case 5: this._configuration.CycleGroup5ForwardHotkeys = v; break;
			}
		}
		private void SetBackwardHotkeys(int g, List<string> v)
		{
			switch (g)
			{
				case 1: this._configuration.CycleGroup1BackwardHotkeys = v; break;
				case 2: this._configuration.CycleGroup2BackwardHotkeys = v; break;
				case 3: this._configuration.CycleGroup3BackwardHotkeys = v; break;
				case 4: this._configuration.CycleGroup4BackwardHotkeys = v; break;
				case 5: this._configuration.CycleGroup5BackwardHotkeys = v; break;
			}
		}
		private void SetClientsOrder(int g, Dictionary<string, int> v)
		{
			switch (g)
			{
				case 1: this._configuration.CycleGroup1ClientsOrder = v; break;
				case 2: this._configuration.CycleGroup2ClientsOrder = v; break;
				case 3: this._configuration.CycleGroup3ClientsOrder = v; break;
				case 4: this._configuration.CycleGroup4ClientsOrder = v; break;
				case 5: this._configuration.CycleGroup5ClientsOrder = v; break;
			}
		}

		private async void SaveApplicationSettings()
		{
			await this.SaveApplicationSettingsAsync();
		}

		private async Task SaveApplicationSettingsAsync(bool allowDuringConfigurationSwitch = false)
		{
			await this._saveSettingsLock.WaitAsync();
			try
			{
				if (this._isLoadingUi ||
					(this._configurationSwitchInProgress && !allowDuringConfigurationSwitch))
				{
					return;
				}
			this._configuration.MinimizeToTray = this.View.MinimizeToTray;

			this._configuration.ThumbnailOpacity = (float)this.View.ThumbnailOpacity;

			if (this._configuration.Language != this.View.Language) {
				this._configuration.Language = this.View.Language;
			}

			this._configuration.EnableClientLayoutTracking = this.View.EnableClientLayoutTracking;
			this._configuration.HideActiveClientThumbnail = this.View.HideActiveClientThumbnail;
			this._configuration.MinimizeInactiveClients = this.View.MinimizeInactiveClients;

			this._configuration.WindowsAnimationStyle = ViewAnimationStyleConverter.Convert(this.View.WindowsAnimationStyle);

			this._configuration.CaptionOnClientsStyle= ViewCaptionBarStyleConverter.Convert(this.View.CaptionOnClientsStyle);
			await this._mediator.Publish(new ThumbnailFrameSettingsUpdated());

			this._configuration.ShowThumbnailsAlwaysOnTop = this.View.ShowThumbnailsAlwaysOnTop;

			if (this._configuration.PreventPreviews != this.View.PreventPreviews)
			{
				this._configuration.PreventPreviews = this.View.PreventPreviews;
				await this._mediator.Publish(new ThumbnailFrameSettingsUpdated());
			}

			this._configuration.HideThumbnailsOnLostFocus = this.View.HideThumbnailsOnLostFocus;
			this._configuration.EnablePerClientThumbnailLayouts = this.View.EnablePerClientThumbnailLayouts;

			this._configuration.ThumbnailSize = this.View.ThumbnailSize;

			this._configuration.ThumbnailZoomEnabled = this.View.EnableThumbnailZoom;
			this._configuration.ThumbnailZoomFactor = this.View.ThumbnailZoomFactor;
			this._configuration.ThumbnailZoomAnchor = ViewZoomAnchorConverter.Convert(this.View.ThumbnailZoomAnchor);
			this._configuration.OverlayLabelAnchor = ViewZoomAnchorConverter.Convert(this.View.OverlayLabelAnchor);
			this._configuration.SolarSystemLabelAnchor = ViewZoomAnchorConverter.Convert(this.View.SolarSystemLabelAnchor);

			if (this._configuration.CycleGroupIndicatorAnchor != ViewZoomAnchorConverter.Convert(this.View.CycleGroupIndicatorAnchor))
			{
				this._configuration.CycleGroupIndicatorAnchor = ViewZoomAnchorConverter.Convert(this.View.CycleGroupIndicatorAnchor);
				await this._mediator.Publish(new ThumbnailCycleGroupIndicatorUpdated());
			}

			this._configuration.ShowThumbnailOverlays = this.View.ShowThumbnailOverlays;
			this._configuration.ShowSolarSystemOverlay = this.View.ShowSolarSystemOverlay;
			if (this._configuration.ShowThumbnailFrames != this.View.ShowThumbnailFrames)
			{
				this._configuration.ShowThumbnailFrames = this.View.ShowThumbnailFrames;
				await this._mediator.Publish(new ThumbnailFrameSettingsUpdated());
			}

            this._configuration.LockThumbnailLocation = this.View.LockThumbnailLocation;
			this._configuration.ThumbnailSnapToGrid = this.View.ThumbnailSnapToGrid;
			this._configuration.ThumbnailSnapToGridSizeX = this.View.ThumbnailSnapToGridSizeX;
            this._configuration.ThumbnailSnapToGridSizeY = this.View.ThumbnailSnapToGridSizeY;

            this._configuration.EnableActiveClientHighlight = this.View.EnableActiveClientHighlight;
			this._configuration.ActiveClientHighlightColor = this.View.ActiveClientHighlightColor;

			if (this._configuration.PreventPreviewColor != this.View.PreventPreviewColor)
			{
				this._configuration.PreventPreviewColor = this.View.PreventPreviewColor;
				await this._mediator.Publish(new ThumbnailFrameSettingsUpdated());
			}

			this._configuration.OverlayLabelColor = this.View.OverlayLabelColor;
			this._configuration.OverlayLabelFont = this.View.OverlayLabelFont;
			this._configuration.SolarSystemLabelColor = this.View.SolarSystemLabelColor;
			this._configuration.SolarSystemLabelFont = this.View.SolarSystemLabelFont;

			this._configuration.IconName = this.View.IconName;

			this.SaveCurrentCycleGroupFromView();
			this._characterLocationTracker.FlushToConfiguration();
			this._configurationStorage.Save();
			this.View.RefreshZoomSettings();
			await this._mediator.Publish(new ThumbnailUpdateClientsLayouts());
			await this._mediator.Send(new SaveConfiguration());
			await this._mediator.Publish(new HotkeysConfigurationUpdated());
			}
			finally
			{
				this._saveSettingsLock.Release();
			}
		}


		public void AddThumbnails(IList<string> thumbnailTitles)
		{
			IList<IThumbnailDescription> descriptions = new List<IThumbnailDescription>(thumbnailTitles.Count);

			lock (this._descriptionsCache)
			{
				foreach (string title in thumbnailTitles)
				{
					IThumbnailDescription description = this.CreateThumbnailDescription(title);
					this._descriptionsCache[title] = description;

					descriptions.Add(description);
				}
			}

			this.View.AddThumbnails(descriptions);
			if (!this.View.HasPendingCropAssignments)
			{
				this.RefreshCropsView(this.View.SelectedCropPresetId);
			}
		}

		public void RemoveThumbnails(IList<string> thumbnailTitles)
		{
			IList<IThumbnailDescription> descriptions = new List<IThumbnailDescription>(thumbnailTitles.Count);

			lock (this._descriptionsCache)
			{
				foreach (string title in thumbnailTitles)
				{
					if (!this._descriptionsCache.TryGetValue(title, out IThumbnailDescription description))
					{
						continue;
					}

					this._descriptionsCache.Remove(title);
					descriptions.Add(description);
				}
			}

			this.View.RemoveThumbnails(descriptions);
			if (!this.View.HasPendingCropAssignments)
			{
				this.RefreshCropsView(this.View.SelectedCropPresetId);
			}
		}

		private IThumbnailDescription CreateThumbnailDescription(string title)
		{
			bool isDisabled = this._configuration.IsThumbnailDisabled(title);
			return new ThumbnailDescription(title, isDisabled);
		}

		private async void UpdateThumbnailState(String title)
		{
			if (this._descriptionsCache.TryGetValue(title, out IThumbnailDescription description))
			{
				this._configuration.ToggleThumbnail(title, description.IsDisabled);
			}

			await this._mediator.Send(new SaveConfiguration());
		}

		private void LoadTemporaryGroupToView(int group)
		{
			this.View.CycleGroupForwardHotkeysText = string.Join(",", GetTemporaryHotkeys(
				this._configuration.TemporaryCycleGroupForwardHotkeys, group));
			this.View.CycleGroupBackwardHotkeysText = string.Join(",", GetTemporaryHotkeys(
				this._configuration.TemporaryCycleGroupBackwardHotkeys, group));
			this.View.SetSelectedClientsForCurrentGroup(this._thumbnailManager.GetTemporaryCycleGroupClients(group));
		}

		private void SaveTemporaryGroupFromView(int group)
		{
			this._configuration.TemporaryCycleGroupForwardHotkeys ??= new Dictionary<int, List<string>>();
			this._configuration.TemporaryCycleGroupBackwardHotkeys ??= new Dictionary<int, List<string>>();
			this._configuration.TemporaryCycleGroupForwardHotkeys[group] = ParseCsv(this.View.CycleGroupForwardHotkeysText);
			this._configuration.TemporaryCycleGroupBackwardHotkeys[group] = ParseCsv(this.View.CycleGroupBackwardHotkeysText);
			this._thumbnailManager.SetTemporaryCycleGroupClients(group, this.View.GetSelectedClientsForCurrentGroup());
		}

		private static List<string> GetTemporaryHotkeys(Dictionary<int, List<string>> hotkeysByGroup, int group)
		{
			return hotkeysByGroup != null && hotkeysByGroup.TryGetValue(group, out List<string> hotkeys)
				? hotkeys ?? new List<string>()
				: new List<string>();
		}

		private void OnTemporaryCycleGroupsChanged()
		{
			if (!this._currentGroupIsTemporary || this._isLoadingUi)
			{
				return;
			}
			this.View.BeginUpdateUI();
			this.LoadTemporaryGroupToView(this._currentGroup);
			this.View.EndUpdateUI();
		}

		private CropRegion ShowCropSelector(string title, IThumbnailView thumbnailView, CropRegion initialRegion)
		{
			Image sourceImage = thumbnailView.WindowManager.GetStaticThumbnail(thumbnailView.Id);
			if (sourceImage == null)
			{
				MessageBox.Show(
					"The client image could not be captured. Make sure the client is open and not minimized, then try again.",
					"Unable to capture client",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
				return null;
			}

			using CropRegionSelectorForm selector = new CropRegionSelectorForm(
				title,
				sourceImage,
				thumbnailView.WindowManager,
				thumbnailView.Id,
				initialRegion);
			IWin32Window owner = this.View as IWin32Window;
			DialogResult result = owner == null ? selector.ShowDialog() : selector.ShowDialog(owner);
			return result == DialogResult.OK ? selector.SelectedRegion : null;
		}

		private void OnCropPresetSelected(string presetId)
		{
			this.LoadCropPresetToView(presetId);
		}

		private void CreateCropPreset(string name, string sourceClient)
		{
			if (this._configuration.CropPresets?.Values.Any(preset =>
				string.Equals(preset?.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase)) == true)
			{
				MessageBox.Show("Crop preset names must be unique.", "Unable to create preset", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			IThumbnailView thumbnailView = this._thumbnailManager.GetClientByTitle(sourceClient);
			if (thumbnailView == null)
			{
				MessageBox.Show("The selected source client is no longer open.", "Unable to create preset", MessageBoxButtons.OK, MessageBoxIcon.Information);
				this.View.SetCropSourceClients(this.GetOpenClients());
				return;
			}

			CropRegion selectedRegion = this.ShowCropSelector(
				sourceClient,
				thumbnailView,
				new CropRegion(0.0, 0.0, 1.0, 1.0));
			if (selectedRegion == null)
			{
				return;
			}

			string presetId = this._configuration.CreateCropPreset(name, selectedRegion);
			if (presetId == null)
			{
				MessageBox.Show("Crop preset names must be unique.", "Unable to create preset", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			this._configurationStorage.Save();
			this.RefreshCropsView(presetId);
		}

		private void RenameCropPreset(string presetId, string name)
		{
			if (!this._configuration.RenameCropPreset(presetId, name))
			{
				MessageBox.Show("Crop preset names must be unique.", "Unable to rename preset", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			this._configurationStorage.Save();
			this.RefreshCropsView(presetId);
		}

		private void DeleteCropPreset(string presetId)
		{
			IList<string> affected = this._configuration.DeleteCropPreset(presetId);
			this.SaveAndRefreshCrops(affected, null);
		}

		private void SelectCropPresetArea(string presetId, string sourceClient)
		{
			CropPreset preset = this._configuration.GetCropPreset(presetId);
			IThumbnailView thumbnailView = this._thumbnailManager.GetClientByTitle(sourceClient);
			if (preset == null || thumbnailView == null)
			{
				MessageBox.Show("The selected preset or source client is no longer available.", "Unable to edit crop", MessageBoxButtons.OK, MessageBoxIcon.Information);
				this.View.SetCropSourceClients(this.GetOpenClients());
				return;
			}

			CropRegion selectedRegion = this.ShowCropSelector(sourceClient, thumbnailView, preset.Region);
			if (selectedRegion == null)
			{
				return;
			}

			IList<string> affected = this._configuration.UpdateCropPresetRegion(presetId, selectedRegion);
			this.SaveAndRefreshCrops(affected, presetId);
		}

		private bool ApplyCropAssignments(string presetId, IList<string> checkedClients)
		{
			CropPreset preset = this._configuration.GetCropPreset(presetId);
			if (preset == null)
			{
				return false;
			}

			HashSet<string> desired = new HashSet<string>(checkedClients ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
			HashSet<string> current = new HashSet<string>(this.GetClientsAssignedToPreset(presetId), StringComparer.OrdinalIgnoreCase);
			List<string> additions = desired.Except(current, StringComparer.OrdinalIgnoreCase).ToList();
			List<string> removals = current.Except(desired, StringComparer.OrdinalIgnoreCase).ToList();
			int conflicts = additions.Count(client =>
			{
				CropPreset currentPreset = this._configuration.GetCropPresetForClient(client);
				return currentPreset != null && !string.Equals(currentPreset.Id, presetId, StringComparison.Ordinal);
			});

			if (additions.Count == 0 && removals.Count == 0)
			{
				return true;
			}

			string message = $"Assign ‘{preset.Name}’ to {additions.Count} character(s) and return {removals.Count} to Full window?";
			if (conflicts > 0)
			{
				message += $"\n\n{conflicts} character(s) currently use another crop preset.";
			}
			if (MessageBox.Show(message, "Apply crop assignments", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
			{
				return false;
			}

			foreach (string client in additions)
			{
				this._configuration.AssignCropPreset(client, presetId);
			}
			foreach (string client in removals)
			{
				this._configuration.UnassignCropPreset(client);
			}
			this.SaveAndRefreshCrops(additions.Concat(removals), presetId);
			return true;
		}

		private void SelectCropCycleGroup(int group, bool onlyOpen)
		{
			this.View.SelectCropClients(this.GetCycleGroupClients(group), onlyOpen);
		}

		private bool AssignCropToCycleGroup(string presetId, int group, bool keepExisting, bool onlyOpen)
		{
			CropPreset preset = this._configuration.GetCropPreset(presetId);
			if (preset == null)
			{
				return false;
			}

			HashSet<string> openClients = new HashSet<string>(this.GetOpenClients(), StringComparer.OrdinalIgnoreCase);
			List<string> groupClients = this.GetCycleGroupClients(group)
				.Where(client => !onlyOpen || openClients.Contains(client))
				.Where(client => !keepExisting || this._configuration.GetCropPresetForClient(client) == null)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
			if (groupClients.Count == 0)
			{
				MessageBox.Show(
					"No characters matched the selected Cycle Group options.",
					"Nothing to assign",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				return true;
			}
			int conflicts = groupClients.Count(client =>
			{
				CropPreset current = this._configuration.GetCropPresetForClient(client);
				return current != null && !string.Equals(current.Id, presetId, StringComparison.Ordinal);
			});

			string message = $"Assign ‘{preset.Name}’ to {groupClients.Count} current member(s) of Cycle Group {group}?" +
				"\n\nThis is a one-time assignment; characters added to the group later are not changed automatically.";
			if (conflicts > 0)
			{
				message += $"\n\n{conflicts} character(s) currently use another crop preset.";
			}
			if (MessageBox.Show(message, "Assign crop to cycle group", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
			{
				return false;
			}

			foreach (string client in groupClients)
			{
				this._configuration.AssignCropPreset(client, presetId);
			}
			this.SaveAndRefreshCrops(groupClients, presetId);
			return true;
		}

		private bool ClearCropFromCycleGroup(int group)
		{
			List<string> affectedClients = this.GetCycleGroupClients(group)
				.Where(client => this._configuration.GetCropPresetForClient(client) != null)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
			if (affectedClients.Count == 0)
			{
				MessageBox.Show(
					$"No current member of Cycle Group {group} has a crop assigned.",
					"Nothing to clear",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				return true;
			}

			if (MessageBox.Show(
				$"Return {affectedClients.Count} current member(s) of Cycle Group {group} to Full window?\n\n" +
				"This is a one-time change to the group's current members.",
				"Clear Cycle Group crops",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question) != DialogResult.Yes)
			{
				return false;
			}

			foreach (string client in affectedClients)
			{
				this._configuration.UnassignCropPreset(client);
			}
			this.SaveAndRefreshCrops(affectedClients, this.View.SelectedCropPresetId);
			return true;
		}

		private bool AssignCropToTemporaryCycleGroup(string presetId, int group)
		{
			CropPreset preset = this._configuration.GetCropPreset(presetId);
			if (preset == null)
			{
				return false;
			}
			List<string> clients = this._thumbnailManager.GetTemporaryCycleGroupClients(group)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
			if (clients.Count == 0)
			{
				MessageBox.Show(
					$"Temp {group} has no members.",
					"Nothing to assign",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				return true;
			}
			int conflicts = clients.Count(client =>
			{
				CropPreset current = this._configuration.GetCropPresetForClient(client);
				return current != null && !string.Equals(current.Id, presetId, StringComparison.Ordinal);
			});
			string message = $"Assign ‘{preset.Name}’ to {clients.Count} current member(s) of Temp {group}?";
			if (conflicts > 0)
			{
				message += $"\n\n{conflicts} character(s) currently use another crop preset.";
			}
			if (MessageBox.Show(message, "Assign crop to temporary group", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
			{
				return false;
			}
			foreach (string client in clients)
			{
				this._configuration.AssignCropPreset(client, presetId);
			}
			this.SaveAndRefreshCrops(clients, presetId);
			return true;
		}

		private void RefreshCropsView(string selectedPresetId = null)
		{
			IEnumerable<CropPreset> configuredPresets = this._configuration.CropPresets == null
				? Enumerable.Empty<CropPreset>()
				: this._configuration.CropPresets.Values;
			List<CropPreset> presets = configuredPresets
				.Where(preset => preset?.IsValid == true)
				.ToList();
			if (this._configuration.GetCropPreset(selectedPresetId) == null)
			{
				selectedPresetId = presets.OrderBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault()?.Id;
			}
			this.View.SetCropPresets(presets, selectedPresetId);
			this.LoadCropPresetToView(this.View.SelectedCropPresetId);
		}

		private void LoadCropPresetToView(string presetId)
		{
			CropPreset preset = this._configuration.GetCropPreset(presetId);
			List<string> openClients = this.GetOpenClients();
			HashSet<string> allClients = new HashSet<string>(this._configuration.GetAllKnownClients(), StringComparer.OrdinalIgnoreCase);
			allClients.UnionWith(openClients);
			if (this._configuration.PerClientCropPresetIds != null)
			{
				allClients.UnionWith(this._configuration.PerClientCropPresetIds.Keys);
			}
			allClients.RemoveWhere(IsPlaceholderClient);

			Dictionary<string, string> currentPresetNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (string client in allClients)
			{
				CropPreset clientPreset = this._configuration.GetCropPresetForClient(client);
				if (clientPreset != null)
				{
					currentPresetNames[client] = clientPreset.Name;
				}
			}

			HashSet<string> checkedClients = new HashSet<string>(
				preset == null ? new List<string>() : this.GetClientsAssignedToPreset(preset.Id),
				StringComparer.OrdinalIgnoreCase);
			this.View.SetCropPresetRegion(preset?.Region);
			this.View.SetCropSourceClients(openClients);
			this.View.SetCropClients(
				allClients.OrderBy(client => client, StringComparer.OrdinalIgnoreCase).ToList(),
				currentPresetNames,
				new HashSet<string>(openClients, StringComparer.OrdinalIgnoreCase),
				checkedClients);
		}

		private void SaveAndRefreshCrops(IEnumerable<string> clients, string selectedPresetId)
		{
			this._configurationStorage.Save();
			foreach (string client in (clients ?? Enumerable.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				this._thumbnailManager.GetClientByTitle(client)?.Refresh(true);
			}
			this.RefreshCropsView(selectedPresetId);
		}

		private List<string> GetClientsAssignedToPreset(string presetId)
		{
			return this._configuration.PerClientCropPresetIds?
				.Where(entry => string.Equals(entry.Value, presetId, StringComparison.Ordinal))
				.Select(entry => entry.Key)
				.ToList() ?? new List<string>();
		}

		private List<string> GetOpenClients()
		{
			lock (this._descriptionsCache)
			{
				return this._descriptionsCache.Keys.OrderBy(client => client, StringComparer.OrdinalIgnoreCase).ToList();
			}
		}

		private List<string> GetCycleGroupClients(int group)
		{
			if (!this._isLoadingUi && !this._currentGroupIsTemporary && group == this._currentGroup)
			{
				this.SaveGroupFromView(this._currentGroup);
			}
			Dictionary<string, int> clients = GetClientsOrder(group) ?? new Dictionary<string, int>();
			return clients.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToList();
		}

		private static bool IsPlaceholderClient(string title)
		{
			return title?.StartsWith("EVE - Example ", StringComparison.OrdinalIgnoreCase) == true ||
				title?.StartsWith("EVE - cycle group ", StringComparison.OrdinalIgnoreCase) == true;
		}

		public void UpdateThumbnailSize(Size size)
		{
			this._suppressSizeNotifications = true;
			this.View.ThumbnailSize = size;
			this._configuration.ThumbnailSize = size;
			this._suppressSizeNotifications = false;
		}

		private void OpenDocumentationLink()
		{
			// funtimes
			// https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/
			// https://github.com/dotnet/runtime/issues/17938

			// TODO Move out to a separate service / presenter / message handler
#if LINUX
			Process.Start("xdg-open", new Uri(MainFormPresenter.FORUM_URL).AbsoluteUri);
#else
			ProcessStartInfo processStartInfo = new ProcessStartInfo(new Uri(MainFormPresenter.FORUM_URL).AbsoluteUri);
			processStartInfo.UseShellExecute = true;
			Process.Start(processStartInfo);
#endif
		}

		private string GetApplicationVersion()
		{
			Version version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
			string target = "Windows";
#if LINUX
  target = "Linux";
#endif
			return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision} {target}";
		}

		private void ExitApplication()
		{
			this._exitApplication = true;
			this.View.Close();
		}

		public List<string> GetActiveClients()
		{
			return _descriptionsCache?.Select(x => x.Value.Title).ToList();
		}

		public async void LoadNewSettings(string filename)
		{
			if (this._configurationSwitchInProgress)
			{
				return;
			}

			this._configurationSwitchInProgress = true;
			try
			{
				// Finish saving the active profile before changing the storage target.
				// Otherwise an async UI save can overwrite the newly selected JSON.
				await this.SaveApplicationSettingsAsync(allowDuringConfigurationSwitch: true);
				await this._mediator.Send(new StopService());
				if (!string.IsNullOrEmpty(filename))
				{
					this._configurationStorage.SetConfigurationFilename(filename);
				}

				this.LoadApplicationSettings();

				await this._mediator.Publish(new ThumbnailFrameSettingsUpdated());
				await this._mediator.Publish(new ThumbnailApplyAllClientsLayouts());
				await this._mediator.Publish(new ThumbnailCycleGroupIndicatorUpdated());
				this.View.RefreshZoomSettings();
				await this._mediator.Publish(new HotkeysConfigurationUpdated());
			}
			finally
			{
				this._configurationSwitchInProgress = false;
			}
		}
	}
}
