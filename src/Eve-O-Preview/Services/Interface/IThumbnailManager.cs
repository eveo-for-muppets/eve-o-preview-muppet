using System;
using System.Collections.Generic;
using EveOPreview.View;

namespace EveOPreview.Services
{
	public interface IThumbnailManager
	{
		event Action TemporaryCycleGroupsChanged;

		void Start();
		void Stop();

		void UpdateCycleGroupIndicator();
		void UpdateThumbnailsSize();
		void UpdateThumbnailFrames();
		void ApplyAllClientLayouts();
		void UpdateClientLayouts();

		void ReloadCycleClientHotkeys();
		int? GetTemporaryCycleGroupForClient(string title);
		IList<string> GetTemporaryCycleGroupClients(int group);
		void SetTemporaryCycleGroupClients(int group, IList<string> orderedClients);
		void ClearTemporaryCycleGroup(int group);

		IThumbnailView GetClientByTitle(string title);
		IThumbnailView GetClientByPointer(System.IntPtr ptr);
		IThumbnailView GetActiveClient();
	}
}
