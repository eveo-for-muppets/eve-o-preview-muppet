using EveOPreview.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EveOPreview.Services
{
	sealed class CharacterLocationTracker : ICharacterLocationTracker
	{
		private const int POLL_INTERVAL_MILLISECONDS = 2000;
		private const int MAX_LOG_FILES = 10000;
		private const string SOURCE_LOCAL_CHAT = "Local chat";
		private const string SOURCE_GAME_LOG = "Game log";

		private readonly IThumbnailConfiguration _configuration;
		private readonly Dictionary<string, SolarSystemObservation> _observations;
		private readonly Dictionary<string, long> _knownFileLengths;
		private readonly object _lifecycleLock;
		private readonly object _observationLock;
		private CancellationTokenSource _cancellation;
		private Task _scanTask;

		public CharacterLocationTracker(IThumbnailConfiguration configuration)
		{
			this._configuration = configuration;
			this._observations = new Dictionary<string, SolarSystemObservation>(StringComparer.OrdinalIgnoreCase);
			this._knownFileLengths = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
			this._lifecycleLock = new object();
			this._observationLock = new object();

			this.LoadLocationsFromConfiguration();
		}

		public void Start()
		{
			lock (this._lifecycleLock)
			{
				if (this._scanTask != null && !this._scanTask.IsCompleted)
				{
					return;
				}

				this._cancellation = new CancellationTokenSource();
				this.LoadLocationsFromConfiguration();
				this._scanTask = Task.Run(() => this.ScanLoop(this._cancellation.Token));
			}
		}

		public void Stop()
		{
			Task scanTask;
			lock (this._lifecycleLock)
			{
				this._cancellation?.Cancel();
				scanTask = this._scanTask;
			}

			try
			{
				scanTask?.Wait(1500);
			}
			catch (AggregateException)
			{
				// Cancellation during shutdown is expected.
			}

			this.CopyLocationsToConfiguration();
		}

		public string GetSolarSystem(string clientTitle)
		{
			if (string.IsNullOrWhiteSpace(clientTitle))
			{
				return null;
			}

			lock (this._observationLock)
			{
				return this._observations.TryGetValue(NormalizeClientTitle(clientTitle), out SolarSystemObservation observation)
					? observation.SolarSystem
					: null;
			}
		}

		private async Task ScanLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					this.ScanChangedLogs(cancellationToken);
					this.CopyLocationsToConfiguration();
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (IOException)
				{
					// EVE may rotate a log while it is being inspected. Retry next poll.
				}
				catch (UnauthorizedAccessException)
				{
					// A locked or inaccessible log should not stop the tracker.
				}

				try
				{
					await Task.Delay(POLL_INTERVAL_MILLISECONDS, cancellationToken);
				}
				catch (OperationCanceledException)
				{
					break;
				}
			}
		}

		private void ScanChangedLogs(CancellationToken cancellationToken)
		{
			string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			string chatLogsPath = Path.Combine(documents, "EVE", "logs", "Chatlogs");
			string gameLogsPath = Path.Combine(documents, "EVE", "logs", "Gamelogs");
			if (!Directory.Exists(gameLogsPath) && !Directory.Exists(chatLogsPath))
			{
				return;
			}

			IEnumerable<(FileInfo File, string Source)> gameLogs = Directory.Exists(gameLogsPath)
				? new DirectoryInfo(gameLogsPath)
					.EnumerateFiles("*.txt", SearchOption.TopDirectoryOnly)
					.Select(file => (file, SOURCE_GAME_LOG))
				: Enumerable.Empty<(FileInfo, string)>();
			IEnumerable<(FileInfo File, string Source)> localChatLogs = Directory.Exists(chatLogsPath)
				? new DirectoryInfo(chatLogsPath)
					.EnumerateFiles("Local_*.txt", SearchOption.TopDirectoryOnly)
					.Select(file => (file, SOURCE_LOCAL_CHAT))
				: Enumerable.Empty<(FileInfo, string)>();
			IEnumerable<(FileInfo File, string Source)> files = gameLogs
				.Concat(localChatLogs)
				.OrderBy(entry => entry.File.LastWriteTimeUtc)
				.TakeLast(MAX_LOG_FILES);

			foreach ((FileInfo file, string source) in files)
			{
				cancellationToken.ThrowIfCancellationRequested();
				long length;
				try
				{
					length = file.Length;
				}
				catch (IOException)
				{
					continue;
				}

				if (this._knownFileLengths.TryGetValue(file.FullName, out long knownLength) && knownLength == length)
				{
					continue;
				}

				if (this.TryReadLatestLocation(file.FullName, source, cancellationToken, out string character, out SolarSystemObservation observation))
				{
					this.ApplyObservation(character, observation);
				}
				this._knownFileLengths[file.FullName] = length;
			}
		}

		private bool TryReadLatestLocation(
			string path,
			string source,
			CancellationToken cancellationToken,
			out string character,
			out SolarSystemObservation observation)
		{
			character = null;
			observation = null;

			try
			{
				using FileStream stream = new FileStream(
					path,
					FileMode.Open,
					FileAccess.Read,
					FileShare.ReadWrite | FileShare.Delete);
				using StreamReader reader = new StreamReader(
					stream,
					Encoding.Unicode,
					detectEncodingFromByteOrderMarks: true);

				string line;
				while ((line = reader.ReadLine()) != null)
				{
					cancellationToken.ThrowIfCancellationRequested();
					string listener = TryParseHeaderValue(line, "Listener");
					if (!string.IsNullOrWhiteSpace(listener))
					{
						character = listener.Trim();
					}

					string parsedSystem = TryParseSolarSystem(line);
					if (!string.IsNullOrWhiteSpace(parsedSystem) && TryParseLogTimestamp(line, out DateTime observedAtUtc))
					{
						SolarSystemObservation candidate = new SolarSystemObservation
						{
							SolarSystem = parsedSystem,
							ObservedAtUtc = observedAtUtc,
							Source = source
						};
						if (observation == null || IsNewer(candidate, observation))
						{
							observation = candidate;
						}
					}
				}
			}
			catch (IOException)
			{
				return false;
			}
			catch (UnauthorizedAccessException)
			{
				return false;
			}

			return !string.IsNullOrWhiteSpace(character) && observation?.IsValid == true;
		}

		private static string TryParseSolarSystem(string line)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				return null;
			}

			string message = StripLogPrefix(line).Trim();
			const string localChannelChanged = "Channel changed to Local";
			int localChangeIndex = message.IndexOf(localChannelChanged, StringComparison.OrdinalIgnoreCase);
			if (localChangeIndex >= 0)
			{
				string localChange = message.Substring(localChangeIndex + localChannelChanged.Length);
				int separator = localChange.LastIndexOf(':');
				if (separator >= 0)
				{
					return CleanSystemName(localChange.Substring(separator + 1));
				}
			}

			const string jumpingFrom = "Jumping from ";
			int jumpIndex = message.IndexOf(jumpingFrom, StringComparison.OrdinalIgnoreCase);
			if (jumpIndex >= 0)
			{
				string route = message.Substring(jumpIndex + jumpingFrom.Length);
				int toIndex = route.LastIndexOf(" to ", StringComparison.OrdinalIgnoreCase);
				if (toIndex >= 0)
				{
					return CleanSystemName(route.Substring(toIndex + 4));
				}
			}

			string[] enteredPrefixes =
			{
				"You have entered ",
				"Entering solar system ",
				"Solar system changed to "
			};
			foreach (string prefix in enteredPrefixes)
			{
				int prefixIndex = message.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
				if (prefixIndex >= 0)
				{
					return CleanSystemName(message.Substring(prefixIndex + prefix.Length));
				}
			}

			return null;
		}

		private static string TryParseHeaderValue(string line, string key)
		{
			string trimmed = line?.Trim();
			if (string.IsNullOrWhiteSpace(trimmed))
			{
				return null;
			}

			int separator = trimmed.IndexOf(':');
			if (separator <= 0 || !string.Equals(trimmed.Substring(0, separator).Trim(), key, StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			return trimmed.Substring(separator + 1).Trim();
		}

		private static bool TryParseLogTimestamp(string line, out DateTime observedAtUtc)
		{
			observedAtUtc = default;
			if (string.IsNullOrWhiteSpace(line))
			{
				return false;
			}

			int openingBracket = line.IndexOf('[');
			int closingBracket = line.IndexOf(']', openingBracket + 1);
			if (openingBracket < 0 || closingBracket <= openingBracket)
			{
				return false;
			}

			string rawTimestamp = line.Substring(openingBracket + 1, closingBracket - openingBracket - 1).Trim();
			return DateTime.TryParseExact(
				rawTimestamp,
				"yyyy.MM.dd HH:mm:ss",
				CultureInfo.InvariantCulture,
				DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
				out observedAtUtc);
		}

		private static string StripLogPrefix(string line)
		{
			int closingBracket = line.IndexOf(']');
			if (closingBracket >= 0 && closingBracket + 1 < line.Length)
			{
				line = line.Substring(closingBracket + 1);
			}

			string trimmed = line.TrimStart();
			if (trimmed.StartsWith("(", StringComparison.Ordinal))
			{
				int closingParenthesis = trimmed.IndexOf(')');
				if (closingParenthesis >= 0 && closingParenthesis + 1 < trimmed.Length)
				{
					trimmed = trimmed.Substring(closingParenthesis + 1);
				}
			}
			return trimmed;
		}

		private static string CleanSystemName(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return null;
			}

			string cleaned = value.Trim().TrimEnd('.', '!', '\r', '\n');
			int parenthesis = cleaned.IndexOf(" (", StringComparison.Ordinal);
			if (parenthesis > 0)
			{
				cleaned = cleaned.Substring(0, parenthesis).TrimEnd();
			}
			return cleaned.Length <= 64 ? cleaned : null;
		}

		private static string NormalizeClientTitle(string value)
		{
			string name = value?.Trim() ?? string.Empty;
			if (name.StartsWith("EVE - ", StringComparison.OrdinalIgnoreCase))
			{
				name = name.Substring("EVE - ".Length);
			}
			else if (name.StartsWith("EVE Frontier - ", StringComparison.OrdinalIgnoreCase))
			{
				name = name.Substring("EVE Frontier - ".Length);
			}
			return name;
		}

		private void ApplyObservation(string character, SolarSystemObservation candidate)
		{
			if (string.IsNullOrWhiteSpace(character) || candidate?.IsValid != true)
			{
				return;
			}

			string normalizedCharacter = NormalizeClientTitle(character);
			lock (this._observationLock)
			{
				if (!this._observations.TryGetValue(normalizedCharacter, out SolarSystemObservation current) ||
					CompareObservations(candidate, current) > 0)
				{
					this._observations[normalizedCharacter] = CloneObservation(candidate);
				}
			}
		}

		private static bool IsNewer(SolarSystemObservation candidate, SolarSystemObservation current)
		{
			return CompareObservations(candidate, current) >= 0;
		}

		private static int CompareObservations(SolarSystemObservation candidate, SolarSystemObservation current)
		{
			int timestampComparison = candidate.ObservedAtUtc.CompareTo(current.ObservedAtUtc);
			if (timestampComparison != 0)
			{
				return timestampComparison;
			}

			return GetSourcePriority(candidate.Source).CompareTo(GetSourcePriority(current.Source));
		}

		private static int GetSourcePriority(string source)
		{
			return string.Equals(source, SOURCE_LOCAL_CHAT, StringComparison.OrdinalIgnoreCase) ? 2 :
				string.Equals(source, SOURCE_GAME_LOG, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
		}

		private static SolarSystemObservation CloneObservation(SolarSystemObservation observation)
		{
			return new SolarSystemObservation
			{
				SolarSystem = observation.SolarSystem,
				ObservedAtUtc = observation.ObservedAtUtc,
				Source = observation.Source
			};
		}

		private void CopyLocationsToConfiguration()
		{
			lock (this._observationLock)
			{
				this._configuration.PerClientSolarSystems = this._observations
					.Where(entry => !string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Value?.SolarSystem))
					.ToDictionary(
						entry => $"EVE - {entry.Key}",
						entry => entry.Value.SolarSystem,
						StringComparer.OrdinalIgnoreCase);
				this._configuration.PerClientSolarSystemObservations = this._observations
					.Where(entry => !string.IsNullOrWhiteSpace(entry.Key) && entry.Value?.IsValid == true)
					.ToDictionary(
						entry => $"EVE - {entry.Key}",
						entry => CloneObservation(entry.Value),
						StringComparer.OrdinalIgnoreCase);
			}
		}

		private void LoadLocationsFromConfiguration()
		{
			foreach (KeyValuePair<string, SolarSystemObservation> entry in this._configuration.PerClientSolarSystemObservations ??
				new Dictionary<string, SolarSystemObservation>())
			{
				this.ApplyObservation(entry.Key, entry.Value);
			}

			foreach (KeyValuePair<string, string> entry in this._configuration.PerClientSolarSystems ??
				new Dictionary<string, string>())
			{
				if (!string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Value))
				{
					string normalizedCharacter = NormalizeClientTitle(entry.Key);
					lock (this._observationLock)
					{
						if (!this._observations.ContainsKey(normalizedCharacter))
						{
							this._observations[normalizedCharacter] = new SolarSystemObservation
							{
								SolarSystem = entry.Value.Trim(),
								ObservedAtUtc = DateTime.MinValue,
								Source = "Legacy cache"
							};
						}
					}
				}
			}
		}
	}
}
