using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EveOPreview.Configuration;
using EveOPreview.Configuration.Implementation;

namespace EveOPreview.View
{
	sealed class CycleGroupsPanel : UserControl
	{
		private sealed class CropPresetItem
		{
			public string Id { get; init; }
			public string Name { get; init; }
			public override string ToString() => this.Name;
		}

		private readonly ComboBox _modeCombo;
		private readonly ComboBox _groupCombo;
		private readonly ListBox _forwardHotkeys;
		private readonly ListBox _backwardHotkeys;
		private readonly Label _modeHint;
		private readonly TextBox _filterBox;
		private readonly FlowLayoutPanel _bulkButtons;
		private readonly ListView _clientsList;
		private readonly Label _summary;
		private readonly Button _removeButton;
		private readonly Button _clearGroupButton;
		private readonly ComboBox _cropPresetCombo;
		private readonly Button _applyCropButton;
		private readonly Dictionary<Control, string> _localizedControls = new Dictionary<Control, string>();
		private readonly List<string> _allClients = new List<string>();
		private readonly List<string> _orderedMembers = new List<string>();
		private readonly HashSet<string> _openClients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private bool _suppressEvents;

		public CycleGroupsPanel()
		{
			this.Dock = DockStyle.Fill;
			this.Padding = new Padding(5);
			this.MinimumSize = new Size(540, 440);

			TableLayoutPanel root = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				ColumnCount = 1,
				RowCount = 7,
				Padding = new Padding(0)
			};
			root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84.0f));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

			FlowLayoutPanel header = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				WrapContents = true,
				Margin = new Padding(0, 0, 0, 4)
			};
			header.Controls.Add(this.CreateLabel("TypeLabel", "Type"));
			this._modeCombo = new ComboBox
			{
				Name = "ModeCombo",
				DropDownStyle = ComboBoxStyle.DropDownList,
				Width = 105,
				Margin = new Padding(0, 2, 8, 2)
			};
			this._modeCombo.Items.AddRange(new object[] { "Saved", "Temporary" });
			this._modeCombo.SelectedIndex = 0;
			this._modeCombo.SelectedIndexChanged += this.SelectionChanged_Handler;
			header.Controls.Add(this._modeCombo);
			header.Controls.Add(this.CreateLabel("GroupLabel", "Group"));
			this._groupCombo = new ComboBox
			{
				Name = "GroupCombo",
				DropDownStyle = ComboBoxStyle.DropDownList,
				Width = 52,
				Margin = new Padding(0, 2, 8, 2)
			};
			this._groupCombo.Items.AddRange(new object[] { "1", "2", "3", "4", "5" });
			this._groupCombo.SelectedIndex = 0;
			this._groupCombo.SelectedIndexChanged += this.SelectionChanged_Handler;
			header.Controls.Add(this._groupCombo);

			TableLayoutPanel hotkeys = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoSize = false,
				ColumnCount = 4,
				RowCount = 2,
				Margin = new Padding(0, 0, 0, 4)
			};
			hotkeys.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			hotkeys.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
			hotkeys.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			hotkeys.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			hotkeys.RowStyles.Add(new RowStyle(SizeType.Percent, 50.0f));
			hotkeys.RowStyles.Add(new RowStyle(SizeType.Percent, 50.0f));
			this._forwardHotkeys = CreateHotkeyList();
			this._backwardHotkeys = CreateHotkeyList();
			this.AddHotkeyRow(hotkeys, 0, "Forward", this._forwardHotkeys);
			this.AddHotkeyRow(hotkeys, 1, "Backward", this._backwardHotkeys);

			this._modeHint = new Label
			{
				AutoSize = true,
				MaximumSize = new Size(480, 0),
				ForeColor = SystemColors.GrayText,
				Margin = new Padding(0, 0, 0, 4)
			};

			TableLayoutPanel tools = new TableLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				ColumnCount = 2,
				RowCount = 2,
				Margin = new Padding(0, 0, 0, 4)
			};
			tools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			tools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
			tools.Controls.Add(this.CreateLabel("FilterLabel", "Filter"), 0, 0);
			this._filterBox = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 2) };
			this._filterBox.TextChanged += (sender, args) => this.RebuildClientList();
			tools.Controls.Add(this._filterBox, 1, 0);
			this._bulkButtons = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				WrapContents = true,
				Margin = new Padding(0)
			};
			this._bulkButtons.Controls.Add(this.CreateActionButton("AllVisibleButton", "All visible", (sender, args) => this.SetVisibleMembership(true)));
			this._bulkButtons.Controls.Add(this.CreateActionButton("ClearVisibleButton", "Clear visible", (sender, args) => this.SetVisibleMembership(false)));
			this._bulkButtons.Controls.Add(this.CreateActionButton("InvertButton", "Invert", (sender, args) => this.InvertVisibleMembership()));
			this._bulkButtons.Controls.Add(this.CreateActionButton("OpenButton", "Open", (sender, args) => this.SelectOpenClients()));
			tools.Controls.Add(this._bulkButtons, 0, 1);
			tools.SetColumnSpan(this._bulkButtons, 2);

			this._clientsList = new ListView
			{
				Dock = DockStyle.Fill,
				View = System.Windows.Forms.View.Details,
				CheckBoxes = true,
				FullRowSelect = true,
				HideSelection = false,
				MultiSelect = true,
				AllowDrop = true,
				MinimumSize = new Size(0, 90)
			};
			this._clientsList.Columns.Add("#", 38);
			this._clientsList.Columns.Add("Character", 230);
			this._clientsList.Columns.Add("Status", 62);
			this._clientsList.ItemChecked += this.ClientsList_ItemChecked;
			this._clientsList.ItemDrag += this.ClientsList_ItemDrag;
			this._clientsList.DragEnter += this.ClientsList_DragEnter;
			this._clientsList.DragDrop += this.ClientsList_DragDrop;
			this._clientsList.MouseEnter += (sender, args) => this._clientsList.Focus();
			this._clientsList.Resize += (sender, args) => this.ResizeCharacterColumn();

			TableLayoutPanel footer = new TableLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				ColumnCount = 2,
				Margin = new Padding(0, 3, 0, 3)
			};
			footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
			footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
			this._summary = new Label { AutoSize = true, Margin = new Padding(0, 7, 0, 0) };
			footer.Controls.Add(this._summary, 0, 0);
			FlowLayoutPanel orderButtons = new FlowLayoutPanel
			{
				AutoSize = true,
				WrapContents = true,
				Margin = new Padding(0)
			};
			orderButtons.Controls.Add(this.CreateActionButton("TopButton", "Top", (sender, args) => this.MoveSelectedToTop()));
			orderButtons.Controls.Add(this.CreateActionButton("UpButton", "Up", (sender, args) => this.MoveSelected(-1)));
			orderButtons.Controls.Add(this.CreateActionButton("DownButton", "Down", (sender, args) => this.MoveSelected(1)));
			orderButtons.Controls.Add(this.CreateActionButton("BottomButton", "Bottom", (sender, args) => this.MoveSelectedToBottom()));
			this._removeButton = this.CreateActionButton("RemoveMemberButton", "Remove", (sender, args) => this.RemoveSelectedTemporaryMembers());
			this._clearGroupButton = this.CreateActionButton("ClearGroupButton", "Clear group", (sender, args) => this.ClearTemporaryMembers());
			orderButtons.Controls.Add(this._removeButton);
			orderButtons.Controls.Add(this._clearGroupButton);
			footer.Controls.Add(orderButtons, 1, 0);

			FlowLayoutPanel cropRow = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				WrapContents = true,
				Margin = new Padding(0)
			};
			cropRow.Controls.Add(this.CreateLabel("CropPresetLabel", "Crop preset"));
			this._cropPresetCombo = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Width = 155,
				Margin = new Padding(0, 2, 5, 2)
			};
			this._cropPresetCombo.SelectedIndexChanged += (sender, args) => this.UpdateCropButton();
			cropRow.Controls.Add(this._cropPresetCombo);
			this._applyCropButton = this.CreateActionButton("ApplyCropButton", "Apply to members", this.ApplyCropButton_Click);
			cropRow.Controls.Add(this._applyCropButton);

			root.Controls.Add(header, 0, 0);
			root.Controls.Add(hotkeys, 0, 1);
			root.Controls.Add(this._modeHint, 0, 2);
			root.Controls.Add(tools, 0, 3);
			root.Controls.Add(this._clientsList, 0, 4);
			root.Controls.Add(footer, 0, 5);
			root.Controls.Add(cropRow, 0, 6);
			this.Controls.Add(root);
			this.Resize += (sender, args) => this.UpdateResponsiveBounds();
			this.UpdateModeControls();
			this.UpdateResponsiveBounds();
		}

		public void ApplyLocalization()
		{
			foreach (KeyValuePair<Control, string> entry in this._localizedControls)
			{
				entry.Key.Text = this.Localize(entry.Key.Name, entry.Value);
			}

			int modeIndex = this._modeCombo.SelectedIndex;
			this._suppressEvents = true;
			this._modeCombo.Items.Clear();
			this._modeCombo.Items.Add(this.Localize("ModeSaved", "Saved"));
			this._modeCombo.Items.Add(this.Localize("ModeTemporary", "Temporary"));
			this._modeCombo.SelectedIndex = modeIndex < 0 ? 0 : modeIndex;
			this._suppressEvents = false;

			this._clientsList.Columns[0].Text = "#";
			this._clientsList.Columns[1].Text = this.Localize("CharacterColumn", "Character");
			this._clientsList.Columns[2].Text = this.Localize("StatusColumn", "Status");
			this._clientsList.Columns[2].Width = Math.Max(62, new[]
			{
				this._clientsList.Columns[2].Text,
				this.Localize("StatusOpen", "Open"),
				this.Localize("StatusOffline", "Offline")
			}.Max(text => TextRenderer.MeasureText(text, this._clientsList.Font).Width) + 18);
			this.UpdateModeControls();
			this.UpdateResponsiveBounds();
		}

		public Action SelectionChanged { get; set; }
		public Action ContentsChanged { get; set; }
		public Func<string, bool> ValidateHotkey { get; set; }
		public Func<string, int, bool, bool> ApplyCropRequested { get; set; }

		public bool IsTemporary
		{
			get => this._modeCombo.SelectedIndex == 1;
			set
			{
				this._suppressEvents = true;
				this._modeCombo.SelectedIndex = value ? 1 : 0;
				this._suppressEvents = false;
				this.UpdateModeControls();
			}
		}
		public int SelectedGroup
		{
			get => Math.Max(1, this._groupCombo.SelectedIndex + 1);
			set
			{
				this._suppressEvents = true;
				this._groupCombo.SelectedIndex = Math.Max(0, Math.Min(4, value - 1));
				this._suppressEvents = false;
			}
		}

		public string ForwardHotkeysText
		{
			get => string.Join(",", this._forwardHotkeys.Items.Cast<object>().Select(item => item.ToString()));
			set => SetHotkeys(this._forwardHotkeys, value);
		}

		public string BackwardHotkeysText
		{
			get => string.Join(",", this._backwardHotkeys.Items.Cast<object>().Select(item => item.ToString()));
			set => SetHotkeys(this._backwardHotkeys, value);
		}

		public void SetAvailableClients(IList<string> clients)
		{
			this._allClients.Clear();
			this._allClients.AddRange((clients ?? new List<string>())
				.Where(client => !string.IsNullOrWhiteSpace(client))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(client => client, StringComparer.OrdinalIgnoreCase));
			foreach (string member in this._orderedMembers)
			{
				if (!this._allClients.Contains(member, StringComparer.OrdinalIgnoreCase))
				{
					this._allClients.Add(member);
				}
			}
			this.RebuildClientList();
		}

		public void SetClientOpen(string client, bool isOpen)
		{
			if (string.IsNullOrWhiteSpace(client))
			{
				return;
			}
			if (!this._allClients.Contains(client, StringComparer.OrdinalIgnoreCase))
			{
				this._allClients.Add(client);
			}
			if (isOpen)
			{
				this._openClients.Add(client);
			}
			else
			{
				this._openClients.Remove(client);
			}
			this.RebuildClientList();
		}

		public void SetOrderedMembers(IList<string> orderedMembers)
		{
			this._orderedMembers.Clear();
			this._orderedMembers.AddRange((orderedMembers ?? new List<string>())
				.Where(client => !string.IsNullOrWhiteSpace(client))
				.Distinct(StringComparer.OrdinalIgnoreCase));
			foreach (string member in this._orderedMembers)
			{
				if (!this._allClients.Contains(member, StringComparer.OrdinalIgnoreCase))
				{
					this._allClients.Add(member);
				}
			}
			this.RebuildClientList();
		}

		public IList<string> GetOrderedMembers() => this._orderedMembers.ToList();

		public void SetCropPresets(IList<CropPreset> presets, string selectedPresetId)
		{
			string previousId = (this._cropPresetCombo.SelectedItem as CropPresetItem)?.Id ?? selectedPresetId;
			this._cropPresetCombo.Items.Clear();
			foreach (CropPreset preset in (presets ?? new List<CropPreset>())
				.Where(preset => preset?.IsValid == true)
				.OrderBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase))
			{
				this._cropPresetCombo.Items.Add(new CropPresetItem { Id = preset.Id, Name = preset.Name });
			}
			int index = Enumerable.Range(0, this._cropPresetCombo.Items.Count)
				.FirstOrDefault(candidate => string.Equals(
					(this._cropPresetCombo.Items[candidate] as CropPresetItem)?.Id,
					previousId,
					StringComparison.Ordinal));
			bool found = this._cropPresetCombo.Items.Count > 0 && string.Equals(
				(this._cropPresetCombo.Items[index] as CropPresetItem)?.Id,
				previousId,
				StringComparison.Ordinal);
			this._cropPresetCombo.SelectedIndex = found ? index : (this._cropPresetCombo.Items.Count > 0 ? 0 : -1);
			this.UpdateCropButton();
		}

		private Label CreateLabel(string name, string text)
		{
			Label label = new Label { Name = name, Text = text, AutoSize = true, Margin = new Padding(0, 7, 6, 0) };
			this._localizedControls[label] = text;
			return label;
		}

		private Button CreateActionButton(string name, string text, EventHandler handler)
		{
			Button button = new Button { Name = name, Text = text, AutoSize = true, Height = 29, Margin = new Padding(2) };
			button.Click += handler;
			this._localizedControls[button] = text;
			return button;
		}

		private static ListBox CreateHotkeyList()
		{
			return new ListBox
			{
				Dock = DockStyle.Fill,
				Height = 40,
				IntegralHeight = false,
				HorizontalScrollbar = true,
				Margin = new Padding(2)
			};
		}

		private void AddHotkeyRow(TableLayoutPanel table, int row, string label, ListBox list)
		{
			table.Controls.Add(this.CreateLabel($"{label}Label", label), 0, row);
			table.Controls.Add(list, 1, row);
			Button record = this.CreateActionButton($"{label}RecordButton", "Record", (sender, args) => this.RecordHotkey(list, label));
			record.AutoSize = true;
			record.Dock = DockStyle.Fill;
			Button remove = this.CreateActionButton($"{label}RemoveButton", "Remove", (sender, args) => this.RemoveHotkey(list));
			remove.AutoSize = true;
			remove.Dock = DockStyle.Fill;
			table.Controls.Add(record, 2, row);
			table.Controls.Add(remove, 3, row);
		}

		private void UpdateResponsiveBounds()
		{
			if (this._modeCombo != null)
			{
				int dropdownPadding = SystemInformation.VerticalScrollBarWidth + 18;
				string longestMode = this._modeCombo.Items.Cast<object>()
					.Select(item => item?.ToString() ?? string.Empty)
					.OrderByDescending(text => TextRenderer.MeasureText(text, this._modeCombo.Font).Width)
					.FirstOrDefault() ?? "Temporary";
				this._modeCombo.Width = Math.Max(105,
					TextRenderer.MeasureText(longestMode, this._modeCombo.Font).Width + dropdownPadding);
				this._groupCombo.Width = Math.Max(52,
					TextRenderer.MeasureText("5", this._groupCombo.Font).Width + dropdownPadding);
			}
			if (this._modeHint != null)
			{
				this._modeHint.MaximumSize = new Size(
					Math.Max(220, this.ClientSize.Width - this.Padding.Horizontal),
					0);
			}
		}

		private static void SetHotkeys(ListBox list, string csv)
		{
			list.Items.Clear();
			foreach (string hotkey in (csv ?? string.Empty)
				.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(value => value.Trim())
				.Where(value => !string.IsNullOrWhiteSpace(value))
				.Distinct(StringComparer.OrdinalIgnoreCase))
			{
				list.Items.Add(hotkey);
			}
		}

		private void RecordHotkey(ListBox target, string direction)
		{
			string localizedDirection = this.Localize($"{direction}Direction", direction).ToLowerInvariant();
			string hotkey = this.CaptureHotkey(this, string.Format(
				this.Localize("RecordHotkeyTitleFormat", "Record {0} hotkey"),
				localizedDirection));
			if (string.IsNullOrWhiteSpace(hotkey) || target.Items.Cast<object>().Any(item =>
				string.Equals(item.ToString(), hotkey, StringComparison.OrdinalIgnoreCase)))
			{
				return;
			}
			if (this.ValidateHotkey?.Invoke(hotkey) == false)
			{
				return;
			}
			target.Items.Add(hotkey);
			this.ContentsChanged?.Invoke();
		}

		private void RemoveHotkey(ListBox target)
		{
			if (target.SelectedIndex < 0)
			{
				return;
			}
			target.Items.RemoveAt(target.SelectedIndex);
			this.ContentsChanged?.Invoke();
		}

		private void SelectionChanged_Handler(object sender, EventArgs e)
		{
			if (this._suppressEvents)
			{
				return;
			}
			this.UpdateModeControls();
			this.SelectionChanged?.Invoke();
		}

		private void UpdateModeControls()
		{
			this._clientsList.CheckBoxes = !this.IsTemporary;
			this._bulkButtons.Visible = !this.IsTemporary;
			this._removeButton.Visible = this.IsTemporary;
			this._clearGroupButton.Visible = this.IsTemporary;
			this._modeHint.Text = this.IsTemporary
				? string.Format(
					this.Localize("TemporaryHintFormat", "Hold {0} and click a thumbnail to toggle Temp {0}. Membership clears when EVE-O exits."),
					this.SelectedGroup)
				: this.Localize("SavedHint", "Checked characters are saved in JSON. Temporary assignments override saved groups for this session.");
			this.RebuildClientList();
		}

		private void RebuildClientList()
		{
			if (this._clientsList == null)
			{
				return;
			}
			string filter = this._filterBox?.Text?.Trim() ?? string.Empty;
			IEnumerable<string> displayClients = this.IsTemporary
				? this._orderedMembers
				: this._orderedMembers.Concat(this._allClients
					.Where(client => !this._orderedMembers.Contains(client, StringComparer.OrdinalIgnoreCase))
					.OrderBy(client => client, StringComparer.OrdinalIgnoreCase));
			displayClients = displayClients.Where(client =>
				string.IsNullOrEmpty(filter) || client.Contains(filter, StringComparison.OrdinalIgnoreCase));

			this._suppressEvents = true;
			this._clientsList.BeginUpdate();
			this._clientsList.Items.Clear();
			foreach (string client in displayClients)
			{
				int memberIndex = this._orderedMembers.FindIndex(member =>
					string.Equals(member, client, StringComparison.OrdinalIgnoreCase));
				ListViewItem item = new ListViewItem(memberIndex >= 0 ? (memberIndex + 1).ToString() : string.Empty)
				{
					Tag = client,
					Checked = memberIndex >= 0
				};
				item.SubItems.Add(client);
				item.SubItems.Add(this._openClients.Contains(client)
					? this.Localize("StatusOpen", "Open")
					: this.Localize("StatusOffline", "Offline"));
				this._clientsList.Items.Add(item);
			}
			this._clientsList.EndUpdate();
			this._suppressEvents = false;
			this.ResizeCharacterColumn();
			this.UpdateSummary();
			this.UpdateCropButton();
		}

		private void ResizeCharacterColumn()
		{
			if (this._clientsList.Columns.Count >= 3)
			{
				this._clientsList.Columns[1].Width = Math.Max(90,
					this._clientsList.ClientSize.Width - this._clientsList.Columns[0].Width - this._clientsList.Columns[2].Width - 8);
			}
		}

		private void ClientsList_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			if (this._suppressEvents || this.IsTemporary || e.Item.Tag is not string client)
			{
				return;
			}
			if (e.Item.Checked)
			{
				if (!this._orderedMembers.Contains(client, StringComparer.OrdinalIgnoreCase))
				{
					this._orderedMembers.Add(client);
				}
			}
			else
			{
				this._orderedMembers.RemoveAll(member => string.Equals(member, client, StringComparison.OrdinalIgnoreCase));
			}
			this.ContentsChanged?.Invoke();
			this.BeginInvoke((Action)this.RebuildClientList);
		}

		private IEnumerable<string> VisibleClients()
		{
			return this._clientsList.Items.Cast<ListViewItem>()
				.Select(item => item.Tag as string)
				.Where(client => !string.IsNullOrWhiteSpace(client));
		}

		private void SetVisibleMembership(bool isMember)
		{
			foreach (string client in this.VisibleClients().ToList())
			{
				bool alreadyMember = this._orderedMembers.Contains(client, StringComparer.OrdinalIgnoreCase);
				if (isMember && !alreadyMember)
				{
					this._orderedMembers.Add(client);
				}
				else if (!isMember && alreadyMember)
				{
					this._orderedMembers.RemoveAll(member => string.Equals(member, client, StringComparison.OrdinalIgnoreCase));
				}
			}
			this.NotifyContentsChanged();
		}

		private void InvertVisibleMembership()
		{
			foreach (string client in this.VisibleClients().ToList())
			{
				int index = this._orderedMembers.FindIndex(member => string.Equals(member, client, StringComparison.OrdinalIgnoreCase));
				if (index >= 0)
				{
					this._orderedMembers.RemoveAt(index);
				}
				else
				{
					this._orderedMembers.Add(client);
				}
			}
			this.NotifyContentsChanged();
		}

		private void SelectOpenClients()
		{
			this._orderedMembers.Clear();
			this._orderedMembers.AddRange(this._allClients
				.Where(client => this._openClients.Contains(client))
				.OrderBy(client => client, StringComparer.OrdinalIgnoreCase));
			this.NotifyContentsChanged();
		}

		private List<string> SelectedMemberTitles()
		{
			return this._clientsList.SelectedItems.Cast<ListViewItem>()
				.Select(item => item.Tag as string)
				.Where(client => !string.IsNullOrWhiteSpace(client))
				.Where(client => this._orderedMembers.Contains(client, StringComparer.OrdinalIgnoreCase))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		private void MoveSelected(int direction)
		{
			HashSet<string> selected = new HashSet<string>(this.SelectedMemberTitles(), StringComparer.OrdinalIgnoreCase);
			if (direction < 0)
			{
				for (int index = 1; index < this._orderedMembers.Count; index++)
				{
					if (selected.Contains(this._orderedMembers[index]) && !selected.Contains(this._orderedMembers[index - 1]))
					{
						(this._orderedMembers[index - 1], this._orderedMembers[index]) =
							(this._orderedMembers[index], this._orderedMembers[index - 1]);
					}
				}
			}
			else
			{
				for (int index = this._orderedMembers.Count - 2; index >= 0; index--)
				{
					if (selected.Contains(this._orderedMembers[index]) && !selected.Contains(this._orderedMembers[index + 1]))
					{
						(this._orderedMembers[index], this._orderedMembers[index + 1]) =
							(this._orderedMembers[index + 1], this._orderedMembers[index]);
					}
				}
			}
			this.NotifyContentsChanged(selected);
		}

		private void MoveSelectedToTop()
		{
			List<string> selected = this._orderedMembers
				.Where(this.SelectedMemberTitles().Contains)
				.ToList();
			this._orderedMembers.RemoveAll(selected.Contains);
			this._orderedMembers.InsertRange(0, selected);
			this.NotifyContentsChanged(new HashSet<string>(selected, StringComparer.OrdinalIgnoreCase));
		}

		private void MoveSelectedToBottom()
		{
			List<string> selected = this._orderedMembers
				.Where(this.SelectedMemberTitles().Contains)
				.ToList();
			this._orderedMembers.RemoveAll(selected.Contains);
			this._orderedMembers.AddRange(selected);
			this.NotifyContentsChanged(new HashSet<string>(selected, StringComparer.OrdinalIgnoreCase));
		}

		private void RemoveSelectedTemporaryMembers()
		{
			HashSet<string> selected = new HashSet<string>(this.SelectedMemberTitles(), StringComparer.OrdinalIgnoreCase);
			this._orderedMembers.RemoveAll(selected.Contains);
			this.NotifyContentsChanged();
		}

		private void ClearTemporaryMembers()
		{
			if (this._orderedMembers.Count == 0 || MessageBox.Show(
				this,
				string.Format(this.Localize("ClearTemporaryMessageFormat", "Clear all members from Temp {0}?"), this.SelectedGroup),
				this.Localize("ClearTemporaryTitle", "Clear temporary group"),
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question) != DialogResult.Yes)
			{
				return;
			}
			this._orderedMembers.Clear();
			this.NotifyContentsChanged();
		}

		private void ClientsList_ItemDrag(object sender, ItemDragEventArgs e)
		{
			List<string> selected = this.SelectedMemberTitles();
			if (selected.Count > 0)
			{
				this._clientsList.DoDragDrop(selected, DragDropEffects.Move);
			}
		}

		private void ClientsList_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(typeof(List<string>)) ? DragDropEffects.Move : DragDropEffects.None;
		}

		private void ClientsList_DragDrop(object sender, DragEventArgs e)
		{
			if (e.Data.GetData(typeof(List<string>)) is not List<string> moving || moving.Count == 0)
			{
				return;
			}
			Point clientPoint = this._clientsList.PointToClient(new Point(e.X, e.Y));
			ListViewItem targetItem = this._clientsList.GetItemAt(clientPoint.X, clientPoint.Y);
			string target = targetItem?.Tag as string;
			HashSet<string> movingSet = new HashSet<string>(moving, StringComparer.OrdinalIgnoreCase);
			List<string> orderedMoving = this._orderedMembers.Where(movingSet.Contains).ToList();
			this._orderedMembers.RemoveAll(movingSet.Contains);
			int insertionIndex = string.IsNullOrWhiteSpace(target)
				? this._orderedMembers.Count
				: this._orderedMembers.FindIndex(member => string.Equals(member, target, StringComparison.OrdinalIgnoreCase));
			if (insertionIndex < 0)
			{
				insertionIndex = this._orderedMembers.Count;
			}
			this._orderedMembers.InsertRange(insertionIndex, orderedMoving);
			this.NotifyContentsChanged(movingSet);
		}

		private void NotifyContentsChanged(ISet<string> restoreSelection = null)
		{
			this.ContentsChanged?.Invoke();
			this.RebuildClientList();
			if (restoreSelection != null)
			{
				foreach (ListViewItem item in this._clientsList.Items)
				{
					item.Selected = item.Tag is string client && restoreSelection.Contains(client);
				}
			}
		}

		private void UpdateSummary()
		{
			this._summary.Text = this.IsTemporary
				? string.Format(
					this.Localize("TemporarySummaryFormat", "Temp {0}: {1} member(s)"),
					this.SelectedGroup,
					this._orderedMembers.Count)
				: string.Format(
					this.Localize("SavedSummaryFormat", "Saved {0}: {1} of {2} member(s)"),
					this.SelectedGroup,
					this._orderedMembers.Count,
					this._allClients.Count);
		}

		private void UpdateCropButton()
		{
			this._applyCropButton.Enabled = this._cropPresetCombo.SelectedItem is CropPresetItem && this._orderedMembers.Count > 0;
		}

		private void ApplyCropButton_Click(object sender, EventArgs e)
		{
			if (this._cropPresetCombo.SelectedItem is CropPresetItem preset)
			{
				this.ApplyCropRequested?.Invoke(preset.Id, this.SelectedGroup, this.IsTemporary);
			}
		}

		private string CaptureHotkey(IWin32Window owner, string title)
		{
			Form ownerForm = (owner as Control)?.FindForm() ?? owner as Form;
			using Form dialog = new Form
			{
				Text = title,
				StartPosition = FormStartPosition.CenterParent,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				ClientSize = new Size(330, 92),
				MinimizeBox = false,
				MaximizeBox = false,
				ShowInTaskbar = false,
				KeyPreview = true,
				TopMost = ownerForm?.TopMost == true
			};
			Label instruction = new Label
			{
				Text = this.Localize("HotkeyDialogInstruction", "Press the key combination to record. Press Esc to cancel."),
				AutoSize = true,
				Location = new Point(12, 14)
			};
			Label captured = new Label
			{
				Text = this.Localize("HotkeyDialogWaiting", "Waiting for key..."),
				AutoSize = true,
				Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
				Location = new Point(12, 48)
			};
			dialog.Controls.Add(instruction);
			dialog.Controls.Add(captured);
			string result = null;
			dialog.KeyDown += (sender, args) =>
			{
				args.SuppressKeyPress = true;
				args.Handled = true;
				if (args.KeyCode == Keys.Escape)
				{
					dialog.DialogResult = DialogResult.Cancel;
					dialog.Close();
					return;
				}
				if (args.KeyCode == Keys.ControlKey || args.KeyCode == Keys.ShiftKey ||
					args.KeyCode == Keys.Menu || args.KeyCode == Keys.ProcessKey)
				{
					return;
				}
				result = new KeysConverter().ConvertToInvariantString(args.KeyData);
				captured.Text = result;
				dialog.DialogResult = DialogResult.OK;
				dialog.Close();
			};
			dialog.Shown += (sender, args) => { dialog.Activate(); dialog.BringToFront(); };
			IWin32Window dialogOwner = ownerForm != null ? ownerForm : owner;
			return dialog.ShowDialog(dialogOwner) == DialogResult.OK ? result : null;
		}

		private string Localize(string key, string fallback)
		{
			return LocalizationExtensions.GetString(
				$"MainForm.ContentTabControl.CycleGroupTabPage.{key}",
				fallback);
		}
	}
}
