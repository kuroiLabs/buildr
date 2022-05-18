using System;

namespace buildr
{

	public class Benchmark
	{

		private DateTime startedAt;

		public Benchmark()
		{
			startedAt = DateTime.Now;
		}

		public string Elapsed()
		{
			TimeSpan _elapsed = DateTime.Now - startedAt;

			int _totalSeconds = (int)Math.Ceiling(_elapsed.TotalSeconds);
			int _hours = (int)Math.Floor((float)(_totalSeconds / (60 * 60)));
			int _minutes = (int)Math.Floor((float)(_totalSeconds / 60));
			int _seconds = _totalSeconds % 60;

			return $"{Pad(_hours)}:{Pad(_minutes)}:{Pad(_seconds)}";
		}

		private static string Pad(int _value)
		{
			if (_value < 10)
				return $"0{_value}";

			return _value.ToString();
		}

	}

}