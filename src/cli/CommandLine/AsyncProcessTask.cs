using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace buildr.CommandLine
{

	public class AsyncProcessTask
	{

		private TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

		private Process runningProcess;

		private EventHandler exitHandler;
		
		private bool isCompleted;

		private bool showOutput;

		public bool IsCompleted
		{
			get { return isCompleted; }
		}

		public Task GetTask()
		{
			return tcs.Task;
		}

		public void OnExit(object _sender, EventArgs _event)
		{
			if (isCompleted)
				return;
			
			isCompleted = true;

			if (exitHandler != null)
				exitHandler(runningProcess, _event);

			tcs.SetResult(runningProcess.ExitCode);
		}

		public AsyncProcessTask(
			Process _process,
			Func<int, EventHandler> _exitHandlerFactory,
			bool _showOutput = false
		) {
			runningProcess = _process;
			exitHandler = _exitHandlerFactory(_process.Id);
			showOutput = _showOutput;
		}

	}

}