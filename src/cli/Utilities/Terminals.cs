using System.Collections.Generic;

namespace buildr
{
	public static class Terminals
	{

		public static readonly string CMD = "cmd.exe";

		public static readonly string BASH = "bash";

		private static HashSet<string> VALID_TERMINALS = new HashSet<string> { CMD, BASH };

		public static bool IsValid(string _terminal)
		{
			return VALID_TERMINALS.Contains(_terminal);
		}

	}
}