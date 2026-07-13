using EveOPreview.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace EveOPreview.View
{
	/// <summary>
	/// Main view interface
	/// Presenter uses it to access GUI properties
	/// </summary>
	public interface IMainFormView : IView
	{
		string Language { get; set; }
		bool MinimizeToTray { get; set; }

		double ThumbnailOpacity { get; set; }

		bool EnableClientLayoutTracking { get; set; }
		bool HideActiveClientThumbnail { get; set; }
		bool MinimizeInactiveClients { get; set; }
		ViewCaptionBarStyle CaptionOnClientsStyle { get; set; }
		ViewAnimationStyle WindowsAnimationStyle { get; set; }
        bool ShowThumbnailsAlwaysOnTop { get; set; }
		bool PreventPreviews { get; set; }
		bool HideThumbnailsOnLostFocus { get; set; }
		bool EnablePerClientThumbnailLayouts { get; set; }

		Size ThumbnailSize { get; set; }

		bool EnableThumbnailZoom { get; set; }
		int ThumbnailZoomFactor { get; set; }
		ViewZoomAnchor ThumbnailZoomAnchor { get; set; }
		ViewZoomAnchor OverlayLabelAnchor { get; set; }
		ViewZoomAnchor CycleGroupIndicatorAnchor { get; set; }

		bool ShowThumbnailOverlays { get; set; }
		bool ShowSolarSystemOverlay { get; set; }
		bool ShowThumbnailFrames { get; set; }

		bool LockThumbnailLocation { get; set; }
		bool ThumbnailSnapToGrid { get; set; }
		int ThumbnailSnapToGridSizeX { get; set; }
		int ThumbnailSnapToGridSizeY { get; set; }

		bool EnableActiveClientHighlight { get; set; }
		Color ActiveClientHighlightColor { get; set; }
		Color PreventPreviewColor { get; set; }
		Color OverlayLabelColor { get; set; }
		Font OverlayLabelFont { get; set; }

		string IconName { get; set; }

		int SelectedCycleGroup { get; set; } // 1..5
		string CycleGroupForwardHotkeysText { get; set; }
		string CycleGroupBackwardHotkeysText { get; set; }

		void SetAvailableClients(IList<string> clients);
		IList<string> GetSelectedClientsForCurrentGroup();
		void SetSelectedClientsForCurrentGroup(IList<string> orderedClients);

		string SelectedCropPresetId { get; }
		bool HasPendingCropAssignments { get; }
		void SetCropPresets(IList<CropPreset> presets, string selectedPresetId);
		void SetCropPresetRegion(CropRegion region);
		void SetCropSourceClients(IList<string> clients);
		void SetCropClients(
			IList<string> clients,
			IDictionary<string, string> currentPresetNames,
			ISet<string> openClients,
			ISet<string> checkedClients);
		void SelectCropClients(IList<string> clients, bool onlyOpen);

		void SetDocumentationUrl(string url);
		void SetVersionInfo(string version);
		void SetThumbnailSizeLimitations(Size minimumSize, Size maximumSize);

		void Minimize();

		void AddThumbnails(IList<IThumbnailDescription> thumbnails);
		void RemoveThumbnails(IList<IThumbnailDescription> thumbnails);
		void RefreshZoomSettings();

		Action ApplicationExitRequested { get; set; }
		Action<string> LoadNewSettings { get; set; }
		Action SaveSettings { get; set; }

		Action FormActivated { get; set; }
		Action FormMinimized { get; set; }
		Action<ViewCloseRequest> FormCloseRequested { get; set; }
		Action ApplicationSettingsChanged { get; set; }
		Action ThumbnailsSizeChanged { get; set; }
		Action<string> ThumbnailStateChanged { get; set; }
		Action<string> CropRegionSelectionRequested { get; set; }
		Action<string> CropRegionResetRequested { get; set; }
		Action<string> CropPresetSelected { get; set; }
		Action<string> CropPresetCreateRequested { get; set; }
		Action<string, string> CropPresetRenameRequested { get; set; }
		Action<string> CropPresetDeleteRequested { get; set; }
		Action<string, string> CropPresetSelectAreaRequested { get; set; }
		Func<string, IList<string>, bool> CropAssignmentsApplyRequested { get; set; }
		Action<int, bool> CropCycleGroupSelectRequested { get; set; }
		Func<string, int, bool, bool, bool> CropCycleGroupAssignRequested { get; set; }
		Action DocumentationLinkActivated { get; set; }
		void InitializeLanguageControls();

		Action SelectedCycleGroupChanged { get; set; }
		void BeginUpdateUI();
		void EndUpdateUI();

	}
}
