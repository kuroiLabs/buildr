using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace buildr.CommandLine
{

	public class AsyncProcess
	{

		public static readonly int DEFAULT_ID = -1;

		public Process childProcess;

		public AsyncProcessTask asyncTask;

		public bool showOutput;

		public int id { get; private set; } = DEFAULT_ID;

		private Action<AsyncProcess> OnStart;

		private Func<int, EventHandler> OnExitFactory;

		public AsyncProcess(
			String _terminal,
			String _command,
			String _directory,
			Action<AsyncProcess> _onStart,
			Func<int, EventHandler> _onExitFactory,
			bool _showOutput = false
		)
		{
			if (!Terminals.IsValid(_terminal))
				throw new Exception($"Invalid terminal type: {_terminal}");

			childProcess = new Process
			{
				EnableRaisingEvents = true,
				StartInfo =
				{
					FileName = _terminal,
					Arguments = _terminal == Terminals.BASH ? $"-c \"{_command}\"" : $"/C {_command}",
					WorkingDirectory = _directory,
					WindowStyle = ProcessWindowStyle.Hidden,
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
				}
			};
			OnStart = _onStart;
			OnExitFactory = _onExitFactory;
			showOutput = _showOutput;
		}

		public async Task<int> Run()
		{
			childProcess.Start();

			id =  childProcess.Id;

			if (OnStart != null)
				OnStart(this);

			asyncTask = new AsyncProcessTask(childProcess, OnExitFactory);

			childProcess.Exited += asyncTask.OnExit;
			childProcess.BeginOutputReadLine();
			childProcess.BeginErrorReadLine();

			await asyncTask.GetTask();
			return childProcess.ExitCode;
		}

	}

}