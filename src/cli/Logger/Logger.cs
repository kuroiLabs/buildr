using System;

namespace buildr
{
	public static class Logger
	{

		public static void Info(string message)
		{
			Console.WriteLine($"[buildr]: {message}");
		}

		public static void Error(string message)
		{
			Console.Error.WriteLine($"[buildr] ERROR: {message}");
		}

	}
}