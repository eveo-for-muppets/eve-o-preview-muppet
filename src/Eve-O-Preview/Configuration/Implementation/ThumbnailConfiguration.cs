using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace EveOPreview.Configuration.Implementation
{
	sealed class ThumbnailConfiguration : IThumbnailConfiguration
	{
		#region Private fields
		private bool _enablePerClientThumbnailLayouts;
		private bool _enableClientLayoutTracking;
		#endregion

		public ThumbnailConfiguration()
		{
			this.ConfigVersion = 1;

			this.Language = "en-US";

			this.CycleGroup1ForwardHotkeys = new List<string> { "F14", "Control+F14" };
			this.CycleGroup1BackwardHotkeys = new List<string> { "F13", "Control+F13" };
			this.CycleGroup1ClientsOrder = new Dictionary<string, int>
			{
				{ "EVE - Example DPS Toon 1", 1 },
				{ "EVE - Example DPS Toon 2", 2 },
				{ "EVE - Example DPS Toon 3", 3 }
			};

			this.CycleGroup2ForwardHotkeys = new List<string> { "F16", "Control+F16" };
			this.CycleGroup2BackwardHotkeys = new List<string> { "F15", "Control+F15" };
			this.CycleGroup2ClientsOrder = new Dictionary<string, int>
			{
				{ "EVE - Example Logi Toon 1", 1 },
				{ "EVE - Example Scout Toon 2", 2 },
				{ "EVE - Example Tackle Toon 3", 3 }
			};

			this.CycleGroup3ForwardHotkeys = new List<string> { "" };
			this.CycleGroup3BackwardHotkeys = new List<string> { "" };
			this.CycleGroup3ClientsOrder = new Dictionary<string, int>
			{
				{ "EVE - cycle group 3", 1 },
			};
			this.CycleGroup4ForwardHotkeys = new List<string> { "" };
			this.CycleGroup4BackwardHotkeys = new List<string> { "" };
			this.CycleGroup4ClientsOrder = new Dictionary<string, int>
			{
				{ "EVE - cycle group 4", 1 },
			};
			this.CycleGroup5ForwardHotkeys = new List<string> { "" };
			this.CycleGroup5BackwardHotkeys = new List<string> { "" };
			this.CycleGroup5ClientsOrder = new Dictionary<string, int>
			{
				{ "EVE - cycle group 5", 1 },
			};

			this.PerClientActiveClientHighlightColor = new Dictionary<string, Color>
			{
				{"EVE - Example Toon 1", Color.Red},
				{"EVE - Example Toon 2", Color.Green}
			};
			this.PerClientPreventPreviewColor = new Dictionary<string, Color>
			{
				{"EVE - Example Toon 1", Color.Red},
				{"EVE - Example Toon 2", Color.Green}
			};
			this.PerClientPreventPreviews = new Dictionary<string, bool>
			{
				{"EVE - Example Toon 1", false},
				{"EVE - Example Toon 2", true}
			};

			this.PerClientAliases = new Dictionary<string, string>
			{
				{"EVE - Example Toon 1", "Main"},
				{"EVE - Example Toon 2", "Alt"}
			};

			this.PerClientThumbnailSize = new Dictionary<string, Size>
			{
				{"EVE - Example Toon 1", new Size(200, 200)},
				{"EVE - Example Toon 2", new Size(200, 200)}
			};

			this.PerClientCropRegions = new Dictionary<string, CropRegion>();
			this.CropPresets = new Dictionary<string, CropPreset>();
			this.PerClientCropPresetIds = new Dictionary<string, string>();
			this.PerClientSolarSystems = new Dictionary<string, string>();
			this.PerClientSolarSystemObservations = new Dictionary<string, SolarSystemObservation>();

			this.PerClientZoomAnchor = new Dictionary<string, ZoomAnchor>
			{
				{"EVE - Example Toon 1", ZoomAnchor.N },
				{"EVE - Example Toon 2", ZoomAnchor.S}
			};

			this.PerClientLayout = new Dictionary<string, Dictionary<string, Point>>();
			this.FlatLayout = new Dictionary<string, Point>();
			this.ClientLayout = new Dictionary<string, ClientLayout>();
			this.ClientHotkey = new Dictionary<string, string>();
			this.MinimizeAllClientsHotkeys = new List<string> { "Control+F22" };
			this.RefreshMinimizedClientsHotkeys = new List<string> { "Control+F21" };
			this.DisableThumbnail = new Dictionary<string, bool>();
			this.PriorityClients = new List<string>();

			this.ExecutablesToPreview = new List<string> { "exefile" };

			this.MinimizeToTray = false;
			this.ThumbnailRefreshPeriod = 500;
			this.ThumbnailResizeTimeoutPeriod = 500;

#if LINUX
			this.EnableWineCompatibilityMode = true;
#else
			this.EnableWineCompatibilityMode = false;
#endif

			this.ThumbnailOpacity = 0.5;

			this.EnableClientLayoutTracking = false;
			this.HideActiveClientThumbnail = false;
			this.HideLoginClientThumbnail = false;
			this.MinimizeInactiveClients = false;
			this.CaptionOnClientsStyle = CaptionBarStyle.DoNothing;
			this.WindowsAnimationStyle = AnimationStyle.NoAnimation;
			this.ShowThumbnailsAlwaysOnTop = true;
			this.EnablePerClientThumbnailLayouts = false;

			this.HideThumbnailsOnLostFocus = false;
			this.PreventPreviews = false;
			this.HideThumbnailsDelay = 2; // 2 thumbnails refresh cycles (1.0 sec)

			this.ThumbnailSize = new Size(384, 216);
			this.ThumbnailMinimumSize = new Size(192, 108);
			this.ThumbnailMaximumSize = new Size(960, 540);

			this.EnableThumbnailSnap = true;

			this.ThumbnailZoomEnabled = false;
			this.ThumbnailZoomFactor = 2;
			this.ThumbnailZoomAnchor = ZoomAnchor.NW;
			this.OverlayLabelAnchor = ZoomAnchor.NW;
			this.CycleGroupIndicatorAnchor = ZoomAnchor.NW;

			this.ShowThumbnailOverlays = true;
			this.ShowSolarSystemOverlay = true;
			this.ShowThumbnailFrames = false;
			this.LockThumbnailLocation = false;

			this.ThumbnailSnapToGrid = true;
			this.ThumbnailSnapToGridSizeX = 100;
			this.ThumbnailSnapToGridSizeY = 50;

            this.EnableActiveClientHighlight = false;
			this.ActiveClientHighlightColor = Color.GreenYellow;
			this.PreventPreviewColor = Color.Purple;
			this.ActiveClientHighlightThickness = 3;

			this.OverlayLabelColor = Color.Orange;
			this.OverlayLabelFont = new Font(FontFamily.GenericSansSerif,10.0F, FontStyle.Bold);

			this.IconName = "";

			this.LoginThumbnailLocation = new Point(5, 5);
		}


		[JsonProperty("ConfigVersion")]
		public int ConfigVersion { get; set; }

		[JsonProperty("Language")]
		public string Language { get; set; }

		[JsonIgnore]
		public Dictionary<string, bool> CycleGroupExclusions { get; set; }

		[JsonProperty("CycleGroup1ForwardHotkeys")]
		public List<string> CycleGroup1ForwardHotkeys { get; set; }

		[JsonProperty("CycleGroup1BackwardHotkeys")]
		public List<string> CycleGroup1BackwardHotkeys { get; set; }

		[JsonProperty("CycleGroup1ClientsOrder")]
		public Dictionary<string, int> CycleGroup1ClientsOrder { get; set; }

		[JsonProperty("CycleGroup2ForwardHotkeys")]
		public List<string> CycleGroup2ForwardHotkeys { get; set; }

		[JsonProperty("CycleGroup2BackwardHotkeys")]
		public List<string> CycleGroup2BackwardHotkeys { get; set; }

		[JsonProperty("CycleGroup2ClientsOrder")]
		public Dictionary<string, int> CycleGroup2ClientsOrder { get; set; }

		[JsonProperty("CycleGroup3ForwardHotkeys")]
		public List<string> CycleGroup3ForwardHotkeys { get; set; }

		[JsonProperty("CycleGroup3BackwardHotkeys")]
		public List<string> CycleGroup3BackwardHotkeys { get; set; }

		[JsonProperty("CycleGroup3ClientsOrder")]
		public Dictionary<string, int> CycleGroup3ClientsOrder { get; set; }

		[JsonProperty("CycleGroup4ForwardHotkeys")]
		public List<string> CycleGroup4ForwardHotkeys { get; set; }

		[JsonProperty("CycleGroup4BackwardHotkeys")]
		public List<string> CycleGroup4BackwardHotkeys { get; set; }

		[JsonProperty("CycleGroup4ClientsOrder")]
		public Dictionary<string, int> CycleGroup4ClientsOrder { get; set; }

		[JsonProperty("CycleGroup5ForwardHotkeys")]
		public List<string> CycleGroup5ForwardHotkeys { get; set; }

		[JsonProperty("CycleGroup5BackwardHotkeys")]
		public List<string> CycleGroup5BackwardHotkeys { get; set; }

		[JsonProperty("CycleGroup5ClientsOrder")]
		public Dictionary<string, int> CycleGroup5ClientsOrder { get; set; }

		[JsonProperty("PerClientPreventPreviewColor")]
		public Dictionary<string, Color> PerClientPreventPreviewColor { get; set; }

		[JsonProperty("PerClientActiveClientHighlightColor")]
		public Dictionary<string, Color> PerClientActiveClientHighlightColor { get; set; }

		[JsonProperty("PerClientPreventPreviews")]
		public Dictionary<string, bool> PerClientPreventPreviews { get; set; }

		[JsonProperty("PerClientAliases")]
		public Dictionary<string, string> PerClientAliases { get; set; }

		[JsonProperty("PerClientThumbnailSize")]
		public Dictionary<string, Size> PerClientThumbnailSize { get; set; }

		[JsonProperty("PerClientCropRegions")]
		public Dictionary<string, CropRegion> PerClientCropRegions { get; set; }

		[JsonProperty("CropPresets")]
		public Dictionary<string, CropPreset> CropPresets { get; set; }

		[JsonProperty("PerClientCropPresetIds")]
		public Dictionary<string, string> PerClientCropPresetIds { get; set; }

		[JsonProperty("PerClientSolarSystems")]
		public Dictionary<string, string> PerClientSolarSystems { get; set; }

		[JsonProperty("PerClientSolarSystemObservations")]
		public Dictionary<string, SolarSystemObservation> PerClientSolarSystemObservations { get; set; }

		[JsonProperty("PerClientZoomAnchor")]
		public Dictionary<string, ZoomAnchor> PerClientZoomAnchor{ get; set; }
		public bool MinimizeToTray { get; set; }
		public int ThumbnailRefreshPeriod { get; set; }
		public int ThumbnailResizeTimeoutPeriod { get; set; }

		[JsonProperty("WineCompatibilityMode")]
		public bool EnableWineCompatibilityMode { get; set; }

		[JsonProperty("ThumbnailsOpacity")]
		public double ThumbnailOpacity { get; set; }

		public bool EnableClientLayoutTracking
		{
			get => this._enableClientLayoutTracking;
			set
			{
				if (!value)
				{
					this.ClientLayout.Clear();
				}

				this._enableClientLayoutTracking = value;
			}
		}

		public bool HideActiveClientThumbnail { get; set; }
		public bool HideLoginClientThumbnail { get; set; }
		public bool MinimizeInactiveClients { get; set; }
		public CaptionBarStyle CaptionOnClientsStyle { get; set; }
		public AnimationStyle WindowsAnimationStyle { get; set; }
		public bool ShowThumbnailsAlwaysOnTop { get; set; }

		public bool EnablePerClientThumbnailLayouts
		{
			get => this._enablePerClientThumbnailLayouts;
			set
			{
				if (!value)
				{
					this.PerClientLayout.Clear();
				}

				this._enablePerClientThumbnailLayouts = value;
			}
		}

		public bool PreventPreviews { get; set; }
		public bool HideThumbnailsOnLostFocus { get; set; }
		public int HideThumbnailsDelay { get; set; }

		public Size ThumbnailSize { get; set; }
		public Size ThumbnailMaximumSize { get; set; }
		public Size ThumbnailMinimumSize { get; set; }

		public bool EnableThumbnailSnap { get; set; }

		[JsonProperty("EnableThumbnailZoom")]
		public bool ThumbnailZoomEnabled { get; set; }
		public int ThumbnailZoomFactor { get; set; }
		public ZoomAnchor ThumbnailZoomAnchor { get; set; }
		public ZoomAnchor OverlayLabelAnchor { get; set; }
		public ZoomAnchor CycleGroupIndicatorAnchor { get; set; }

		public bool ShowThumbnailOverlays { get; set; }
		public bool ShowSolarSystemOverlay { get; set; }
		public bool ShowThumbnailFrames { get; set; }
		public bool LockThumbnailLocation { get; set; }
		public bool ThumbnailSnapToGrid { get; set; }
		public int ThumbnailSnapToGridSizeX {  get; set; }
		public int ThumbnailSnapToGridSizeY { get; set; }

		public bool EnableActiveClientHighlight { get; set; }

		public Color ActiveClientHighlightColor { get; set; }
		public Color PreventPreviewColor { get; set; }
		public Color OverlayLabelColor { get; set; }

		[JsonProperty]
		public Font OverlayLabelFont { get; set; }
		public string IconName { get; set; }

		public int ActiveClientHighlightThickness { get; set; }

		[JsonProperty("LoginThumbnailLocation")]
		public Point LoginThumbnailLocation { get; set; }

		[JsonProperty]
		private Dictionary<string, Dictionary<string, Point>> PerClientLayout { get; set; }
		[JsonProperty]
		private Dictionary<string, Point> FlatLayout { get; set; }
		[JsonProperty]
		private Dictionary<string, ClientLayout> ClientLayout { get; set; }
		[JsonProperty]
		private Dictionary<string, string> ClientHotkey { get; set; }
		[JsonProperty]
		public List<string> MinimizeAllClientsHotkeys { get; set; }
		[JsonProperty]
		public List<string> RefreshMinimizedClientsHotkeys { get; set; }
		[JsonProperty]
		private Dictionary<string, bool> DisableThumbnail { get; set; }
		[JsonProperty]
		private List<string> PriorityClients { get; set; }
		[JsonProperty]
		private List<string> ExecutablesToPreview { get; set; }

		public Point GetThumbnailLocation(string currentClient, string activeClient, Point defaultLocation)
		{
			Point location;

			// What this code does:
			// If Per-Client layouts are enabled
			//    and client name is known
			//    and there is a separate thumbnails layout for this client
			//    and this layout contains an entry for the current client
			// then return that entry
			// otherwise try to get client layout from the flat all-clients layout
			// If there is no layout too then use the default one
			if (this.EnablePerClientThumbnailLayouts && !string.IsNullOrEmpty(activeClient))
			{
				Dictionary<string, Point> layoutSource;
				if (this.PerClientLayout.TryGetValue(activeClient, out layoutSource) && layoutSource.TryGetValue(currentClient, out location))
				{
					return location;
				}
			}

			return this.FlatLayout.TryGetValue(currentClient, out location) ? location : defaultLocation;
		}

		public Size GetThumbnailSize(string currentClient, string activeClient, Size defaultSize)
		{
			Size sizeOfThumbnail;
			return this.PerClientThumbnailSize.TryGetValue(currentClient, out sizeOfThumbnail) ? sizeOfThumbnail : defaultSize;
		}
		public ZoomAnchor GetZoomAnchor(string currentClient, ZoomAnchor defaultZoomAnchor)
		{
			ZoomAnchor zoomAnchor;
			return this.PerClientZoomAnchor.TryGetValue(currentClient, out zoomAnchor) ? zoomAnchor : defaultZoomAnchor;
		}

		public CropRegion GetCropRegion(string currentClient)
		{
			CropPreset preset = this.GetCropPresetForClient(currentClient);
			if (preset?.Region?.IsValid == true)
			{
				return preset.Region;
			}

			if (this.PerClientCropRegions != null &&
				this.PerClientCropRegions.TryGetValue(currentClient, out CropRegion cropRegion) &&
				cropRegion?.IsValid == true)
			{
				return cropRegion;
			}

			return null;
		}

		public CropPreset GetCropPreset(string presetId)
		{
			if (string.IsNullOrWhiteSpace(presetId) || this.CropPresets == null)
			{
				return null;
			}

			return this.CropPresets.TryGetValue(presetId, out CropPreset preset) && preset?.IsValid == true
				? preset
				: null;
		}

		public CropPreset GetCropPresetForClient(string currentClient)
		{
			if (string.IsNullOrWhiteSpace(currentClient) ||
				this.PerClientCropPresetIds == null ||
				!this.PerClientCropPresetIds.TryGetValue(currentClient, out string presetId))
			{
				return null;
			}

			return this.GetCropPreset(presetId);
		}

		public string CreateCropPreset(string name, CropRegion cropRegion)
		{
			if (string.IsNullOrWhiteSpace(name) || cropRegion?.IsValid != true)
			{
				return null;
			}

			this.CropPresets ??= new Dictionary<string, CropPreset>();
			if (this.CropPresets.Values.Any(preset =>
				string.Equals(preset?.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
			{
				return null;
			}

			CropPreset newPreset = CropPreset.Create(name, CloneRegion(cropRegion));
			this.CropPresets[newPreset.Id] = newPreset;
			return newPreset.Id;
		}

		public bool RenameCropPreset(string presetId, string name)
		{
			CropPreset preset = this.GetCropPreset(presetId);
			if (preset == null || string.IsNullOrWhiteSpace(name))
			{
				return false;
			}

			string trimmedName = name.Trim();
			if (this.CropPresets.Values.Any(other =>
				other != null &&
				!string.Equals(other.Id, presetId, StringComparison.Ordinal) &&
				string.Equals(other.Name, trimmedName, StringComparison.OrdinalIgnoreCase)))
			{
				return false;
			}

			preset.Name = trimmedName;
			return true;
		}

		public IList<string> DeleteCropPreset(string presetId)
		{
			List<string> affectedClients = this.GetClientsAssignedToPreset(presetId);
			foreach (string client in affectedClients)
			{
				this.UnassignCropPreset(client);
			}

			this.CropPresets?.Remove(presetId);
			return affectedClients;
		}

		public IList<string> UpdateCropPresetRegion(string presetId, CropRegion cropRegion)
		{
			CropPreset preset = this.GetCropPreset(presetId);
			if (preset == null || cropRegion?.IsValid != true)
			{
				return new List<string>();
			}

			preset.Region = CloneRegion(cropRegion);
			List<string> affectedClients = this.GetClientsAssignedToPreset(presetId);
			foreach (string client in affectedClients)
			{
				this.PerClientCropRegions[client] = CloneRegion(preset.Region);
			}

			return affectedClients;
		}

		public void AssignCropPreset(string currentClient, string presetId)
		{
			CropPreset preset = this.GetCropPreset(presetId);
			if (string.IsNullOrWhiteSpace(currentClient) || preset == null)
			{
				return;
			}

			this.PerClientCropPresetIds ??= new Dictionary<string, string>();
			this.PerClientCropRegions ??= new Dictionary<string, CropRegion>();
			this.PerClientCropPresetIds[currentClient] = preset.Id;
			// Keep the legacy value synchronized so an older personal build can
			// still read the effective per-character crop.
			this.PerClientCropRegions[currentClient] = CloneRegion(preset.Region);
		}

		public void UnassignCropPreset(string currentClient)
		{
			this.PerClientCropPresetIds?.Remove(currentClient);
			this.PerClientCropRegions?.Remove(currentClient);
		}

		public void SetCropRegion(string currentClient, CropRegion cropRegion)
		{
			if (string.IsNullOrWhiteSpace(currentClient) || cropRegion?.IsValid != true)
			{
				return;
			}

			this.PerClientCropRegions ??= new Dictionary<string, CropRegion>();
			this.PerClientCropPresetIds?.Remove(currentClient);
			this.PerClientCropRegions[currentClient] = cropRegion;
		}

		public void RemoveCropRegion(string currentClient)
		{
			this.UnassignCropPreset(currentClient);
		}

		public void SetThumbnailLocation(string currentClient, string activeClient, Point location)
		{
			Dictionary<string, Point> layoutSource;

			if (this.EnablePerClientThumbnailLayouts)
			{
				if (string.IsNullOrEmpty(activeClient))
				{
					return;
				}

				if (!this.PerClientLayout.TryGetValue(activeClient, out layoutSource))
				{
					layoutSource = new Dictionary<string, Point>();
					this.PerClientLayout[activeClient] = layoutSource;
				}
			}
			else
			{
				layoutSource = this.FlatLayout;
			}

			layoutSource[currentClient] = location;
		}

		public ClientLayout GetClientLayout(string currentClient)
		{
			ClientLayout layout;
			this.ClientLayout.TryGetValue(currentClient, out layout);

			return layout;
		}

		public void SetClientLayout(string currentClient, ClientLayout layout)
		{
			this.ClientLayout[currentClient] = layout;
		}

		public Keys GetClientHotkey(string currentClient)
		{
			string hotkey;
			if (this.ClientHotkey.TryGetValue(currentClient, out hotkey))
			{
				// Protect from incorrect values
				object rawValue = (new KeysConverter()).ConvertFromInvariantString(hotkey);
				return rawValue != null ? (Keys)rawValue : Keys.None;
			}

			return Keys.None;
		}

		public void SetClientHotkey(string currentClient, Keys hotkey)
		{
			this.ClientHotkey[currentClient] = (new KeysConverter()).ConvertToInvariantString(hotkey);
		}

		public Keys StringToKey(string hotkey)
		{
			object rawValue = (new KeysConverter()).ConvertFromInvariantString(hotkey);
			return rawValue != null ? (Keys)rawValue : Keys.None;
		}

		public bool IsPriorityClient(string currentClient)
		{
			return this.PriorityClients.Contains(currentClient);
		}
		public bool IsExecutableToPreview(string processName)
		{
			return this.ExecutablesToPreview.Any(s => s.Equals(processName, StringComparison.OrdinalIgnoreCase));
		}

		public bool IsThumbnailDisabled(string currentClient)
		{
			return this.DisableThumbnail.TryGetValue(currentClient, out bool isDisabled) && isDisabled;
		}

		public void ToggleThumbnail(string currentClient, bool isDisabled)
		{
			this.DisableThumbnail[currentClient] = isDisabled;
		}

		/// <summary>
		/// Applies restrictions to different parameters of the config
		/// </summary>
		public void ApplyRestrictions()
		{
			this.PerClientCropRegions ??= new Dictionary<string, CropRegion>();
			this.CropPresets ??= new Dictionary<string, CropPreset>();
			this.PerClientCropPresetIds ??= new Dictionary<string, string>();
			this.PerClientSolarSystems ??= new Dictionary<string, string>();
			this.PerClientSolarSystemObservations ??= new Dictionary<string, SolarSystemObservation>();
			foreach (string invalidClient in this.PerClientSolarSystems
				.Where(entry => string.IsNullOrWhiteSpace(entry.Key) || string.IsNullOrWhiteSpace(entry.Value))
				.Select(entry => entry.Key)
				.ToList())
			{
				this.PerClientSolarSystems.Remove(invalidClient);
			}
			foreach (string invalidClient in this.PerClientSolarSystemObservations
				.Where(entry => string.IsNullOrWhiteSpace(entry.Key) || entry.Value?.IsValid != true)
				.Select(entry => entry.Key)
				.ToList())
			{
				this.PerClientSolarSystemObservations.Remove(invalidClient);
			}
			foreach (string invalidClient in this.PerClientCropRegions
				.Where(entry => entry.Value?.IsValid != true)
				.Select(entry => entry.Key)
				.ToList())
			{
				this.PerClientCropRegions.Remove(invalidClient);
			}

			foreach (string invalidPresetId in this.CropPresets
				.Where(entry => entry.Value?.IsValid != true ||
					!string.Equals(entry.Key, entry.Value.Id, StringComparison.Ordinal))
				.Select(entry => entry.Key)
				.ToList())
			{
				this.CropPresets.Remove(invalidPresetId);
			}

			foreach (string invalidClient in this.PerClientCropPresetIds
				.Where(entry => string.IsNullOrWhiteSpace(entry.Key) || this.GetCropPreset(entry.Value) == null)
				.Select(entry => entry.Key)
				.ToList())
			{
				this.PerClientCropPresetIds.Remove(invalidClient);
			}

			this.ImportLegacyCropRegions();
			foreach (KeyValuePair<string, string> assignment in this.PerClientCropPresetIds)
			{
				CropPreset preset = this.GetCropPreset(assignment.Value);
				if (preset != null)
				{
					this.PerClientCropRegions[assignment.Key] = CloneRegion(preset.Region);
				}
			}

#if LINUX
			this.ThumbnailRefreshPeriod = ThumbnailConfiguration.ApplyRestrictions(this.ThumbnailRefreshPeriod, 10, 1000);
#else
			this.ThumbnailRefreshPeriod = ThumbnailConfiguration.ApplyRestrictions(this.ThumbnailRefreshPeriod, 100, 2000);
#endif
			this.ThumbnailResizeTimeoutPeriod = ThumbnailConfiguration.ApplyRestrictions(this.ThumbnailResizeTimeoutPeriod, 200, 5000);
			this.ThumbnailSize = new Size(ThumbnailConfiguration.ApplyRestrictions(this.ThumbnailSize.Width, this.ThumbnailMinimumSize.Width, this.ThumbnailMaximumSize.Width),
				ThumbnailConfiguration.ApplyRestrictions(this.ThumbnailSize.Height, this.ThumbnailMinimumSize.Height, this.ThumbnailMaximumSize.Height));
			this.ThumbnailOpacity = ThumbnailConfiguration.ApplyRestrictions((int)(this.ThumbnailOpacity * 100.00), 20, 100) / 100.00;
			this.ThumbnailZoomFactor = ThumbnailConfiguration.ApplyRestrictions(this.ThumbnailZoomFactor, 2, 10);
			this.ActiveClientHighlightThickness = ThumbnailConfiguration.ApplyRestrictions(this.ActiveClientHighlightThickness, 1, 6);
		}
		public IList<string> GetAllKnownClients()
		{
			HashSet<string> clients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			AddKeys(clients, this.FlatLayout);
			AddKeys(clients, this.PerClientCropRegions);
			AddKeys(clients, this.PerClientCropPresetIds);
			AddKeys(clients, this.PerClientSolarSystems);
			AddKeys(clients, this.PerClientSolarSystemObservations);
			AddKeys(clients, this.PerClientAliases);
			AddKeys(clients, this.PerClientThumbnailSize);
			AddKeys(clients, this.CycleGroup1ClientsOrder);
			AddKeys(clients, this.CycleGroup2ClientsOrder);
			AddKeys(clients, this.CycleGroup3ClientsOrder);
			AddKeys(clients, this.CycleGroup4ClientsOrder);
			AddKeys(clients, this.CycleGroup5ClientsOrder);
			return clients.OrderBy(client => client, StringComparer.OrdinalIgnoreCase).ToList();
		}

		private List<string> GetClientsAssignedToPreset(string presetId)
		{
			return this.PerClientCropPresetIds?
				.Where(entry => string.Equals(entry.Value, presetId, StringComparison.Ordinal))
				.Select(entry => entry.Key)
				.ToList() ?? new List<string>();
		}

		private void ImportLegacyCropRegions()
		{
			int importedIndex = this.CropPresets.Count + 1;
			foreach (KeyValuePair<string, CropRegion> entry in this.PerClientCropRegions.ToList())
			{
				if (this.PerClientCropPresetIds.ContainsKey(entry.Key) || entry.Value?.IsValid != true)
				{
					continue;
				}

				CropPreset matchingPreset = this.CropPresets.Values.FirstOrDefault(
					preset => preset?.Region?.IsValid == true && RegionsEqual(preset.Region, entry.Value));
				if (matchingPreset == null)
				{
					string name;
					do
					{
						name = $"Imported crop {importedIndex++}";
					}
					while (this.CropPresets.Values.Any(preset =>
						string.Equals(preset?.Name, name, StringComparison.OrdinalIgnoreCase)));

					matchingPreset = CropPreset.Create(name, CloneRegion(entry.Value));
					this.CropPresets[matchingPreset.Id] = matchingPreset;
				}

				this.PerClientCropPresetIds[entry.Key] = matchingPreset.Id;
			}
		}

		private static CropRegion CloneRegion(CropRegion region)
		{
			return new CropRegion(region.X, region.Y, region.Width, region.Height);
		}

		private static bool RegionsEqual(CropRegion first, CropRegion second)
		{
			const double tolerance = 0.000001;
			return Math.Abs(first.X - second.X) <= tolerance &&
				Math.Abs(first.Y - second.Y) <= tolerance &&
				Math.Abs(first.Width - second.Width) <= tolerance &&
				Math.Abs(first.Height - second.Height) <= tolerance;
		}

		private static void AddKeys<TValue>(HashSet<string> clients, IDictionary<string, TValue> source)
		{
			if (source == null)
			{
				return;
			}

			foreach (string client in source.Keys.Where(key => !string.IsNullOrWhiteSpace(key)))
			{
				clients.Add(client);
			}
		}

		private static int ApplyRestrictions(int value, int minimum, int maximum)
		{
			if (value <= minimum)
			{
				return minimum;
			}

			if (value >= maximum)
			{
				return maximum;
			}

			return value;
		}
	}
}
