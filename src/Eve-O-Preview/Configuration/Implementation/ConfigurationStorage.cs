using System;
using System.IO;
using Newtonsoft.Json;

namespace EveOPreview.Configuration.Implementation
{
	class ConfigurationStorage : IConfigurationStorage
	{
		public const string CONFIGURATION_FILE_NAME = "EVE-O-Preview.json";
		private readonly IAppConfig _appConfig;
		private readonly IThumbnailConfiguration _thumbnailConfiguration;
		private readonly object _ioLock = new object();
		private bool _skipNextBackup;

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

			lock (this._ioLock)
			{
				if (!File.Exists(filename))
				{
					this.ResetConfiguration();
					return;
				}

				try
				{
					string rawData = File.ReadAllText(filename);
					this.BackUpPrePersonalConfiguration(filename, rawData);
					this.PopulateConfiguration(rawData);
					this._skipNextBackup = false;
				}
				catch (JsonException)
				{
					// A partial or hand-edited JSON file must not prevent startup. Keep
					// the damaged file out of the last-good slot and recover if possible.
					this._skipNextBackup = true;
					string backupFilename = this.GetLastGoodFilename(filename);
					try
					{
						if (File.Exists(backupFilename))
						{
							this.PopulateConfiguration(File.ReadAllText(backupFilename));
							return;
						}
					}
					catch (JsonException)
					{
					}
					catch (IOException)
					{
					}
					catch (UnauthorizedAccessException)
					{
					}

					this.ResetConfiguration();
				}
				catch (IOException)
				{
					// Preserve the currently loaded profile if storage is temporarily unavailable.
				}
				catch (UnauthorizedAccessException)
				{
					// Preserve the currently loaded profile if storage is read-only.
				}
			}
		}

		private void PopulateConfiguration(string rawData)
		{
			this.ResetConfiguration();
			try
			{
				JsonConvert.PopulateObject(rawData, this._thumbnailConfiguration, CreateSerializerSettings());
				this._thumbnailConfiguration.ApplyRestrictions();
			}
			catch (JsonException)
			{
				this.ResetConfiguration();
				throw;
			}
		}

		private void ResetConfiguration()
		{
			string defaults = JsonConvert.SerializeObject(new ThumbnailConfiguration());
			JsonConvert.PopulateObject(defaults, this._thumbnailConfiguration, CreateSerializerSettings());
			this._thumbnailConfiguration.ApplyRestrictions();
		}

		private static JsonSerializerSettings CreateSerializerSettings()
		{
			return new JsonSerializerSettings
			{
				ObjectCreationHandling = ObjectCreationHandling.Replace
			};
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
			lock (this._ioLock)
			{
				string rawData = JsonConvert.SerializeObject(this._thumbnailConfiguration, Formatting.Indented);
				string filename = Path.GetFullPath(this.GetConfigFileName());
				string directory = Path.GetDirectoryName(filename) ?? ".";
				string name = Path.GetFileNameWithoutExtension(filename);
				string temporaryFilename = Path.Combine(directory, $"{name}.saving.tmp");
				string backupFilename = this.GetLastGoodFilename(filename);

				try
				{
					File.WriteAllText(temporaryFilename, rawData);
					if (File.Exists(filename))
					{
						if (!this._skipNextBackup)
						{
							File.Copy(filename, backupFilename, overwrite: true);
						}
						try
						{
							File.Replace(temporaryFilename, filename, null, ignoreMetadataErrors: true);
						}
						catch (PlatformNotSupportedException)
						{
							File.Move(temporaryFilename, filename, overwrite: true);
						}
					}
					else
					{
						File.Move(temporaryFilename, filename);
					}
					this._skipNextBackup = false;
				}
				catch (IOException)
				{
					// The previous file remains intact until replacement succeeds.
				}
				catch (System.UnauthorizedAccessException)
				{
					// A read-only directory must not terminate the app.
				}
				finally
				{
					try
					{
						if (File.Exists(temporaryFilename))
						{
							File.Delete(temporaryFilename);
						}
					}
					catch (IOException)
					{
					}
					catch (System.UnauthorizedAccessException)
					{
					}
				}
			}
		}

		private string GetLastGoodFilename(string filename)
		{
			string fullPath = Path.GetFullPath(filename);
			string directory = Path.GetDirectoryName(fullPath) ?? ".";
			string name = Path.GetFileNameWithoutExtension(fullPath);
			string extension = Path.GetExtension(fullPath);
			return Path.Combine(directory, $"{name}.last-good{extension}");
		}

		private string GetConfigFileName()
		{
			return string.IsNullOrEmpty(this._appConfig.ConfigFileName) ? ConfigurationStorage.CONFIGURATION_FILE_NAME : this._appConfig.ConfigFileName;
		}
	}
}
