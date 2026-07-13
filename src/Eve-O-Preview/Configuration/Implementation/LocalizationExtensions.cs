using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;


namespace EveOPreview.Configuration.Implementation
{
	public static class LocalizationExtensions
	{

		private static Dictionary<string, Dictionary<string, string>> _localizations;
		private static string _currentLanguage;
		private const string LOCALIZATION_FILENAME = "EVE-O-Preview.locale";

		static LocalizationExtensions()
		{
			// Initialize localizations first

			InitializeLocalizations();
			_currentLanguage = "en-US";
		}

		private static void InitializeLocalizations()
		{
			_localizations = new Dictionary<string, Dictionary<string, string>>();
			MergeLocalizationData(ReadEmbeddedLocalizationData());
			// A sidecar file remains editable and overrides bundled strings, but
			// missing newer keys continue to fall back to the embedded translations.
			MergeLocalizationData(TryReadExternalLocalizationData());

			if (!_localizations.ContainsKey("en-US"))
			{
				_localizations["en-US"] = new Dictionary<string, string>();
			}
		}

		private static void MergeLocalizationData(string rawData)
		{
			if (string.IsNullOrWhiteSpace(rawData))
			{
				return;
			}

			try
			{
				Dictionary<string, Dictionary<string, string>> loaded =
					JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(rawData);
				foreach (KeyValuePair<string, Dictionary<string, string>> language in
					loaded ?? new Dictionary<string, Dictionary<string, string>>())
				{
					if (!_localizations.TryGetValue(language.Key, out Dictionary<string, string> strings))
					{
						strings = new Dictionary<string, string>();
						_localizations[language.Key] = strings;
					}
					foreach (KeyValuePair<string, string> entry in
						language.Value ?? new Dictionary<string, string>())
					{
						strings[entry.Key] = entry.Value;
					}
				}
			}
			catch (JsonException)
			{
				// A damaged optional sidecar must not prevent startup or discard the
				// valid translations already loaded from the executable.
			}
		}

		private static string TryReadExternalLocalizationData()
		{
			string[] candidates =
			{
				System.IO.Path.Combine(AppContext.BaseDirectory, LOCALIZATION_FILENAME),
				System.IO.Path.GetFullPath(LOCALIZATION_FILENAME)
			};
			foreach (string filename in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
			{
				try
				{
					if (File.Exists(filename))
					{
						return File.ReadAllText(filename);
					}
				}
				catch (IOException)
				{
				}
				catch (UnauthorizedAccessException)
				{
				}
			}
			return null;
		}

		private static string ReadEmbeddedLocalizationData()
		{
			using Stream resource = Assembly.GetExecutingAssembly()
				.GetManifestResourceStream("EveOPreview.EVE-O-Preview.locale");
			if (resource == null)
			{
				return null;
			}
			using StreamReader reader = new StreamReader(resource);
			return reader.ReadToEnd();
		}

		public static void ApplyLocalization(this Form form)
		{
			// Reset NotifyIcon text as ApplyLocalization does not handle it
			//this.NotifyIcon.Text = LocalizationManager.GetString("ApplicationTitleText");

			// Apply localization to the main form itself
			SetFormLocalization(form);
			foreach(Control v in form.Controls)
			{
				ApplyLocalization(v, $"{form.Name}");
			}
		}

		public static void ApplyLocalization(this Control control, string path)
		{
			// Handle TabControl (and TabPage) separately to avoid type checking conflicts
			if (control is TabControl tabControl)
			{
				// Set text for TabPage controls
				foreach (TabPage tabPage in tabControl.TabPages)
				{
					tabPage.Text = GetString($"{path}.{tabControl.Name}.{tabPage.Name}", tabPage.Text);

					// Recursively apply localization to TabPage child controls
					foreach (Control child in tabPage.Controls)
					{
						ApplyLocalization(child, $"{path}.{tabControl.Name}.{tabPage.Name}");
					}
				}
				return;
			}

			// Handle container controls
			if (control is Panel panel)
			{
				// This will catch Panel, FlowLayoutPanel, TableLayoutPanel and other Panel-derived controls
				// Recursively apply localization to child controls for container controls
				foreach (Control child in panel.Controls)
				{
					ApplyLocalization(child, path);
				}
				return;
			}

			if (control is GroupBox groupBox)
			{
				groupBox.Text = GetString($"{path}.{groupBox.Name}", groupBox.Text);
				// Recursively apply localization to child controls for GroupBox
				foreach (Control child in groupBox.Controls)
				{
					ApplyLocalization(child, path);
				}
				return;
			}

			if (control is UserControl userControl)
			{
				// Recursively apply localization to child controls for UserControl
				foreach (Control child in userControl.Controls)
				{
					ApplyLocalization(child, path);
				}
				return;
			}

			if (control is SplitContainer splitContainer)
			{
				// Recursively apply localization to child controls for SplitContainer
				// Handle both panels of the SplitContainer
				// Panel1 and Panel2 are never null, they are automatically created with the SplitContainer
				foreach (Control child in splitContainer.Panel1.Controls)
				{
					ApplyLocalization(child, path);
				}
				foreach (Control child in splitContainer.Panel2.Controls)
				{
					ApplyLocalization(child, path);
				}
				return;
			}

			// Handle specific control types that are not in containers
			if (control is CheckBox checkBox)
			{
				checkBox.Text = GetString($"{path}.{checkBox.Name}", checkBox.Text);
			}
			else if (control is Label label)
			{
				label.Text = GetString($"{path}.{label.Name}", label.Text);
			}
			else if (control is ComboBox comboBox)
			{
				/*

				// Special handling for language combo box
				if (comboBox.Name == "LanguageCombo")
				{
					int selectedIndex = comboBox.SelectedIndex;
					comboBox.Items.Clear();
					//comboBox.Items.Add(LocalizationManager.GetString("English (en-US)"));
					//comboBox.Items.Add(LocalizationManager.GetString("中文 (zh-CN)"));
					if (selectedIndex >= 0 && selectedIndex < comboBox.Items.Count)
					{
						comboBox.SelectedIndex = selectedIndex;
					}
				}
				else
				{
					// Handle other combo boxes if needed
					switch (comboBox.Name)
					{
						case "AnimationStyleCombo":
							// For combo boxes with DataSource, we need to handle localization differently
							// The items are bound to an enum, so we don't need to modify the items directly
							// The text will be displayed using the enum's ToString() method
							// We can customize the display text by handling the Format event if needed
							break;
					}
				}
			    */
			}
			else if (control is Button button)
			{
				button.Text = GetString($"{path}.{button.Name}", button.Text);
			}
			else if (control is NumericUpDown numericUpDown)
			{
				// No specific localization needed for NumericUpDown controls
			}
			else if (control is TrackBar trackBar)
			{
				// No specific localization needed for TrackBar controls
			}
			else if (control is RadioButton radioButton)
			{
				// No specific localization needed for RadioButton controls in this application
			}
			else if (control is ListBox listBox)
			{
				// Special handling for thumbnails list
				if (listBox.Name == "ThumbnailsList")
				{
					// The list items are IThumbnailDescription objects, their text will be updated when the objects are refreshed
				}
			}
			else if (control is LinkLabel linkLabel)
			{
				switch (linkLabel.Name)
				{
					case "DocumentationLink":
						// The text is set from configuration, not localization
						break;
				}
			}
		}

		public static void SetFormLocalization(this Form form)
		{
			form.Text = LocalizationExtensions.GetString($"{form.Name}", form.Text);
		}

		// Separate method to handle ToolStripMenuItems
		public static void ApplyLocalization(this ToolStripItem item, string path)
		{
			if (item is ToolStripMenuItem menuItem)
			{
				item.Text = GetString($"{path}.{item.Name}", item.Text);
			}
		}

		public static void SetLanguage(string languageCode)
		{
			if (!string.IsNullOrWhiteSpace(languageCode) && _localizations.ContainsKey(languageCode))
			{
				_currentLanguage = languageCode;
				//Thread.CurrentThread.CurrentUICulture = new CultureInfo(languageCode);
			}
		}

		public static string GetString(string key, string current)
		{
			// Try to get string in current language
			if (_localizations.ContainsKey(_currentLanguage) && _localizations[_currentLanguage].ContainsKey(key))
			{
				return _localizations[_currentLanguage][key];
			}

			// Fall back to English
			if (_localizations.ContainsKey("en-US") && _localizations["en-US"].ContainsKey(key))
			{
				return _localizations["en-US"][key];
			}

			// Key not found
			System.Diagnostics.Debug.WriteLine($"\"{key}\": \"{current}\"");
			return current;
		}


		public static List<string> GetLanguages()
		{
			return _localizations.Keys
				.OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
	}
}
