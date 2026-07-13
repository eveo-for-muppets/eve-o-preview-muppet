using System;

namespace EveOPreview.Configuration
{
	/// <summary>
	/// A reusable named crop. Characters reference its stable Id so renaming a
	/// preset never breaks their assignments.
	/// </summary>
	public sealed class CropPreset
	{
		public CropPreset()
		{
		}

		public CropPreset(string id, string name, CropRegion region)
		{
			this.Id = id;
			this.Name = name;
			this.Region = region;
		}

		public string Id { get; set; }
		public string Name { get; set; }
		public CropRegion Region { get; set; }

		public bool IsValid =>
			!string.IsNullOrWhiteSpace(this.Id) &&
			!string.IsNullOrWhiteSpace(this.Name) &&
			this.Region?.IsValid == true;

		public static CropPreset Create(string name, CropRegion region)
		{
			return new CropPreset(Guid.NewGuid().ToString("N"), name?.Trim(), region);
		}
	}
}
