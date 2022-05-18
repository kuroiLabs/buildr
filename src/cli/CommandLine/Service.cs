using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

namespace buildr.CommandLine
{
	public class Service
	{

		private FileSystem.Service fileSystemService;

		private Config.Service configService;

		private ConcurrentDictionary<int, AsyncProcess> processes;

		private ConcurrentDictionary<int, EventHandler> processExitHandlers;

		private ConcurrentDictionary<int, DataReceivedEventHandler> standardOutHandlers;

		private ConcurrentDictionary<int, DataReceivedEventHandler> standardErrorHandlers;

		private ConcurrentDictionary<int, StringBuilder> errorOutput;

		public Service(FileSystem.Service _fileSystemService, Config.Service _configService)
		{
			fileSystemService = _fileSystemService;
			configService = _configService;
			processes = new ConcurrentDictionary<int, AsyncProcess>();
			processExitHandlers = new ConcurrentDictionary<int, EventHandler>();
			standardOutHandlers = new ConcurrentDictionary<int, DataReceivedEventHandler>();
			standardErrorHandlers = new ConcurrentDictionary<int, DataReceivedEventHandler>();
			errorOutput = new ConcurrentDictionary<int, StringBuilder>();

			Console.CancelKeyPress += new ConsoleCancelEventHandler(Cancel);
		}

		public async Task<int> Exec(string _command, bool _output = false, int _delay = 0)
		{
			AsyncProcess _process = SpawnProcess(_command, _output);

			if (_delay > 0)
				await Task.Delay(_delay);
			
			return await _process.Run();
		}

		public async Task<int> Exec(string[] _commands, bool _output = false, int _delay = 0)
		{
			int _wait = 0;
			List<Task<int>> _taskList = _commands.Select(_command => {
				_wait += _delay;
				return Exec(_command, _output, _delay);
			}).ToList();

			while (_taskList.Any())
			{
				Task<int> _completed = await Task.WhenAny(_taskList);
				_taskList.Remove(_completed);
				int _exitCode = await _completed;
				if (_exitCode > 0)
				{
					Logger.Error("Encountered error in parallel execution: stopping further processes...");
					return 1;
				}
			}
			return 0;
		}

		private void RegisterProcess(AsyncProcess _process)
		{
			_process.childProcess.OutputDataReceived += StandardOutCallbackFactory(_process.id);
			_process.childProcess.ErrorDataReceived += StandardErrorCallbackFactory(_process.id);
			processes.TryAdd(_process.id, _process);
		}

		private DataReceivedEventHandler StandardOutCallbackFactory(int _processId)
		{
			DataReceivedEventHandler _handler = new DataReceivedEventHandler(
				(object sender, DataReceivedEventArgs e) =>
				{
					AsyncProcess _process = processes[_processId];

					if (_process == null)
						return;

					else
					{

						if (_process.showOutput && !String.IsNullOrEmpty(e.Data))
							Logger.Info($"Process [{_processId}]: {e.Data}");

						if (
							_process.childProcess.HasExited &&
							processExitHandlers.ContainsKey(_process.id) &&
							!_process.asyncTask.IsCompleted
						)
						{
							_process.asyncTask.OnExit(_process.childProcess, null);
						}
					}
				}
			);
			standardOutHandlers.TryAdd(_processId, _handler);
			return _handler;
		}

		private DataReceivedEventHandler StandardErrorCallbackFactory(int _processId)
		{
			DataReceivedEventHandler _handler = new DataReceivedEventHandler(
				(object sender, DataReceivedEventArgs e) =>
				{
					AsyncProcess _process = processes[_processId];

					if (_process == null)
						return;

					else
					{
						if (!String.IsNullOrEmpty(e.Data))
						{
							if (_process.showOutput)
								Logger.Error($"Process [{_processId}]: {e.Data}");
							if (!errorOutput.ContainsKey(_processId))
								errorOutput.TryAdd(_processId, new StringBuilder());

							errorOutput[_processId].AppendLine(e.Data);
						}

						if (
							_process.childProcess.HasExited &&
							processExitHandlers.ContainsKey(_process.id) &&
							!_process.asyncTask.IsCompleted
						)
						{
							_process.asyncTask.OnExit(_process.childProcess, null);
						}
					}
				}
			);
			standardErrorHandlers.TryAdd(_processId, _handler);
			return _handler;
		}

		private EventHandler OnProcessExitFactory(int _processId)
		{
			EventHandler _handler = new EventHandler((object sender, EventArgs e) =>
			{
				if (!processes.ContainsKey(_processId))
					return;

				AsyncProcess _process = processes[_processId];

				if (_process.childProcess.ExitCode > 0)
				{
					if (errorOutput.ContainsKey(_processId))
						Logger.Error($"Process [{_processId}]: \n\n{errorOutput[_processId]}");

					DestroyAllProcesses();
				}
				else
					DestroyProcess(_process);
			});

			processExitHandlers.TryAdd(_processId, _handler);
			return _handler;
		}

		private AsyncProcess SpawnProcess(string _command, bool _output)
		{
			return new AsyncProcess(configService.configuration.terminal, _command,
				fileSystemService.Root, RegisterProcess, OnProcessExitFactory, _output);
		}

		private void DestroyProcess(AsyncProcess _process)
		{

			if (_process == null)
				return;

			if (_process.childProcess == null)
				return;

			EventHandler _processExitHandler;
			DataReceivedEventHandler _stdOutHandler;
			DataReceivedEventHandler _stdErrorHandler;
			StringBuilder _errorMessages;

			try
			{
				_process.childProcess.Exited -= processExitHandlers[_process.id];
				_process.childProcess.OutputDataReceived -= standardOutHandlers[_process.id];
				_process.childProcess.ErrorDataReceived -= standardErrorHandlers[_process.id];

				processExitHandlers.TryRemove(_process.id, out _processExitHandler);
				standardOutHandlers.TryRemove(_process.id, out _stdOutHandler);
				standardErrorHandlers.TryRemove(_process.id, out _stdErrorHandler);
				errorOutput.TryRemove(_process.id, out _errorMessages);
				processes.TryRemove(_process.id, out _process);

				_process.childProcess.Kill();
			}
			catch (NullReferenceException e)
			{
				Logger.Error($"Failed to destroy process [{_process.id}]: {e.Message}");
				processExitHandlers.TryRemove(_process.id, out _processExitHandler);
				standardOutHandlers.TryRemove(_process.id, out _stdOutHandler);
				standardErrorHandlers.TryRemove(_process.id, out _stdErrorHandler);
				processes.TryRemove(_process.id, out _process);
			}
		}

		public void DestroyAllProcesses()
		{
			if (processes.Count > 0)
				foreach (KeyValuePair<int, AsyncProcess> _process in processes)
					DestroyProcess(_process.Value);

			processExitHandlers.Clear();
			standardOutHandlers.Clear();
			standardErrorHandlers.Clear();
			errorOutput.Clear();
			processes.Clear();
		}

		private void Cancel(object _sender, ConsoleCancelEventArgs _args)
		{
			Logger.Info("User cancelled process. Destroying all child processes...");
			DestroyAllProcesses();
		}

	}

}
