using System.IO;
using Newtonsoft.Json;

namespace EveOPreview.Configuration.Implementation
{
	class ConfigurationStorage : IConfigurationStorage
	{
		public const string CONFIGURATION_FILE_NAME = "EVE-O-Preview.json";
		private readonly IAppConfig _appConfig;
		private readonly IThumbnailConfiguration _thumbnailConfiguration;

		public ConfigurationStorage(IAppConfig appConfig, IThumbnailConfiguration thumbnailConfiguration)
		{
			this._appConfig = appConfig;
			this._thumbnailConfiguration = thumbnailConfiguration;
		}

		public void SetConfigurationFilename(string filename) {
			this._appConfig.ConfigFileName = filename;
		}

		public void Load()
		{
			string filename = this.GetConfigFileName();

			if (!File.Exists(filename))
			{
				return;
			}

			string rawData = File.ReadAllText(filename);
			this.BackUpPrePersonalConfiguration(filename, rawData);

			JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings()
			{
				ObjectCreationHandling = ObjectCreationHandling.Replace
			};

			// StageHotkeyArraysToAvoidDuplicates(rawData);

			JsonConvert.PopulateObject(rawData, this._thumbnailConfiguration, jsonSerializerSettings);

			// Validate data after loading it
			this._thumbnailConfiguration.ApplyRestrictions();
		}

		private void BackUpPrePersonalConfiguration(string filename, string rawData)
		{
			if (rawData.IndexOf("\"PerClientSolarSystems\"", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
				rawData.IndexOf("\"CropPresets\"", System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return;
			}

			string directory = Path.GetDirectoryName(Path.GetFullPath(filename));
			string extension = Path.GetExtension(filename);
			string name = Path.GetFileNameWithoutExtension(filename);
			string backup = Path.Combine(directory, $"{name}.before-personal-features{extension}");
			if (File.Exists(backup))
			{
				return;
			}

			try
			{
				File.Copy(filename, backup, overwrite: false);
			}
			catch (IOException)
			{
				// A backup failure must not prevent EVE-O from starting.
			}
			catch (System.UnauthorizedAccessException)
			{
				// A read-only directory must not prevent EVE-O from starting.
			}
		}

		public void Save()
		{
			string rawData = JsonConvert.SerializeObject(this._thumbnailConfiguration, Formatting.Indented);
			string filename = this.GetConfigFileName();

			try
			{
				File.WriteAllText(filename, rawData);
			}
			catch (IOException)
			{
				// Ignore error if for some reason the updated config cannot be written down
			}
		}

		private string GetConfigFileName()
		{
			return string.IsNullOrEmpty(this._appConfig.ConfigFileName) ? ConfigurationStorage.CONFIGURATION_FILE_NAME : this._appConfig.ConfigFileName;
		}
	}
}
