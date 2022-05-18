namespace buildr
{
	class Program
	{

		static int Main(string[] args)
		{
			if (args.Length > 0)
			{
				return Injector.CLI.Command(args);
			}
			Logger.Error("Invalid arguments");
			return 1;
		}

	}
}
