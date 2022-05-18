using System;
using System.Threading;
using System.Threading.Tasks;

namespace buildr
{
	public static class Debouncer
	{
		public static Action Wrap(Func<Task> _wrapped, int _timeout = 1000)
		{
			int _lastExecution = 0;
			return () => {
				int _current = Interlocked.Increment(ref _lastExecution);

				Task.Delay(_timeout).ContinueWith(async _task =>
				{
					if (_current == _lastExecution)
						await _wrapped();

					_task.Dispose();
				});
			};
		}
	}
}