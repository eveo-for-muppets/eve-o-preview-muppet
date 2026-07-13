using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EveOPreview.Configuration;
using EveOPreview.Configuration.Implementation;

namespace EveOPreview.View
{
	sealed class CropPresetsPanel : UserControl
	{
		private sealed class PresetItem
		{
			public string Id { get; init; }
			public string Name { get; init; }
			public override string ToString() => this.Name;
		}

		private sealed class ClientRow
		{
			public string Title { get; init; }
			public string CurrentPreset { get; init; }
			public bool IsOpen { get; init; }
			public bool OriginalIsChecked { get; init; }
			public bool IsChecked { get; set; }
		}

		private readonly ComboBox _presetCombo;
		private readonly Button _renameButton;
		private readonly Button _deleteButton;
		private readonly Label _regionSummary;
		private readonly ComboBox _sourceClientCombo;
		private readonly TextBox _filterBox;
		private readonly ListView _clientsList;
		private readonly Label _selectionSummary;
		private readonly Button _applyButton;
		private readonly List<ClientRow> _clientRows;
		private readonly Dictionary<Control, string> _localizedControls = new Dictionary<Control, string>();
		private CropRegion _currentRegion;
		private bool _suppressEvents;
		private bool _assignmentsDirty;
		private string _previousPresetId;

		public CropPresetsPanel()
		{
			this._clientRows = new List<ClientRow>();
			this.Dock = DockStyle.Fill;
			this.Padding = new Padding(4);

			TableLayoutPanel root = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				ColumnCount = 1,
				RowCount = 5,
				Padding = new Padding(0)
			};
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

			FlowLayoutPanel presetRow = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				WrapContents = true,
				Padding = new Padding(0, 0, 0, 4)
			};
			presetRow.Controls.Add(this.CreateLabel("PresetLabel", "Preset", new Padding(0, 7, 6, 0)));
			this._presetCombo = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Width = 128,
				Margin = new Padding(0, 2, 2, 2)
			};
			this._presetCombo.SelectedIndexChanged += this.PresetCombo_SelectedIndexChanged;
			presetRow.Controls.Add(this._presetCombo);

			Button newButton = this.CreateButton("NewButton", "New...");
			newButton.Click += this.NewButton_Click;
			this._renameButton = this.CreateButton("RenameButton", "Rename");
			this._renameButton.Click += this.RenameButton_Click;
			this._deleteButton = this.CreateButton("DeleteButton", "Delete");
			this._deleteButton.Click += this.DeleteButton_Click;
			presetRow.Controls.Add(newButton);
			presetRow.Controls.Add(this._renameButton);
			presetRow.Controls.Add(this._deleteButton);

			TableLayoutPanel regionPanel = new TableLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				ColumnCount = 3,
				RowCount = 2,
				Padding = new Padding(0, 0, 0, 4)
			};
			regionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			regionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
			regionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			regionPanel.Controls.Add(this.CreateLabel("RegionLabel", "Region", new Padding(0, 6, 8, 0)), 0, 0);
			this._regionSummary = new Label
			{
				Text = "No preset selected",
				AutoEllipsis = true,
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleLeft
			};
			regionPanel.Controls.Add(this._regionSummary, 1, 0);
			regionPanel.SetColumnSpan(this._regionSummary, 2);
			regionPanel.Controls.Add(this.CreateLabel("CaptureFromLabel", "Capture from", new Padding(0, 7, 8, 0)), 0, 1);
			this._sourceClientCombo = new ComboBox
			{
				Dock = DockStyle.Fill,
				DropDownStyle = ComboBoxStyle.DropDownList,
				Margin = new Padding(0, 2, 6, 2)
			};
			regionPanel.Controls.Add(this._sourceClientCombo, 1, 1);
			Button selectAreaButton = this.CreateButton("SelectAreaButton", "Select area...");
			selectAreaButton.AutoSize = true;
			selectAreaButton.Click += this.SelectAreaButton_Click;
			regionPanel.Controls.Add(selectAreaButton, 2, 1);

			TableLayoutPanel toolsPanel = new TableLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				ColumnCount = 2,
				RowCount = 2,
				Padding = new Padding(0, 2, 0, 4)
			};
			toolsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			toolsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
			toolsPanel.Controls.Add(this.CreateLabel("FilterLabel", "Filter", new Padding(0, 7, 8, 0)), 0, 0);
			this._filterBox = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 2) };
			this._filterBox.TextChanged += (sender, args) => this.RebuildClientList();
			toolsPanel.Controls.Add(this._filterBox, 1, 0);

			TableLayoutPanel bulkButtons = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoSize = true,
				ColumnCount = 4,
				RowCount = 1,
				Margin = new Padding(0)
			};
			for (int column = 0; column < bulkButtons.ColumnCount; column++)
			{
				bulkButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25.0f));
			}
			Button selectVisibleButton = this.CreateToolbarButton("AllVisibleButton", "All visible");
			selectVisibleButton.Click += (sender, args) => this.SetVisibleChecks(true);
			Button clearVisibleButton = this.CreateToolbarButton("ClearVisibleButton", "Clear visible");
			clearVisibleButton.Click += (sender, args) => this.SetVisibleChecks(false);
			Button invertVisibleButton = this.CreateToolbarButton("InvertButton", "Invert");
			invertVisibleButton.Click += (sender, args) => this.InvertVisibleChecks();
			Button selectOpenButton = this.CreateToolbarButton("OpenOnlyButton", "Open only");
			selectOpenButton.Click += (sender, args) => this.SelectOpenClients();
			bulkButtons.Controls.Add(selectVisibleButton, 0, 0);
			bulkButtons.Controls.Add(clearVisibleButton, 1, 0);
			bulkButtons.Controls.Add(invertVisibleButton, 2, 0);
			bulkButtons.Controls.Add(selectOpenButton, 3, 0);
			toolsPanel.Controls.Add(bulkButtons, 0, 1);
			toolsPanel.SetColumnSpan(bulkButtons, 2);

			this._clientsList = new ListView
			{
				Dock = DockStyle.Fill,
				View = System.Windows.Forms.View.Details,
				CheckBoxes = true,
				FullRowSelect = true,
				HideSelection = false,
				MultiSelect = true,
				MinimumSize = new Size(0, 100)
			};
			this._clientsList.Columns.Add("Character", 160);
			this._clientsList.Columns.Add("Current crop", 105);
			this._clientsList.Columns.Add("Status", 55);
			this._clientsList.ItemChecked += this.ClientsList_ItemChecked;
			this._clientsList.Resize += (sender, args) => this.ResizeClientColumns();
			this._clientsList.MouseEnter += (sender, args) =>
			{
				if (!this._clientsList.Focused)
				{
					this._clientsList.Focus();
				}
			};

			TableLayoutPanel footer = new TableLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				ColumnCount = 2,
				Padding = new Padding(0, 2, 0, 0)
			};
			footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
			footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			this._selectionSummary = new Label { AutoSize = true, Text = "0 characters", Margin = new Padding(0, 7, 0, 0) };
			this._applyButton = this.CreateButton("ApplyAssignmentsButton", "Apply assignments");
			this._applyButton.AutoSize = true;
			this._applyButton.Enabled = false;
			this._applyButton.Click += this.ApplyButton_Click;
			footer.Controls.Add(this._selectionSummary, 0, 0);
			footer.Controls.Add(this._applyButton, 1, 0);

			root.Controls.Add(presetRow, 0, 0);
			root.Controls.Add(regionPanel, 0, 1);
			root.Controls.Add(toolsPanel, 0, 2);
			root.Controls.Add(this._clientsList, 0, 3);
			root.Controls.Add(footer, 0, 4);
			this.Controls.Add(root);
			this.Resize += (sender, args) => this.ResizeClientColumns();
			this.UpdatePresetControls();
			this.ResizeClientColumns();
		}

		public void ApplyLocalization()
		{
			foreach (KeyValuePair<Control, string> entry in this._localizedControls)
			{
				entry.Key.Text = this.Localize(entry.Key.Name, entry.Value);
			}
			this._clientsList.Columns[0].Text = this.Localize("CharacterColumn", "Character");
			this._clientsList.Columns[1].Text = this.Localize("CurrentCropColumn", "Current crop");
			this._clientsList.Columns[2].Text = this.Localize("StatusColumn", "Status");
			this.UpdateRegionSummary();
			this.RebuildClientList();
			this.ResizeClientColumns();
		}

		public Action<string> PresetSelected { get; set; }
		public Action<string, string> CreatePresetRequested { get; set; }
		public Action<string, string> RenamePresetRequested { get; set; }
		public Action<string> DeletePresetRequested { get; set; }
		public Action<string, string> SelectAreaRequested { get; set; }
		public Func<string, IList<string>, bool> ApplyAssignmentsRequested { get; set; }
		public Action<int, bool> SelectCycleGroupRequested { get; set; }
		public Func<string, int, bool, bool, bool> AssignCycleGroupRequested { get; set; }

		public string SelectedPresetId => (this._presetCombo.SelectedItem as PresetItem)?.Id;
		public bool HasPendingAssignments => this._assignmentsDirty;
		public bool ResolvePendingAssignments() => this.CommitOrDiscardPendingAssignments(
			this.Localize("NextActionContinuing", "continuing"));

		public void SetPresets(IList<CropPreset> presets, string selectedPresetId)
		{
			this._suppressEvents = true;
			this._presetCombo.Items.Clear();
			foreach (CropPreset preset in (presets ?? new List<CropPreset>())
				.Where(preset => preset?.IsValid == true)
				.OrderBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase))
			{
				this._presetCombo.Items.Add(new PresetItem { Id = preset.Id, Name = preset.Name });
			}

			int selectedIndex = -1;
			for (int index = 0; index < this._presetCombo.Items.Count; index++)
			{
				if (string.Equals((this._presetCombo.Items[index] as PresetItem)?.Id, selectedPresetId, StringComparison.Ordinal))
				{
					selectedIndex = index;
					break;
				}
			}
			this._presetCombo.SelectedIndex = selectedIndex >= 0
				? selectedIndex
				: (this._presetCombo.Items.Count > 0 ? 0 : -1);
			this._previousPresetId = this.SelectedPresetId;
			this._assignmentsDirty = false;
			this._suppressEvents = false;
			this.UpdatePresetControls();
		}

		public void SetRegion(CropRegion region)
		{
			this._currentRegion = region;
			this.UpdateRegionSummary();
		}

		private void UpdateRegionSummary()
		{
			this._regionSummary.Text = this._currentRegion?.IsValid == true
				? string.Format(
					this.Localize("RegionSummaryFormat", "X {0:P1}   Y {1:P1}   Width {2:P1}   Height {3:P1}"),
					this._currentRegion.X,
					this._currentRegion.Y,
					this._currentRegion.Width,
					this._currentRegion.Height)
				: this.Localize("NoPresetSelected", "No preset selected");
		}

		public void SetSourceClients(IList<string> clients)
		{
			string previous = this._sourceClientCombo.SelectedItem?.ToString();
			this._sourceClientCombo.Items.Clear();
			foreach (string client in (clients ?? new List<string>()).OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
			{
				this._sourceClientCombo.Items.Add(client);
			}
			int previousIndex = previous == null ? -1 : this._sourceClientCombo.Items.IndexOf(previous);
			this._sourceClientCombo.SelectedIndex = previousIndex >= 0
				? previousIndex
				: (this._sourceClientCombo.Items.Count > 0 ? 0 : -1);
		}

		public void SetClients(
			IList<string> clients,
			IDictionary<string, string> currentPresetNames,
			ISet<string> openClients,
			ISet<string> checkedClients)
		{
			this._clientRows.Clear();
			foreach (string client in (clients ?? new List<string>()).OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
			{
				string currentPreset = null;
				currentPresetNames?.TryGetValue(client, out currentPreset);
				this._clientRows.Add(new ClientRow
				{
					Title = client,
					CurrentPreset = currentPreset,
					IsOpen = openClients?.Contains(client) == true,
					OriginalIsChecked = checkedClients?.Contains(client) == true,
					IsChecked = checkedClients?.Contains(client) == true
				});
			}
			this._assignmentsDirty = false;
			this.RebuildClientList();
		}

		public void SelectClients(IList<string> clients, bool onlyOpen)
		{
			HashSet<string> selected = new HashSet<string>(clients ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
			bool changed = false;
			foreach (ClientRow row in this._clientRows)
			{
				if (!row.IsChecked && selected.Contains(row.Title) && (!onlyOpen || row.IsOpen))
				{
					row.IsChecked = true;
					changed = true;
				}
			}
			if (changed)
			{
				this.MarkAssignmentsDirty();
			}
			this.RebuildClientList();
		}

		private Button CreateButton(string name, string text)
		{
			Button button = new Button
			{
				Name = name,
				Text = text,
				AutoSize = true,
				Height = 30,
				Margin = new Padding(2)
			};
			this._localizedControls[button] = text;
			return button;
		}

		private Label CreateLabel(string name, string text, Padding margin)
		{
			Label label = new Label { Name = name, Text = text, AutoSize = true, Margin = margin };
			this._localizedControls[label] = text;
			return label;
		}

		private void ResizeClientColumns()
		{
			if (this._clientsList?.Columns.Count < 3)
			{
				return;
			}
			int statusWidth = Math.Max(62, new[]
			{
				this._clientsList.Columns[2].Text,
				this.Localize("StatusOpen", "Open"),
				this.Localize("StatusOffline", "Offline")
			}.Max(text => TextRenderer.MeasureText(text, this._clientsList.Font).Width) + 18);
			int available = Math.Max(220, this._clientsList.ClientSize.Width - statusWidth - 8);
			this._clientsList.Columns[0].Width = Math.Max(130, (int)(available * 0.58));
			this._clientsList.Columns[1].Width = Math.Max(90, available - this._clientsList.Columns[0].Width);
			this._clientsList.Columns[2].Width = statusWidth;
		}

		private Button CreateToolbarButton(string name, string text)
		{
			Button button = new Button
			{
				Name = name,
				Text = text,
				Dock = DockStyle.Fill,
				AutoSize = false,
				Height = 30,
				Margin = new Padding(2)
			};
			this._localizedControls[button] = text;
			return button;
		}

		private void RebuildClientList()
		{
			string filter = this._filterBox.Text?.Trim() ?? string.Empty;
			this._suppressEvents = true;
			this._clientsList.BeginUpdate();
			this._clientsList.Items.Clear();
			foreach (ClientRow row in this._clientRows.Where(row =>
				string.IsNullOrEmpty(filter) || row.Title.Contains(filter, StringComparison.OrdinalIgnoreCase)))
			{
				ListViewItem item = new ListViewItem(row.Title)
				{
					Tag = row,
					Checked = row.IsChecked
				};
				item.SubItems.Add(string.IsNullOrWhiteSpace(row.CurrentPreset)
					? this.Localize("FullWindow", "Full window")
					: row.CurrentPreset);
				item.SubItems.Add(row.IsOpen
					? this.Localize("StatusOpen", "Open")
					: this.Localize("StatusOffline", "Offline"));
				this._clientsList.Items.Add(item);
			}
			this._clientsList.EndUpdate();
			this._suppressEvents = false;
			this.UpdateSelectionSummary();
		}

		private void ClientsList_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			if (this._suppressEvents || e.Item.Tag is not ClientRow row)
			{
				return;
			}
			// WinForms can deliver ItemChecked after a programmatic list rebuild.
			// If the model already has this value, nothing was changed by the user.
			if (row.IsChecked == e.Item.Checked)
			{
				return;
			}
			row.IsChecked = e.Item.Checked;
			this.MarkAssignmentsDirty();
			this.BeginInvoke((Action)this.UpdateSelectionSummary);
		}

		private void SetVisibleChecks(bool isChecked)
		{
			bool changed = false;
			this._suppressEvents = true;
			foreach (ListViewItem item in this._clientsList.Items)
			{
				if (item.Tag is ClientRow row)
				{
					changed |= row.IsChecked != isChecked;
					row.IsChecked = isChecked;
					item.Checked = isChecked;
				}
			}
			this._suppressEvents = false;
			if (changed)
			{
				this.MarkAssignmentsDirty();
			}
			this.UpdateSelectionSummary();
		}

		private void InvertVisibleChecks()
		{
			bool changed = this._clientsList.Items.Count > 0;
			this._suppressEvents = true;
			foreach (ListViewItem item in this._clientsList.Items)
			{
				if (item.Tag is ClientRow row)
				{
					row.IsChecked = !row.IsChecked;
					item.Checked = row.IsChecked;
				}
			}
			this._suppressEvents = false;
			if (changed)
			{
				this.MarkAssignmentsDirty();
			}
			this.UpdateSelectionSummary();
		}

		private void SelectOpenClients()
		{
			bool changed = false;
			foreach (ClientRow row in this._clientRows)
			{
				changed |= row.IsChecked != row.IsOpen;
				row.IsChecked = row.IsOpen;
			}
			if (changed)
			{
				this.MarkAssignmentsDirty();
			}
			this.RebuildClientList();
		}

		private void MarkAssignmentsDirty()
		{
			this._assignmentsDirty = this._clientRows.Any(row => row.IsChecked != row.OriginalIsChecked);
			this._applyButton.Enabled = this._assignmentsDirty && !string.IsNullOrWhiteSpace(this.SelectedPresetId);
		}

		private void UpdateSelectionSummary()
		{
			int selected = this._clientRows.Count(row => row.IsChecked);
			int visibleSelected = this._clientsList.Items
				.Cast<ListViewItem>()
				.Count(item => item.Tag is ClientRow row && row.IsChecked);
			int hiddenSelected = selected - visibleSelected;
			string summary = hiddenSelected > 0
				? string.Format(
					this.Localize("SelectionHiddenFormat", "Selected: {0} of {1} ({2} hidden by filter)"),
					selected,
					this._clientRows.Count,
					hiddenSelected)
				: string.Format(
					this.Localize("SelectionFormat", "Selected: {0} of {1}"),
					selected,
					this._clientRows.Count);
			this._selectionSummary.Text = this._assignmentsDirty
				? string.Format(this.Localize("ChangesNotAppliedFormat", "{0} — changes not applied"), summary)
				: summary;
		}

		private IList<string> GetCheckedClients()
		{
			return this._clientRows.Where(row => row.IsChecked).Select(row => row.Title).ToList();
		}

		private bool ApplyAssignments(string presetId)
		{
			if (string.IsNullOrWhiteSpace(presetId) || this.ApplyAssignmentsRequested == null)
			{
				return false;
			}
			bool applied = this.ApplyAssignmentsRequested(presetId, this.GetCheckedClients());
			if (applied)
			{
				this._assignmentsDirty = false;
				this._applyButton.Enabled = false;
			}
			return applied;
		}

		private void ApplyButton_Click(object sender, EventArgs e)
		{
			this.ApplyAssignments(this.SelectedPresetId);
		}

		private void PresetCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (this._suppressEvents)
			{
				return;
			}

			string newPresetId = this.SelectedPresetId;
			this.MarkAssignmentsDirty();
			if (this._assignmentsDirty && !string.IsNullOrWhiteSpace(this._previousPresetId))
			{
				string previousPresetName = this._presetCombo.Items
					.Cast<PresetItem>()
					.FirstOrDefault(preset => string.Equals(preset.Id, this._previousPresetId, StringComparison.Ordinal))?.Name
					?? this.Localize("PreviousPreset", "the previous preset");
				DialogResult result = MessageBox.Show(
					this.GetDialogOwner(),
					string.Format(
						this.Localize(
							"SwitchPresetUnsavedMessageFormat",
							"You changed which characters use ‘{0}’, but have not clicked Apply assignments.\n\nApply those changes before switching presets?\n\nYes = apply changes\nNo = discard changes\nCancel = keep editing"),
						previousPresetName),
					this.Localize("UnsavedAssignmentsTitle", "Unsaved crop assignments"),
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Question);
				if (result == DialogResult.Cancel)
				{
					this.RestorePresetSelection(this._previousPresetId);
					return;
				}
				if (result == DialogResult.Yes)
				{
					if (!this.ApplyAssignments(this._previousPresetId))
					{
						this.RestorePresetSelection(this._previousPresetId);
						return;
					}
					// Applying refreshes the panel on the old preset; restore the
					// selection the user originally asked to switch to.
					this.RestorePresetSelection(newPresetId);
				}
			}

			this._assignmentsDirty = false;
			this._previousPresetId = newPresetId;
			this.UpdatePresetControls();
			this.PresetSelected?.Invoke(newPresetId);
		}

		private void RestorePresetSelection(string presetId)
		{
			this._suppressEvents = true;
			for (int index = 0; index < this._presetCombo.Items.Count; index++)
			{
				if (string.Equals((this._presetCombo.Items[index] as PresetItem)?.Id, presetId, StringComparison.Ordinal))
				{
					this._presetCombo.SelectedIndex = index;
					break;
				}
			}
			this._suppressEvents = false;
		}

		private void UpdatePresetControls()
		{
			bool hasPreset = !string.IsNullOrWhiteSpace(this.SelectedPresetId);
			this._renameButton.Enabled = hasPreset;
			this._deleteButton.Enabled = hasPreset;
			this._applyButton.Enabled = hasPreset && this._assignmentsDirty;
		}

		private void NewButton_Click(object sender, EventArgs e)
		{
			if (!this.CommitOrDiscardPendingAssignments(this.Localize("NextActionCreatePreset", "creating a new preset")))
			{
				return;
			}
			string sourceClient = this._sourceClientCombo.SelectedItem?.ToString();
			if (string.IsNullOrWhiteSpace(sourceClient))
			{
				MessageBox.Show(
					this,
					this.Localize("OpenClientFirstMessage", "Open an EVE client first. A new preset is created from an area you select in that client."),
					this.Localize("NoSourceClientTitle", "No source client"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				return;
			}
			string name = this.PromptForText(
				this.GetDialogOwner(),
				this.Localize("NewPresetTitle", "New crop preset"),
				this.Localize("PresetNameLabel", "Preset name"),
				string.Empty);
			if (!string.IsNullOrWhiteSpace(name))
			{
				this.CreatePresetRequested?.Invoke(name.Trim(), sourceClient);
			}
		}

		private void RenameButton_Click(object sender, EventArgs e)
		{
			if (!this.CommitOrDiscardPendingAssignments(this.Localize("NextActionRenamePreset", "renaming this preset")))
			{
				return;
			}
			if (this._presetCombo.SelectedItem is not PresetItem preset)
			{
				return;
			}
			string name = this.PromptForText(
				this.GetDialogOwner(),
				this.Localize("RenamePresetTitle", "Rename crop preset"),
				this.Localize("PresetNameLabel", "Preset name"),
				preset.Name);
			if (!string.IsNullOrWhiteSpace(name) && !string.Equals(name.Trim(), preset.Name, StringComparison.Ordinal))
			{
				this.RenamePresetRequested?.Invoke(preset.Id, name.Trim());
			}
		}

		private void DeleteButton_Click(object sender, EventArgs e)
		{
			if (!this.CommitOrDiscardPendingAssignments(this.Localize("NextActionDeletePreset", "deleting this preset")))
			{
				return;
			}
			if (this._presetCombo.SelectedItem is not PresetItem preset)
			{
				return;
			}
			int assignedCount = this._clientRows.Count(row => row.IsChecked);
			DialogResult result = MessageBox.Show(
				this.GetDialogOwner(),
				string.Format(
					this.Localize("DeletePresetMessageFormat", "Delete ‘{0}’? {1} assigned character(s) will return to Full window."),
					preset.Name,
					assignedCount),
				this.Localize("DeletePresetTitle", "Delete crop preset"),
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Warning);
			if (result == DialogResult.Yes)
			{
				this.DeletePresetRequested?.Invoke(preset.Id);
			}
		}

		private void SelectAreaButton_Click(object sender, EventArgs e)
		{
			if (!this.CommitOrDiscardPendingAssignments(this.Localize("NextActionEditArea", "editing the crop area")))
			{
				return;
			}
			string presetId = this.SelectedPresetId;
			string sourceClient = this._sourceClientCombo.SelectedItem?.ToString();
			if (string.IsNullOrWhiteSpace(presetId))
			{
				MessageBox.Show(
					this,
					this.Localize("SelectPresetFirstMessage", "Create or select a preset first."),
					this.Localize("NoPresetSelected", "No preset selected"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				return;
			}
			if (string.IsNullOrWhiteSpace(sourceClient))
			{
				MessageBox.Show(
					this,
					this.Localize("OpenCropSourceMessage", "Open an EVE client to use as the crop source."),
					this.Localize("NoSourceClientTitle", "No source client"),
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				return;
			}
			this.SelectAreaRequested?.Invoke(presetId, sourceClient);
		}

		private bool CommitOrDiscardPendingAssignments(string nextAction)
		{
			this.MarkAssignmentsDirty();
			if (!this._assignmentsDirty)
			{
				return true;
			}
			if (string.IsNullOrWhiteSpace(this.SelectedPresetId))
			{
				// There is nothing to apply until a preset exists. This can happen when
				// characters are checked before the first preset is created.
				this._assignmentsDirty = false;
				this._applyButton.Enabled = false;
				foreach (ClientRow row in this._clientRows)
				{
					row.IsChecked = false;
				}
				this.RebuildClientList();
				return true;
			}

			string presetName = (this._presetCombo.SelectedItem as PresetItem)?.Name ??
				this.Localize("SelectedPreset", "the selected preset");
			DialogResult result = MessageBox.Show(
				this.GetDialogOwner(),
				string.Format(
					this.Localize(
						"UnsavedAssignmentsMessageFormat",
						"You changed which characters use ‘{0}’, but have not clicked Apply assignments.\n\nApply those changes before {1}?\n\nYes = apply changes\nNo = discard changes\nCancel = keep editing"),
					presetName,
					nextAction),
				this.Localize("UnsavedAssignmentsTitle", "Unsaved crop assignments"),
				MessageBoxButtons.YesNoCancel,
				MessageBoxIcon.Question);
			if (result == DialogResult.Cancel)
			{
				return false;
			}
			if (result == DialogResult.Yes)
			{
				return this.ApplyAssignments(this.SelectedPresetId);
			}

			this._assignmentsDirty = false;
			this._applyButton.Enabled = false;
			this.PresetSelected?.Invoke(this.SelectedPresetId);
			return true;
		}

		private IWin32Window GetDialogOwner()
		{
			return (IWin32Window)this.FindForm() ?? this;
		}

		private string PromptForText(IWin32Window owner, string title, string labelText, string initialValue)
		{
			// The settings window is TopMost. Owning this modal dialog with the inner
			// UserControl can leave it behind the settings window, making the owner beep
			// as though the dialog had disappeared. Use the top-level form as the real
			// owner and mirror its TopMost state so the prompt remains visible.
			Form ownerForm = (owner as Control)?.FindForm() ?? owner as Form;
			IWin32Window dialogOwner = ownerForm != null ? ownerForm : owner;
			using Form dialog = new Form
			{
				Text = title,
				StartPosition = FormStartPosition.CenterParent,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				ClientSize = new Size(380, 120),
				MinimizeBox = false,
				MaximizeBox = false,
				ShowInTaskbar = false,
				TopMost = ownerForm?.TopMost == true
			};
			Label label = new Label { Text = labelText, AutoSize = true, Location = new Point(12, 12) };
			TextBox textBox = new TextBox { Text = initialValue ?? string.Empty, Location = new Point(12, 36), Width = 356 };
			Button okButton = new Button { Text = this.Localize("DialogOk", "OK"), DialogResult = DialogResult.OK, Location = new Point(212, 78), Width = 75 };
			Button cancelButton = new Button { Text = this.Localize("DialogCancel", "Cancel"), DialogResult = DialogResult.Cancel, Location = new Point(293, 78), Width = 75 };
			dialog.Controls.Add(label);
			dialog.Controls.Add(textBox);
			dialog.Controls.Add(okButton);
			dialog.Controls.Add(cancelButton);
			dialog.AcceptButton = okButton;
			dialog.CancelButton = cancelButton;
			dialog.Shown += (sender, args) =>
			{
				dialog.Activate();
				dialog.BringToFront();
				textBox.SelectAll();
				textBox.Focus();
			};
			return dialog.ShowDialog(dialogOwner) == DialogResult.OK ? textBox.Text : null;
		}

		private string Localize(string key, string fallback)
		{
			return LocalizationExtensions.GetString(
				$"MainForm.ContentTabControl.CropsTabPage.{key}",
				fallback);
		}
	}
}
