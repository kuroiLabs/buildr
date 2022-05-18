
namespace buildr
{

	public static class Injector
	{

		private static FileSystem.Service fileSystemService;

		private static Config.Service configService;

		private static Monitor.State monitorState;

		private static Monitor.Service monitorService;

		private static CommandLine.Service commandLineService;

		private static Build.Service buildService;

		private static Cli cli;

		public static FileSystem.Service FileSystemService
		{
			get
			{
				if (fileSystemService == null)
					fileSystemService = new FileSystem.Service();
				return fileSystemService;
			}
		}

		public static Config.Service ConfigService
		{
			get
			{
				if (configService == null)
					configService = new Config.Service(FileSystemService);
				return configService;
			}
		}

		public static Monitor.State MonitorState
		{
			get
			{
				if (monitorState == null)
					monitorState = new Monitor.State(FileSystemService);
				return monitorState;
			}
		}

		public static Monitor.Service MonitorService
		{
			get
			{
				if (monitorService == null)
					monitorService = new Monitor.Service(FileSystemService, ConfigService, MonitorState);
				return monitorService;
			}
		}

		public static CommandLine.Service CommandLineService
		{
			get
			{
				if (commandLineService == null)
					commandLineService = new CommandLine.Service(FileSystemService, ConfigService);
				return commandLineService;
			}
		}

		public static Build.Service BuildService
		{
			get
			{
				if (buildService == null)
					buildService = new Build.Service(MonitorService, ConfigService, CommandLineService);
				return buildService;
			}
		}

		public static Cli CLI
		{
			get
			{
				if (cli == null)
					cli = new Cli();
				return cli;
			}
		}

	}

}