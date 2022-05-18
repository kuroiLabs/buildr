using System;
using System.Collections.Generic;

namespace buildr
{

	public class Cli
	{

		private static readonly string VERSION = "0.0.1";

		private static readonly string INVALID_CONFIG_MSG = "No available configuration file";

		private Config.Service configService {
			get { return Injector.ConfigService; }
		}

		private Build.Service buildService {
			get { return Injector.BuildService; }
		}

		private Monitor.Service monitorService {
			get { return Injector.MonitorService; }
		}

		private CommandLine.Service commandLineService
		{
			get { return Injector.CommandLineService; }
		}

		public Cli()
		{
			
		}

		/** Returns true if CLI should prompt read line again after execution */
		public int Command(string[] _args)
		{
			Dictionary<string, string> _arguments = ArgumentParser.Parse(_args);
			int _exitCode = 0;

			switch (_args[0].ToLower())
			{
				case "start":
				case "watch":
					StartCommand();
					break;

				case "stop":
				case "kill":
					StopCommand();
					break;

				case "build":
					BuildCommand(_args, _arguments, ref _exitCode);
					break;

				case "reset":
				case "clear":
					ResetCommand(_args, ref _exitCode);
					break;

				case "cls":
					Console.Clear();
					break;

				case "version":
					Logger.Info(VERSION);
					break;

				case "config":
					RunConfig(_arguments, ref _exitCode);
					break;

				default:
					Logger.Error($"Unknown command \"{_args[0]}\"");
					break;
			}

			return _exitCode;

		}

		private void Run()
		{
			string _input = Prompt();
			string[] _args = _input.Split(" ");
			int _exitCode = 0;

			if (_args.Length == 0)
			{
				Console.Error.WriteLine("Invalid argument list");
				_exitCode = 1;
			}
			else
				_exitCode = Command(_args);

			if (monitorService.isRunning)
				Run();
			else
				Environment.Exit(_exitCode);
		}

		private string Prompt()
		{
			Console.Write("buildr > ");
			return Console.ReadLine();
		}

		private void StartCommand()
		{
			if (!configService.IsValid)
			{
				Logger.Error(INVALID_CONFIG_MSG);
				return;
			}
			monitorService.Start();
			Run();
		}

		private void StopCommand()
		{
			commandLineService.DestroyAllProcesses();
			monitorService.Stop();
		}

		private void BuildCommand(string[] _args, Dictionary<string, string> _arguments, ref int _exitCode)
		{
			if (_args.Length < 2 || String.IsNullOrEmpty(_args[1]))
			{
				Logger.Error("Please provide a valid project name");
				_exitCode = 1;
				return;
			}

			if (!configService.IsValid)
			{
				Logger.Error(INVALID_CONFIG_MSG);
				_exitCode = 1;
				return;
			}

			bool _incremental = !_arguments.ContainsKey("--incremental") ||
				_arguments["--incremental"] != "false";
			bool _output = _arguments.ContainsKey("--output") &&
				_arguments["--output"] != "false";
			int _delay = Build.Service.DEFAULT_PARALLEL_DELAY;
			int _concurrency = Injector.ConfigService.GetConcurrencyLimit();

			if (_arguments.ContainsKey("--delay"))
				_delay = Int32.Parse(_arguments["--delay"]);

			if (_arguments.ContainsKey("--concurrency"))
				configService.SetConcurrencyLimit(Int32.Parse(_arguments["--concurrency"]));

			if (_args[1] == "all")
				_exitCode = buildService.BuildAll(_incremental, _output, _delay).GetAwaiter().GetResult();
			else
				_exitCode = buildService.Build(_args[1], _incremental, _output, _delay).GetAwaiter().GetResult();

			// set concurrency limit back to saved value
			configService.SetConcurrencyLimit(_concurrency);
		}

		private void ResetCommand(string[] _args, ref int _exitCode)
		{
			if (!configService.IsValid)
			{
				Logger.Error(INVALID_CONFIG_MSG);
				_exitCode = 1;
				return;
			}
			if (_args.Length > 1 && !String.IsNullOrEmpty(_args[1]))
				monitorService.state.Clear(_args[1]);
			else
				monitorService.state.Clear();
			monitorService.state.Save().GetAwaiter().GetResult();
		}

		private void RunConfig(Dictionary<string, string> _arguments, ref int _exitCode)
		{
			if (!configService.IsValid)
			{
				Logger.Error(INVALID_CONFIG_MSG);
				_exitCode = 1;
				return;
			}

			if (_arguments.ContainsKey("--terminal"))
				configService.SetTerminal(_arguments["--terminal"]).GetAwaiter().GetResult();

			if (_arguments.ContainsKey("--concurrency"))
			{
				int _newLimit = Int32.Parse(_arguments["--concurrency"]);
				configService.SaveConcurrencyLimit(_newLimit).GetAwaiter().GetResult();
			}
		}

	}

}