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
		private sealed class LogFileState
		{
			public long Offset { get; set; }
			public string PendingText { get; set; } = string.Empty;
			public string Character { get; set; }
			public string Source { get; set; }
		}

		private const int POLL_INTERVAL_MILLISECONDS = 2000;
		private const int MAX_LOG_FILES = 1000;
		private const int MAX_LOG_AGE_DAYS = 14;
		private const int MAX_BYTES_PER_FILE_PER_SCAN = 4 * 1024 * 1024;
		private const string SOURCE_LOCAL_CHAT = "Local chat";
		private const string SOURCE_GAME_LOG = "Game log";

		private readonly IThumbnailConfiguration _configuration;
		private readonly Dictionary<string, SolarSystemObservation> _observations;
		private readonly Dictionary<string, LogFileState> _fileStates;
		private readonly object _lifecycleLock;
		private readonly object _observationLock;
		private readonly object _scanLock;
		private CancellationTokenSource _cancellation;
		private Task _scanTask;

		public CharacterLocationTracker(IThumbnailConfiguration configuration)
		{
			this._configuration = configuration;
			this._observations = new Dictionary<string, SolarSystemObservation>(StringComparer.OrdinalIgnoreCase);
			this._fileStates = new Dictionary<string, LogFileState>(StringComparer.OrdinalIgnoreCase);
			this._lifecycleLock = new object();
			this._observationLock = new object();
			this._scanLock = new object();
			this.ReloadConfiguration();
		}

		public void Start()
		{
			lock (this._lifecycleLock)
			{
				if (this._scanTask != null && !this._scanTask.IsCompleted)
				{
					// A normal repeated Start is a no-op. If Stop has already cancelled
					// the old loop, queue the replacement after it has finished.
					if (this._cancellation?.IsCancellationRequested != true)
					{
						return;
					}

					Task previousTask = this._scanTask;
					CancellationTokenSource previousCancellation = this._cancellation;
					this._cancellation = new CancellationTokenSource();
					CancellationToken nextToken = this._cancellation.Token;
					this._scanTask = Task.Run(async () =>
					{
						try
						{
							await previousTask;
						}
						catch (OperationCanceledException)
						{
						}
						previousCancellation?.Dispose();
						this.ReloadConfiguration();
						await this.ScanLoop(nextToken);
					});
					return;
				}

				this._cancellation?.Dispose();
				this._cancellation = new CancellationTokenSource();
				this.ReloadConfiguration();
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
				scanTask?.Wait(3000);
			}
			catch (AggregateException exception) when (
				exception.InnerExceptions.All(inner => inner is OperationCanceledException || inner is TaskCanceledException))
			{
			}

			this.FlushToConfiguration();
		}

		public void ReloadConfiguration()
		{
			lock (this._scanLock)
			{
				this._fileStates.Clear();
				lock (this._observationLock)
				{
					this._observations.Clear();
				}

				foreach (KeyValuePair<string, SolarSystemObservation> entry in
					this._configuration.PerClientSolarSystemObservations ?? new Dictionary<string, SolarSystemObservation>())
				{
					this.ApplyObservation(entry.Key, entry.Value);
				}

				foreach (KeyValuePair<string, string> entry in
					this._configuration.PerClientSolarSystems ?? new Dictionary<string, string>())
				{
					if (string.IsNullOrWhiteSpace(entry.Key) || string.IsNullOrWhiteSpace(entry.Value))
					{
						continue;
					}

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

		public void FlushToConfiguration()
		{
			Dictionary<string, string> systems;
			Dictionary<string, SolarSystemObservation> observations;
			lock (this._observationLock)
			{
				systems = this._observations
					.Where(entry => !string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Value?.SolarSystem))
					.ToDictionary(
						entry => $"EVE - {entry.Key}",
						entry => entry.Value.SolarSystem,
						StringComparer.OrdinalIgnoreCase);
				observations = this._observations
					.Where(entry => !string.IsNullOrWhiteSpace(entry.Key) && entry.Value?.IsValid == true)
					.ToDictionary(
						entry => $"EVE - {entry.Key}",
						entry => CloneObservation(entry.Value),
						StringComparer.OrdinalIgnoreCase);
			}

			lock (this._configuration)
			{
				this._configuration.PerClientSolarSystems = systems;
				this._configuration.PerClientSolarSystemObservations = observations;
			}
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
				if (this._configuration.ShowSolarSystemOverlay)
				{
					try
					{
						this.ScanChangedLogs(cancellationToken);
					}
					catch (OperationCanceledException)
					{
						break;
					}
					catch (IOException)
					{
						// EVE may rotate a log while it is being inspected.
					}
					catch (UnauthorizedAccessException)
					{
						// An inaccessible log must not stop thumbnail updates.
					}
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
			lock (this._scanLock)
			{
				string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				string chatLogsPath = Path.Combine(documents, "EVE", "logs", "Chatlogs");
				string gameLogsPath = Path.Combine(documents, "EVE", "logs", "Gamelogs");
				if (!Directory.Exists(gameLogsPath) && !Directory.Exists(chatLogsPath))
				{
					return;
				}

				DateTime cutoff = DateTime.UtcNow.AddDays(-MAX_LOG_AGE_DAYS);
				IEnumerable<(FileInfo File, string Source)> gameLogs = Directory.Exists(gameLogsPath)
					? new DirectoryInfo(gameLogsPath).EnumerateFiles("*.txt", SearchOption.TopDirectoryOnly)
						.Select(file => (file, SOURCE_GAME_LOG))
					: Enumerable.Empty<(FileInfo, string)>();
				IEnumerable<(FileInfo File, string Source)> localChatLogs = Directory.Exists(chatLogsPath)
					? new DirectoryInfo(chatLogsPath).EnumerateFiles("Local_*.txt", SearchOption.TopDirectoryOnly)
						.Select(file => (file, SOURCE_LOCAL_CHAT))
					: Enumerable.Empty<(FileInfo, string)>();
				List<(FileInfo File, string Source)> files = gameLogs
					.Concat(localChatLogs)
					.Where(entry => entry.File.LastWriteTimeUtc >= cutoff)
					.OrderBy(entry => entry.File.LastWriteTimeUtc)
					.TakeLast(MAX_LOG_FILES)
					.ToList();

				HashSet<string> retainedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach ((FileInfo file, string source) in files)
				{
					cancellationToken.ThrowIfCancellationRequested();
					retainedPaths.Add(file.FullName);
					this.ReadAppendedLogText(file, source, cancellationToken);
				}

				foreach (string stalePath in this._fileStates.Keys.Where(path => !retainedPaths.Contains(path)).ToList())
				{
					this._fileStates.Remove(stalePath);
				}
			}
		}

		private void ReadAppendedLogText(FileInfo file, string source, CancellationToken cancellationToken)
		{
			long length;
			try
			{
				length = file.Length;
			}
			catch (IOException)
			{
				return;
			}

			if (!this._fileStates.TryGetValue(file.FullName, out LogFileState state) || length < state.Offset)
			{
				state = new LogFileState { Source = source };
				this._fileStates[file.FullName] = state;
			}
			if (length <= state.Offset)
			{
				return;
			}

			using FileStream stream = new FileStream(
				file.FullName,
				FileMode.Open,
				FileAccess.Read,
				FileShare.ReadWrite | FileShare.Delete);
			stream.Seek(state.Offset, SeekOrigin.Begin);
			long remaining = Math.Min(length - state.Offset, MAX_BYTES_PER_FILE_PER_SCAN);
			int requestedBytes = (int)(remaining - (remaining % 2));
			if (requestedBytes <= 0)
			{
				return;
			}

			byte[] buffer = new byte[requestedBytes];
			int bytesRead = 0;
			while (bytesRead < requestedBytes)
			{
				cancellationToken.ThrowIfCancellationRequested();
				int read = stream.Read(buffer, bytesRead, requestedBytes - bytesRead);
				if (read <= 0)
				{
					break;
				}
				bytesRead += read;
			}
			bytesRead -= bytesRead % 2;
			if (bytesRead <= 0)
			{
				return;
			}

			state.Offset += bytesRead;
			string appended = Encoding.Unicode.GetString(buffer, 0, bytesRead).TrimStart('\uFEFF');
			string combined = state.PendingText + appended;
			int finalNewline = combined.LastIndexOf('\n');
			if (finalNewline < 0)
			{
				state.PendingText = combined;
				return;
			}

			string completeText = combined.Substring(0, finalNewline + 1);
			state.PendingText = combined.Substring(finalNewline + 1);
			SolarSystemObservation latest = null;
			foreach (string rawLine in completeText.Split('\n'))
			{
				cancellationToken.ThrowIfCancellationRequested();
				string line = rawLine.TrimEnd('\r');
				string listener = TryParseHeaderValue(line, "Listener");
				if (!string.IsNullOrWhiteSpace(listener))
				{
					state.Character = listener.Trim();
				}

				string parsedSystem = TryParseSolarSystem(line);
				if (!string.IsNullOrWhiteSpace(parsedSystem) && TryParseLogTimestamp(line, out DateTime observedAtUtc))
				{
					SolarSystemObservation candidate = new SolarSystemObservation
					{
						SolarSystem = parsedSystem,
						ObservedAtUtc = observedAtUtc,
						Source = state.Source
					};
					if (latest == null || IsNewer(candidate, latest))
					{
						latest = candidate;
					}
				}
			}

			if (!string.IsNullOrWhiteSpace(state.Character) && latest?.IsValid == true)
			{
				this.ApplyObservation(state.Character, latest);
			}
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
			if (string.IsNullOrWhiteSpace(character) || candidate == null || string.IsNullOrWhiteSpace(candidate.SolarSystem))
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
	}
}
