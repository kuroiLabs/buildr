using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace buildr.Build
{
	public class Service
	{
		
		public static readonly int DEFAULT_PARALLEL_DELAY = 500;

		private Monitor.Service monitorService;

		private Config.Service configService;

		private CommandLine.Service commandLineService;

		private Queue<Queue<string>> buildQueue;

		private HashSet<string> buildManifest;

		public Service(
			Monitor.Service _monitorService,
			Config.Service _configService,
			CommandLine.Service _commandLineService
		) {
			monitorService = _monitorService;
			configService = _configService;
			commandLineService = _commandLineService;
			buildQueue = new Queue<Queue<string>>();
			buildManifest = new HashSet<string>();
		}

		public Task<int> Build(string _project, bool _incremental, bool _output, int _delay = 0)
		{
			Clear();

			if (!_incremental)
				Logger.Info("Running non-incremental build.");

			EnqueueDependencies(_project, _incremental);

			if (_incremental && !monitorService.state.HasChanged(_project))
			{
				Logger.Info($"No action: {_project} and all dependencies up to date.");
				return Task.FromResult<int>(0);
			}

			EnqueueProject(_project);
			return ExecuteBuildQueue(_output, _delay);
		}

		public Task<int> BuildAll(bool _incremental, bool _output, int _delay = 0)
		{
			Clear();
			foreach (Config.ProjectDefinition _project in configService.GetProjects())
			{
				if (buildManifest.Contains(_project.name) || _project.type != "application")
					continue;
				
				EnqueueDependencies(_project.name, _incremental);

				if (_incremental && !monitorService.state.HasChanged(_project.name))
				{
					Logger.Info($"No action: {_project} and all dependencies up to date.");
					continue;
				}

				EnqueueProject(_project.name);
			}

			if (buildManifest.Count == 0)
			{
				Logger.Info($"No action: All projects up to date.");
				return Task.FromResult<int>(0);
			}

			return ExecuteBuildQueue(_output, _delay);
		}

		private void EnqueueBuildGroup(string _project)
		{
			EnqueueBuildGroup(new Queue<string>(new string[] { _project }));
		}

		private void EnqueueBuildGroup(Queue<string> _projects)
		{
			buildQueue.Enqueue(_projects);
		}

		private void EnqueueProject(string _project)
		{
			buildManifest.Add(_project);
			EnqueueBuildGroup(_project);
		}

		private void EnqueueDependencies(string _project, bool _incremental)
		{
			Config.ProjectDefinition _projectDefinition = configService.GetProject(_project);

			if (_projectDefinition == null)
				throw new Exception($"No valid project definition for {_project}");

			Queue<string> _buildGroup = new Queue<string>();
			int _concurrencyLimit = configService.GetConcurrencyLimit();

			if (_projectDefinition.dependencies != null && _projectDefinition.dependencies.Length > 0)
			{

				foreach (string _dependency in _projectDefinition.dependencies)
				{
					if (buildManifest.Contains(_dependency))
						continue;

					Config.ProjectDefinition _dependencyDefinition = configService.GetProject(_dependency);

					if (_dependencyDefinition == null)
						throw new Exception($"No valid project definition for {_dependency}");

					if (_dependencyDefinition.dependencies.Length > 0)
						EnqueueDependencies(_dependencyDefinition.name, _incremental);

					if (!CanBuildInParallel(_dependencyDefinition, _buildGroup)) {
						buildQueue.Enqueue(new Queue<string>(_buildGroup));
						_buildGroup.Clear();
					}

					if (_incremental)
					{
						if (!monitorService.state.HasChanged(_dependency))
						{
							Logger.Info($"No delta for {_dependency}. Skipping incremental build.");
							buildManifest.Add(_dependency);
							continue;
						}
						else
							monitorService.state.Change(_project);
					}

					_buildGroup.Enqueue(_dependency);
					buildManifest.Add(_dependency);
				}

				if (_buildGroup.Count > 0)
					EnqueueBuildGroup(_buildGroup);
			}
		}

		private async Task<int> ExecuteBuildQueue(bool _output, int _delay)
		{
			int _exitCode = 0;
			
			if (buildQueue.Count < 1)
				return _exitCode;

			Benchmark _queueTimer = new Benchmark();

			if (!String.IsNullOrEmpty(configService.configuration.preBuild))
			{
				Logger.Info($"Executing pre-build script: {configService.configuration.preBuild}");

				_exitCode = await commandLineService.Exec(configService.configuration.preBuild, _output);
				
				if (_exitCode != 0)
					return _exitCode;
			}

			Queue<string> _buildGroup;
			while (buildQueue.Count > 0)
			{
				_buildGroup = buildQueue.Dequeue();

				if (_buildGroup.Count == 0)
					continue;

				try
				{
					string[] _commands = _buildGroup.Select((string _name) =>
					{
						Config.ProjectDefinition _project = configService.GetProject(_name);
						if (String.IsNullOrEmpty(_project.buildCommand))
							return $"ng build {_project.name} --configuration production";
						return _project.buildCommand;
					}).ToArray();

					string _groupName = String.Join(", ", _buildGroup);
					Benchmark _groupTimer = new Benchmark();
					
					Logger.Info($"Executing build for {_groupName}...");
					
					if (_buildGroup.Count == 1)
						_exitCode = await commandLineService.Exec(_commands[0], _output, 0);
					else if (_buildGroup.Count > 1)
						_exitCode = await commandLineService.Exec(_commands, _output, _delay);
					else
						continue;

					if (_exitCode != 0)
					{
						Clear();
						Logger.Error($"Failed build for {_groupName} in {_groupTimer.Elapsed()}");
						return _exitCode;
					}

					Logger.Info($"Completed build for {_groupName} in {_groupTimer.Elapsed()}");
					monitorService.state.Record(_buildGroup.ToArray());
					await monitorService.state.Save();
					
				}
				catch (Exception e)
				{
					Logger.Error(e.Message);
					Clear();
					_exitCode = 1;
					return _exitCode;
				}
			}

			if (!String.IsNullOrEmpty(configService.configuration.postBuild))
			{
				Logger.Info($"Executing post-build script: {configService.configuration.postBuild}");
				_exitCode = await commandLineService.Exec(configService.configuration.postBuild, _output);
			}

			Logger.Info($"SUCCESS: Completed build queue in {_queueTimer.Elapsed()}.");
			Clear();
			return _exitCode;
		}

		private void Clear()
		{
			buildQueue.Clear();
			buildManifest.Clear();
		}

		private bool CanBuildInParallel(Config.ProjectDefinition _next, Queue<string> _buildGroup)
		{
			if (_next.dependencies.Length == 0 || _buildGroup.Count == 0)
				return true;

			if (_buildGroup.Count >= configService.GetConcurrencyLimit())
				return false;

			foreach (string _dependency in _next.dependencies)
				if (_buildGroup.Contains(_dependency))
					return false;

			return true;
		}

	}
}
