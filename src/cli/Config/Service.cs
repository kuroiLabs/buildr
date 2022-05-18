using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace buildr.Config
{
	public class Service
	{

		private static readonly string CONFIG_FILE =  @"\buildr.config.json";

		public BuildConfiguration configuration;

		private FileSystem.Service fileSystemService;

		private Dictionary<string, ProjectDefinition> projectMap;

		public bool IsValid => configuration != default(BuildConfiguration);

		public Service(FileSystem.Service _fileSystem)
		{
			fileSystemService = _fileSystem;
			projectMap = new Dictionary<string, ProjectDefinition>();
			LoadConfiguration();
		}

		public void LoadConfiguration()
		{
			try
			{
				configuration = fileSystemService.ReadFile<BuildConfiguration>(fileSystemService.Root + CONFIG_FILE);
				foreach (ProjectDefinition _project in configuration.projectDefinitions)
					projectMap[_project.name] = _project;	
			}
			catch
			{
				Logger.Error("Failed to load configuration file");
			}
		}

		public async Task SaveConfiguration()
		{
			await fileSystemService.WriteFile(fileSystemService.Root + CONFIG_FILE,
				configuration.Export());
		}

		public ProjectDefinition GetProject(string _name)
		{
			if (projectMap.ContainsKey(_name))
				return projectMap[_name];

			return null;
		}

		public ProjectDefinition[] GetProjects()
		{
			return configuration.projectDefinitions;
		}

		public int GetConcurrencyLimit()
		{
			int _processorCount = Environment.ProcessorCount;
			return configuration.maxConcurrentBuilds == 0 ?
				_processorCount : Math.Min(configuration.maxConcurrentBuilds, _processorCount);
		}

		public void SetConcurrencyLimit(int _limit)
		{
			configuration.maxConcurrentBuilds = Math.Clamp(_limit, 0, Environment.ProcessorCount);
		}

		public async Task SaveConcurrencyLimit(int _limit)
		{
			SetConcurrencyLimit(_limit);
			await SaveConfiguration();
		}

		public async Task SetTerminal(string _terminal)
		{
			if (!Terminals.IsValid(_terminal))
			{
				Logger.Error($"Invalid terminal type: {_terminal}");
				return;
			}
			configuration.terminal = _terminal;
			await SaveConfiguration();
		}

	}
}
