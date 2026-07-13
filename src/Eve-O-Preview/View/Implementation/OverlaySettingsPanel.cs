using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EveOPreview.Configuration.Implementation;

namespace EveOPreview.View
{
	internal sealed class OverlaySettingsPanel : UserControl
	{
		private readonly CheckBox _showOverlayCheckBox;
		private readonly CheckBox _showFramesCheckBox;
		private readonly CheckBox _highlightActiveClientCheckBox;
		private readonly Button _activeHighlightColorButton;
		private readonly CheckBox _showSolarSystemCheckBox;
		private readonly Button _characterFontButton;
		private readonly Button _characterColorButton;
		private readonly Label _characterFontSummaryLabel;
		private readonly Button _solarSystemFontButton;
		private readonly Button _solarSystemColorButton;
		private readonly Label _solarSystemFontSummaryLabel;
		private readonly Dictionary<ViewZoomAnchor, RadioButton> _characterAnchors;
		private readonly Dictionary<ViewZoomAnchor, RadioButton> _solarSystemAnchors;
		private readonly Dictionary<ViewZoomAnchor, RadioButton> _cycleIndicatorAnchors;
		private Font _characterFont;
		private Font _solarSystemFont;
		private Color _activeHighlightColor;
		private Color _characterColor;
		private Color _solarSystemColor;
		private bool _loading;

		public OverlaySettingsPanel()
		{
			this.Name = "OverlaySettingsPanel";
			this.Dock = DockStyle.Fill;
			this.AutoScroll = true;
			this.Padding = new Padding(10);

			this._characterAnchors = new Dictionary<ViewZoomAnchor, RadioButton>();
			this._solarSystemAnchors = new Dictionary<ViewZoomAnchor, RadioButton>();
			this._cycleIndicatorAnchors = new Dictionary<ViewZoomAnchor, RadioButton>();
			this._characterFont = new Font(FontFamily.GenericSansSerif, 10.0f, FontStyle.Bold);
			this._solarSystemFont = new Font(FontFamily.GenericSansSerif, 10.0f, FontStyle.Bold);
			this._activeHighlightColor = Color.GreenYellow;
			this._characterColor = Color.Orange;
			this._solarSystemColor = Color.WhiteSmoke;

			TableLayoutPanel root = new TableLayoutPanel
			{
				Name = "OverlaySettingsLayout",
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				ColumnCount = 2,
				RowCount = 3,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50.0f));
			root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50.0f));

			GroupBox generalGroup = new GroupBox
			{
				Name = "OverlayGeneralGroup",
				Text = "General",
				Dock = DockStyle.Fill,
				AutoSize = true,
				Padding = new Padding(9),
				Margin = new Padding(0, 0, 0, 8)
			};
			FlowLayoutPanel generalFlow = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoSize = true,
				WrapContents = true,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			this._showOverlayCheckBox = CreateCheckBox("ShowThumbnailOverlaysCheckBox", "Show overlay");
			this._showFramesCheckBox = CreateCheckBox("ShowThumbnailFramesCheckBox", "Show frames");
			this._highlightActiveClientCheckBox = CreateCheckBox("EnableActiveClientHighlightCheckBox", "Highlight active client");
			this._activeHighlightColorButton = CreateColorButton("ActiveClientHighlightColorButton", "Highlight color…", this._activeHighlightColor);
			generalFlow.Controls.Add(this._showOverlayCheckBox);
			generalFlow.Controls.Add(this._showFramesCheckBox);
			generalFlow.Controls.Add(this._highlightActiveClientCheckBox);
			generalFlow.Controls.Add(this._activeHighlightColorButton);
			generalGroup.Controls.Add(generalFlow);
			root.Controls.Add(generalGroup, 0, 0);
			root.SetColumnSpan(generalGroup, 2);

			GroupBox characterGroup = new GroupBox
			{
				Name = "CharacterLabelGroup",
				Text = "Character name",
				Dock = DockStyle.Fill,
				AutoSize = true,
				Padding = new Padding(9),
				Margin = new Padding(0, 0, 5, 8)
			};
			TableLayoutPanel characterLayout = CreateLabelSettingsLayout();
			this._characterFontButton = CreateActionButton("CharacterFontButton", "Choose font…");
			this._characterFontSummaryLabel = CreateFontSummaryLabel("CharacterFontSummaryLabel");
			this._characterColorButton = CreateColorButton("CharacterColorButton", "", this._characterColor);
			AddLabelSettingsRows(
				characterLayout,
				this._characterFontButton,
				this._characterFontSummaryLabel,
				this._characterColorButton,
				CreateAnchorPicker(this._characterAnchors, "CharacterAnchor"));
			characterGroup.Controls.Add(characterLayout);
			root.Controls.Add(characterGroup, 0, 1);

			GroupBox solarSystemGroup = new GroupBox
			{
				Name = "SolarSystemLabelGroup",
				Text = "Solar system",
				Dock = DockStyle.Fill,
				AutoSize = true,
				Padding = new Padding(9),
				Margin = new Padding(5, 0, 0, 8)
			};
			TableLayoutPanel solarLayout = CreateLabelSettingsLayout();
			this._showSolarSystemCheckBox = CreateCheckBox("ShowSolarSystemOverlayCheckBox", "Show solar system");
			solarLayout.Controls.Add(this._showSolarSystemCheckBox, 0, 0);
			solarLayout.SetColumnSpan(this._showSolarSystemCheckBox, 2);
			this._solarSystemFontButton = CreateActionButton("SolarSystemFontButton", "Choose font…");
			this._solarSystemFontSummaryLabel = CreateFontSummaryLabel("SolarSystemFontSummaryLabel");
			this._solarSystemColorButton = CreateColorButton("SolarSystemColorButton", "", this._solarSystemColor);
			AddLabelSettingsRows(
				solarLayout,
				this._solarSystemFontButton,
				this._solarSystemFontSummaryLabel,
				this._solarSystemColorButton,
				CreateAnchorPicker(this._solarSystemAnchors, "SolarSystemAnchor"),
				1);
			Label helpLabel = new Label
			{
				Name = "SolarSystemOverlayHelpLabel",
				Text = "Requires EVE's ‘Log Chat to File’. Reads Local chat logs; cached systems may be stale.",
				AutoSize = true,
				MaximumSize = new Size(330, 0),
				ForeColor = SystemColors.GrayText,
				Margin = new Padding(3, 6, 3, 0)
			};
			solarLayout.Controls.Add(helpLabel, 0, 4);
			solarLayout.SetColumnSpan(helpLabel, 2);
			solarSystemGroup.Controls.Add(solarLayout);
			root.Controls.Add(solarSystemGroup, 1, 1);

			GroupBox cycleIndicatorGroup = new GroupBox
			{
				Name = "CycleIndicatorGroup",
				Text = "Cycle group indicator",
				Dock = DockStyle.Fill,
				AutoSize = true,
				Padding = new Padding(9),
				Margin = new Padding(0, 0, 5, 0)
			};
			TableLayoutPanel cycleLayout = new TableLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				ColumnCount = 2,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			cycleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			cycleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
			Label cyclePositionLabel = CreateSettingLabel("CycleIndicatorPositionLabel", "Position");
			cycleLayout.Controls.Add(cyclePositionLabel, 0, 0);
			cycleLayout.Controls.Add(CreateAnchorPicker(this._cycleIndicatorAnchors, "CycleIndicatorAnchor"), 1, 0);
			cycleIndicatorGroup.Controls.Add(cycleLayout);
			root.Controls.Add(cycleIndicatorGroup, 0, 2);

			this.Controls.Add(root);
			this.WireEvents();
			SetAnchor(this._characterAnchors, ViewZoomAnchor.NW);
			SetAnchor(this._solarSystemAnchors, ViewZoomAnchor.S);
			SetAnchor(this._cycleIndicatorAnchors, ViewZoomAnchor.NW);
			this.UpdateFontSummaries();
		}

		public Action ContentsChanged { get; set; }

		public void ApplyLocalization()
		{
			LocalizationExtensions.ApplyLocalization(
				this,
				"MainForm.ContentTabControl.OverlayTabPage");
		}

		public bool ShowThumbnailOverlays
		{
			get => this._showOverlayCheckBox.Checked;
			set => this.SetLoadingValue(() => this._showOverlayCheckBox.Checked = value);
		}

		public bool ShowThumbnailFrames
		{
			get => this._showFramesCheckBox.Checked;
			set => this.SetLoadingValue(() => this._showFramesCheckBox.Checked = value);
		}

		public bool EnableActiveClientHighlight
		{
			get => this._highlightActiveClientCheckBox.Checked;
			set => this.SetLoadingValue(() => this._highlightActiveClientCheckBox.Checked = value);
		}

		public bool ShowSolarSystemOverlay
		{
			get => this._showSolarSystemCheckBox.Checked;
			set => this.SetLoadingValue(() => this._showSolarSystemCheckBox.Checked = value);
		}

		public Color ActiveClientHighlightColor
		{
			get => this._activeHighlightColor;
			set
			{
				this._activeHighlightColor = value;
				this._activeHighlightColorButton.BackColor = value;
			}
		}

		public Color CharacterLabelColor
		{
			get => this._characterColor;
			set
			{
				this._characterColor = value;
				this._characterColorButton.BackColor = value;
			}
		}

		public Color SolarSystemLabelColor
		{
			get => this._solarSystemColor;
			set
			{
				this._solarSystemColor = value;
				this._solarSystemColorButton.BackColor = value;
			}
		}

		public Font CharacterLabelFont
		{
			get => this._characterFont;
			set
			{
				this._characterFont = value ?? new Font(FontFamily.GenericSansSerif, 10.0f, FontStyle.Bold);
				this.UpdateFontSummaries();
			}
		}

		public Font SolarSystemLabelFont
		{
			get => this._solarSystemFont;
			set
			{
				this._solarSystemFont = value ?? new Font(FontFamily.GenericSansSerif, 10.0f, FontStyle.Bold);
				this.UpdateFontSummaries();
			}
		}

		public ViewZoomAnchor CharacterLabelAnchor
		{
			get => GetAnchor(this._characterAnchors, ViewZoomAnchor.NW);
			set => this.SetLoadingValue(() => SetAnchor(this._characterAnchors, value));
		}

		public ViewZoomAnchor SolarSystemLabelAnchor
		{
			get => GetAnchor(this._solarSystemAnchors, ViewZoomAnchor.S);
			set => this.SetLoadingValue(() => SetAnchor(this._solarSystemAnchors, value));
		}

		public ViewZoomAnchor CycleGroupIndicatorAnchor
		{
			get => GetAnchor(this._cycleIndicatorAnchors, ViewZoomAnchor.NW);
			set => this.SetLoadingValue(() => SetAnchor(this._cycleIndicatorAnchors, value));
		}

		private void WireEvents()
		{
			this._showOverlayCheckBox.CheckedChanged += this.ControlChanged;
			this._showFramesCheckBox.CheckedChanged += this.ControlChanged;
			this._highlightActiveClientCheckBox.CheckedChanged += this.ControlChanged;
			this._showSolarSystemCheckBox.CheckedChanged += this.ControlChanged;
			foreach (RadioButton radio in this._characterAnchors.Values
				.Concat(this._solarSystemAnchors.Values)
				.Concat(this._cycleIndicatorAnchors.Values))
			{
				radio.CheckedChanged += this.ControlChanged;
			}

			this._activeHighlightColorButton.Click += (sender, args) =>
				this.ChooseColor(this._activeHighlightColor, color => this.ActiveClientHighlightColor = color);
			this._characterColorButton.Click += (sender, args) =>
				this.ChooseColor(this._characterColor, color => this.CharacterLabelColor = color);
			this._solarSystemColorButton.Click += (sender, args) =>
				this.ChooseColor(this._solarSystemColor, color => this.SolarSystemLabelColor = color);
			this._characterFontButton.Click += (sender, args) =>
				this.ChooseFont(this._characterFont, font => this.CharacterLabelFont = font);
			this._solarSystemFontButton.Click += (sender, args) =>
				this.ChooseFont(this._solarSystemFont, font => this.SolarSystemLabelFont = font);
		}

		private void ChooseColor(Color current, Action<Color> apply)
		{
			using ColorDialog dialog = new ColorDialog { Color = current, FullOpen = true };
			if (dialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}
			apply(dialog.Color);
			this.NotifyChanged();
		}

		private void ChooseFont(Font current, Action<Font> apply)
		{
			using FontDialog dialog = new FontDialog
			{
				Font = current,
				ShowApply = false,
				ShowColor = false,
				ShowHelp = false
			};
			if (dialog.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}
			apply((Font)dialog.Font.Clone());
			this.NotifyChanged();
		}

		private void ControlChanged(object sender, EventArgs e)
		{
			if (sender is RadioButton radio && !radio.Checked)
			{
				return;
			}
			this.NotifyChanged();
		}

		private void NotifyChanged()
		{
			if (!this._loading)
			{
				this.ContentsChanged?.Invoke();
			}
		}

		private void SetLoadingValue(Action action)
		{
			bool wasLoading = this._loading;
			this._loading = true;
			try
			{
				action();
			}
			finally
			{
				this._loading = wasLoading;
			}
		}

		private void UpdateFontSummaries()
		{
			if (this._characterFontSummaryLabel != null)
			{
				this._characterFontSummaryLabel.Text = DescribeFont(this._characterFont);
			}
			if (this._solarSystemFontSummaryLabel != null)
			{
				this._solarSystemFontSummaryLabel.Text = DescribeFont(this._solarSystemFont);
			}
		}

		private static string DescribeFont(Font font)
		{
			string style = font.Style == FontStyle.Regular ? string.Empty : $", {font.Style}";
			return $"{font.Name}, {font.SizeInPoints:0.#} pt{style}";
		}

		private static TableLayoutPanel CreateLabelSettingsLayout()
		{
			TableLayoutPanel layout = new TableLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				ColumnCount = 2,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
			return layout;
		}

		private static void AddLabelSettingsRows(
			TableLayoutPanel layout,
			Button fontButton,
			Label fontSummary,
			Button colorButton,
			Control anchorPicker,
			int startingRow = 0)
		{
			Label fontLabel = CreateSettingLabel("FontLabel", "Font");
			FlowLayoutPanel fontFlow = new FlowLayoutPanel
			{
				AutoSize = true,
				Dock = DockStyle.Fill,
				WrapContents = true,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			fontFlow.Controls.Add(fontButton);
			fontFlow.Controls.Add(fontSummary);
			layout.Controls.Add(fontLabel, 0, startingRow);
			layout.Controls.Add(fontFlow, 1, startingRow);

			layout.Controls.Add(CreateSettingLabel("ColorLabel", "Color"), 0, startingRow + 1);
			layout.Controls.Add(colorButton, 1, startingRow + 1);
			layout.Controls.Add(CreateSettingLabel("PositionLabel", "Position"), 0, startingRow + 2);
			layout.Controls.Add(anchorPicker, 1, startingRow + 2);
		}

		private static CheckBox CreateCheckBox(string name, string text)
		{
			return new CheckBox
			{
				Name = name,
				Text = text,
				AutoSize = true,
				UseVisualStyleBackColor = true,
				Margin = new Padding(3, 5, 12, 5)
			};
		}

		private static Button CreateActionButton(string name, string text)
		{
			return new Button
			{
				Name = name,
				Text = text,
				AutoSize = true,
				MinimumSize = new Size(92, 28),
				UseVisualStyleBackColor = true,
				Margin = new Padding(3)
			};
		}

		private static Button CreateColorButton(string name, string text, Color color)
		{
			return new Button
			{
				Name = name,
				Text = text,
				BackColor = color,
				AutoSize = true,
				MinimumSize = new Size(74, 28),
				FlatStyle = FlatStyle.Flat,
				UseVisualStyleBackColor = false,
				Margin = new Padding(3)
			};
		}

		private static Label CreateFontSummaryLabel(string name)
		{
			return new Label
			{
				Name = name,
				AutoSize = true,
				AutoEllipsis = true,
				Margin = new Padding(3, 8, 3, 3)
			};
		}

		private static Label CreateSettingLabel(string name, string text)
		{
			return new Label
			{
				Name = name,
				Text = text,
				AutoSize = true,
				TextAlign = ContentAlignment.MiddleLeft,
				Margin = new Padding(3, 8, 8, 3)
			};
		}

		private static TableLayoutPanel CreateAnchorPicker(
			IDictionary<ViewZoomAnchor, RadioButton> map,
			string namePrefix)
		{
			ViewZoomAnchor[,] anchors =
			{
				{ ViewZoomAnchor.NW, ViewZoomAnchor.N, ViewZoomAnchor.NE },
				{ ViewZoomAnchor.W, ViewZoomAnchor.C, ViewZoomAnchor.E },
				{ ViewZoomAnchor.SW, ViewZoomAnchor.S, ViewZoomAnchor.SE }
			};
			TableLayoutPanel picker = new TableLayoutPanel
			{
				Name = $"{namePrefix}Picker",
				AutoSize = true,
				ColumnCount = 3,
				RowCount = 3,
				CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
				Margin = new Padding(3),
				Padding = Padding.Empty
			};
			ToolTip toolTip = new ToolTip();
			for (int row = 0; row < 3; row++)
			{
				for (int column = 0; column < 3; column++)
				{
					ViewZoomAnchor anchor = anchors[row, column];
					RadioButton radio = new RadioButton
					{
						Name = $"{namePrefix}{anchor}RadioButton",
						AutoSize = true,
						Margin = new Padding(6),
						TabStop = true,
						UseVisualStyleBackColor = true
					};
					toolTip.SetToolTip(radio, AnchorDescription(anchor));
					map[anchor] = radio;
					picker.Controls.Add(radio, column, row);
				}
			}
			return picker;
		}

		private static string AnchorDescription(ViewZoomAnchor anchor)
		{
			return anchor switch
			{
				ViewZoomAnchor.NW => "Top left",
				ViewZoomAnchor.N => "Top center",
				ViewZoomAnchor.NE => "Top right",
				ViewZoomAnchor.W => "Middle left",
				ViewZoomAnchor.C => "Center",
				ViewZoomAnchor.E => "Middle right",
				ViewZoomAnchor.SW => "Bottom left",
				ViewZoomAnchor.S => "Bottom center",
				ViewZoomAnchor.SE => "Bottom right",
				_ => anchor.ToString()
			};
		}

		private static ViewZoomAnchor GetAnchor(
			IReadOnlyDictionary<ViewZoomAnchor, RadioButton> map,
			ViewZoomAnchor fallback)
		{
			foreach (KeyValuePair<ViewZoomAnchor, RadioButton> entry in map)
			{
				if (entry.Value.Checked)
				{
					return entry.Key;
				}
			}
			return fallback;
		}

		private static void SetAnchor(
			IReadOnlyDictionary<ViewZoomAnchor, RadioButton> map,
			ViewZoomAnchor anchor)
		{
			if (!map.TryGetValue(anchor, out RadioButton radio))
			{
				radio = map.Values.First();
			}
			radio.Checked = true;
		}
	}
}
