namespace EveOPreview.Services
{
	public interface ICharacterLocationTracker
	{
		void Start();
		void Stop();
		void ReloadConfiguration();
		void FlushToConfiguration();
		string GetSolarSystem(string clientTitle);
	}
}
