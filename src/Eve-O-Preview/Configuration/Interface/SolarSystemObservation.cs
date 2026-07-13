using System;

namespace EveOPreview.Configuration
{
	public sealed class SolarSystemObservation
	{
		public string SolarSystem { get; set; }
		public DateTime ObservedAtUtc { get; set; }
		public string Source { get; set; }

		public bool IsValid =>
			!string.IsNullOrWhiteSpace(this.SolarSystem) &&
			this.ObservedAtUtc != default;
	}
}
