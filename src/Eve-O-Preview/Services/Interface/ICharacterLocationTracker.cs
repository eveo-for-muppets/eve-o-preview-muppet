namespace EveOPreview.Services
{
	public interface ICharacterLocationTracker
	{
		void Start();
		void Stop();
		string GetSolarSystem(string clientTitle);
	}
}
