using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace buildr.Monitor
{

	public class State
	{
		public static readonly string STATE_FILE = @".buildrstate";

		public List<string> changed { get; set; }

		public Dictionary<string, long> history { get; set; }

		private FileSystem.Service fileSystemService;

		private string lastExport;

		private string STATE_PATH
		{
			get { return fileSystemService.Root + $"\\{STATE_FILE}"; }
		}

		public State(FileSystem.Service _fileSystem)
		{
			fileSystemService = _fileSystem;
			history = new Dictionary<string, long>();
			changed = new List<string>();
			LoadExistingState();
		}

		
		// Blank constructor for JSON deserialization
		public State() { }

		public void Record(string _project)
		{
			changed.Remove(_project);
			history[_project] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		}

		public void Record(string[] _projects)
		{
			foreach (string _project in _projects)
				Record(_project);
		}

		public void Clear()
		{
			changed.Clear();
			history.Clear();
		}

		public void Clear(string _project)
		{
			changed.Remove(_project);
			history.Remove(_project);
		}

		public async Task Save()
		{
			string _export = Export();

			// don't write to disk if state is the same
			if (_export == lastExport)
				await Task.CompletedTask;
			else
			{
				lastExport = _export;
				await fileSystemService.WriteFile(STATE_PATH, _export);
			}			
		}

		public bool HasChanged(string _project)
		{
			return changed.Contains(_project) || !history.ContainsKey(_project);
		}

		public void Change(string _project)
		{
			if (!changed.Contains(_project))
				changed.Add(_project);
		}

		public string Export()
		{
			JsonSerializerOptions _options = new JsonSerializerOptions
			{
				WriteIndented = true
			};
			return JsonSerializer.Serialize(this, _options);
		}

		private void LoadExistingState()
		{
			try
			{
				State _cachedState = fileSystemService.ReadFile<State>(STATE_PATH);
				if (_cachedState == null)
				{
					Logger.Info("No existing state file.");
					return;
				}

				changed = _cachedState.changed;
				history = _cachedState.history;
			}
			catch
			{
				Logger.Error("Failed to load existing state file");
			}
		}

	}

}