using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EveOPreview.Configuration;

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
				WrapContents = false,
				Padding = new Padding(0, 0, 0, 4)
			};
			presetRow.Controls.Add(new Label { Text = "Preset", AutoSize = true, Margin = new Padding(0, 7, 6, 0) });
			this._presetCombo = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Width = 128,
				Margin = new Padding(0, 2, 2, 2)
			};
			this._presetCombo.SelectedIndexChanged += this.PresetCombo_SelectedIndexChanged;
			presetRow.Controls.Add(this._presetCombo);

			Button newButton = CreateButton("New");
			newButton.AutoSize = false;
			newButton.Width = 44;
			newButton.Click += this.NewButton_Click;
			this._renameButton = CreateButton("Rename");
			this._renameButton.AutoSize = false;
			this._renameButton.Width = 58;
			this._renameButton.Click += this.RenameButton_Click;
			this._deleteButton = CreateButton("Delete");
			this._deleteButton.AutoSize = false;
			this._deleteButton.Width = 52;
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
			regionPanel.Controls.Add(new Label { Text = "Region", AutoSize = true, Margin = new Padding(0, 6, 8, 0) }, 0, 0);
			this._regionSummary = new Label
			{
				Text = "No preset selected",
				AutoEllipsis = true,
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleLeft
			};
			regionPanel.Controls.Add(this._regionSummary, 1, 0);
			regionPanel.SetColumnSpan(this._regionSummary, 2);
			regionPanel.Controls.Add(new Label { Text = "Capture from", AutoSize = true, Margin = new Padding(0, 7, 8, 0) }, 0, 1);
			this._sourceClientCombo = new ComboBox
			{
				Dock = DockStyle.Fill,
				DropDownStyle = ComboBoxStyle.DropDownList,
				Margin = new Padding(0, 2, 6, 2)
			};
			regionPanel.Controls.Add(this._sourceClientCombo, 1, 1);
			Button selectAreaButton = CreateButton("Select area...");
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
			toolsPanel.Controls.Add(new Label { Text = "Filter", AutoSize = true, Margin = new Padding(0, 7, 8, 0) }, 0, 0);
			this._filterBox = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 2) };
			this._filterBox.TextChanged += (sender, args) => this.RebuildClientList();
			toolsPanel.Controls.Add(this._filterBox, 1, 0);

			FlowLayoutPanel bulkButtons = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				WrapContents = false,
				Margin = new Padding(0)
			};
			Button selectVisibleButton = CreateButton("All visible");
			selectVisibleButton.Click += (sender, args) => this.SetVisibleChecks(true);
			Button clearVisibleButton = CreateButton("Clear");
			clearVisibleButton.Click += (sender, args) => this.SetVisibleChecks(false);
			Button invertVisibleButton = CreateButton("Invert");
			invertVisibleButton.Click += (sender, args) => this.InvertVisibleChecks();
			Button selectOpenButton = CreateButton("Open");
			selectOpenButton.Click += (sender, args) => this.SelectOpenClients();
			bulkButtons.Controls.Add(selectVisibleButton);
			bulkButtons.Controls.Add(clearVisibleButton);
			bulkButtons.Controls.Add(invertVisibleButton);
			bulkButtons.Controls.Add(selectOpenButton);
			toolsPanel.Controls.Add(bulkButtons, 0, 1);
			toolsPanel.SetColumnSpan(bulkButtons, 2);

			this._clientsList = new ListView
			{
				Dock = DockStyle.Fill,
				View = System.Windows.Forms.View.Details,
				CheckBoxes = true,
				FullRowSelect = true,
				HideSelection = false,
				MultiSelect = true
			};
			this._clientsList.Columns.Add("Character", 160);
			this._clientsList.Columns.Add("Current crop", 105);
			this._clientsList.Columns.Add("Status", 55);
			this._clientsList.ItemChecked += this.ClientsList_ItemChecked;
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
			this._applyButton = CreateButton("Apply assignments");
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
			this.UpdatePresetControls();
		}

		public Action<string> PresetSelected { get; set; }
		public Action<string> CreatePresetRequested { get; set; }
		public Action<string, string> RenamePresetRequested { get; set; }
		public Action<string> DeletePresetRequested { get; set; }
		public Action<string, string> SelectAreaRequested { get; set; }
		public Func<string, IList<string>, bool> ApplyAssignmentsRequested { get; set; }
		public Action<int, bool> SelectCycleGroupRequested { get; set; }
		public Func<string, int, bool, bool, bool> AssignCycleGroupRequested { get; set; }

		public string SelectedPresetId => (this._presetCombo.SelectedItem as PresetItem)?.Id;
		public bool HasPendingAssignments => this._assignmentsDirty;
		public bool ResolvePendingAssignments() => this.CommitOrDiscardPendingAssignments();

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
			this._regionSummary.Text = region?.IsValid == true
				? $"X {region.X:P1}   Y {region.Y:P1}   Width {region.Width:P1}   Height {region.Height:P1}"
				: "No preset selected";
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
					CurrentPreset = string.IsNullOrWhiteSpace(currentPreset) ? "Full window" : currentPreset,
					IsOpen = openClients?.Contains(client) == true,
					IsChecked = checkedClients?.Contains(client) == true
				});
			}
			this._assignmentsDirty = false;
			this.RebuildClientList();
		}

		public void SelectClients(IList<string> clients, bool onlyOpen)
		{
			HashSet<string> selected = new HashSet<string>(clients ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
			foreach (ClientRow row in this._clientRows)
			{
				if (selected.Contains(row.Title) && (!onlyOpen || row.IsOpen))
				{
					row.IsChecked = true;
				}
			}
			this.MarkAssignmentsDirty();
			this.RebuildClientList();
		}

		private static Button CreateButton(string text)
		{
			return new Button
			{
				Text = text,
				AutoSize = true,
				Height = 30,
				Margin = new Padding(2)
			};
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
				item.SubItems.Add(row.CurrentPreset);
				item.SubItems.Add(row.IsOpen ? "Open" : "Offline");
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
			row.IsChecked = e.Item.Checked;
			this.MarkAssignmentsDirty();
			this.BeginInvoke((Action)this.UpdateSelectionSummary);
		}

		private void SetVisibleChecks(bool isChecked)
		{
			this._suppressEvents = true;
			foreach (ListViewItem item in this._clientsList.Items)
			{
				if (item.Tag is ClientRow row)
				{
					row.IsChecked = isChecked;
					item.Checked = isChecked;
				}
			}
			this._suppressEvents = false;
			this.MarkAssignmentsDirty();
			this.UpdateSelectionSummary();
		}

		private void InvertVisibleChecks()
		{
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
			this.MarkAssignmentsDirty();
			this.UpdateSelectionSummary();
		}

		private void SelectOpenClients()
		{
			foreach (ClientRow row in this._clientRows)
			{
				row.IsChecked = row.IsOpen;
			}
			this.MarkAssignmentsDirty();
			this.RebuildClientList();
		}

		private void MarkAssignmentsDirty()
		{
			this._assignmentsDirty = true;
			this._applyButton.Enabled = !string.IsNullOrWhiteSpace(this.SelectedPresetId);
		}

		private void UpdateSelectionSummary()
		{
			int selected = this._clientRows.Count(row => row.IsChecked);
			this._selectionSummary.Text = $"Selected: {selected} of {this._clientRows.Count} total ({this._clientsList.Items.Count} visible)";
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
			if (this._assignmentsDirty && !string.IsNullOrWhiteSpace(this._previousPresetId))
			{
				DialogResult result = MessageBox.Show(
					this,
					"Apply the pending character assignments before changing preset?",
					"Unsaved crop assignments",
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
			if (!this.CommitOrDiscardPendingAssignments())
			{
				return;
			}
			string name = PromptForText(this, "New crop preset", "Preset name", string.Empty);
			if (!string.IsNullOrWhiteSpace(name))
			{
				this.CreatePresetRequested?.Invoke(name.Trim());
			}
		}

		private void RenameButton_Click(object sender, EventArgs e)
		{
			if (!this.CommitOrDiscardPendingAssignments())
			{
				return;
			}
			if (this._presetCombo.SelectedItem is not PresetItem preset)
			{
				return;
			}
			string name = PromptForText(this, "Rename crop preset", "Preset name", preset.Name);
			if (!string.IsNullOrWhiteSpace(name) && !string.Equals(name.Trim(), preset.Name, StringComparison.Ordinal))
			{
				this.RenamePresetRequested?.Invoke(preset.Id, name.Trim());
			}
		}

		private void DeleteButton_Click(object sender, EventArgs e)
		{
			if (!this.CommitOrDiscardPendingAssignments())
			{
				return;
			}
			if (this._presetCombo.SelectedItem is not PresetItem preset)
			{
				return;
			}
			int assignedCount = this._clientRows.Count(row => row.IsChecked);
			DialogResult result = MessageBox.Show(
				this,
				$"Delete ‘{preset.Name}’? {assignedCount} assigned character(s) will return to Full window.",
				"Delete crop preset",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Warning);
			if (result == DialogResult.Yes)
			{
				this.DeletePresetRequested?.Invoke(preset.Id);
			}
		}

		private void SelectAreaButton_Click(object sender, EventArgs e)
		{
			if (!this.CommitOrDiscardPendingAssignments())
			{
				return;
			}
			string presetId = this.SelectedPresetId;
			string sourceClient = this._sourceClientCombo.SelectedItem?.ToString();
			if (string.IsNullOrWhiteSpace(presetId))
			{
				MessageBox.Show(this, "Create or select a preset first.", "No preset selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			if (string.IsNullOrWhiteSpace(sourceClient))
			{
				MessageBox.Show(this, "Open an EVE client to use as the crop source.", "No source client", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			this.SelectAreaRequested?.Invoke(presetId, sourceClient);
		}

		private bool CommitOrDiscardPendingAssignments()
		{
			if (!this._assignmentsDirty)
			{
				return true;
			}

			DialogResult result = MessageBox.Show(
				this,
				"Apply the pending character assignments first?",
				"Unsaved crop assignments",
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

		private static string PromptForText(IWin32Window owner, string title, string labelText, string initialValue)
		{
			using Form dialog = new Form
			{
				Text = title,
				StartPosition = FormStartPosition.CenterParent,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				ClientSize = new Size(380, 120),
				MinimizeBox = false,
				MaximizeBox = false,
				ShowInTaskbar = false
			};
			Label label = new Label { Text = labelText, AutoSize = true, Location = new Point(12, 12) };
			TextBox textBox = new TextBox { Text = initialValue ?? string.Empty, Location = new Point(12, 36), Width = 356 };
			Button okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(212, 78), Width = 75 };
			Button cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(293, 78), Width = 75 };
			dialog.Controls.Add(label);
			dialog.Controls.Add(textBox);
			dialog.Controls.Add(okButton);
			dialog.Controls.Add(cancelButton);
			dialog.AcceptButton = okButton;
			dialog.CancelButton = cancelButton;
			dialog.Shown += (sender, args) => { textBox.SelectAll(); textBox.Focus(); };
			return dialog.ShowDialog(owner) == DialogResult.OK ? textBox.Text : null;
		}
	}
}
