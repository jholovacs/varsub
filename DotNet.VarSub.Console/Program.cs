using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using DotNet.VarSub.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace DotNet.VarSub.Console
{
	public class Program
	{
		private static ILoggerFactory _loggerFactory;
		private static ILogger _logger;

		public static int Main(string[] args)
		{
			var program = new Program();
			return program.Run(args);
		}

		private int Run(string[] args)
		{
			try
			{
				var rootCommand = GenerateRootCommand();

				rootCommand.Handler = CommandHandler.Create<FileInfo, string, string, LogLevel>(
					async (file, parameterPath, value, logLevel) =>
					{
						try
						{
							ConfigureLogging(logLevel);
							_logger = _loggerFactory.CreateLogger<Program>();

							_logger.LogDebug($"File Location: '{file.FullName}'");
							_logger.LogDebug($"Parameter Path: '{parameterPath}'");
							_logger.LogDebug($"Value: '{new string('*', value.Length)}' (masked for security)");
							_logger.LogDebug($"Log Level: '{logLevel}'");

							if (!file.Exists) throw new FileNotFoundException($"File not found at '{file.FullName}'");

							await using var readStream = File.OpenRead(file.FullName);
							var subber = new Subber(_loggerFactory);

							await subber.ReadDocument(readStream);
							subber.Sub(parameterPath, value);

							await using var writeStream = File.Open(file.FullName, FileMode.Create, FileAccess.Write);
							await subber.WriteDocument(writeStream);

							_logger.LogInformation(
								$"Updated the value for parameter '{parameterPath}' in file '{file.FullName}'.");
							return 0;
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Cannot run the variable substitution. The exception is included.");
							return -1;
						}
					});

				return rootCommand.InvokeAsync(args).Result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to execute command line. The exception is included.");
				return -1;
			}
		}

		private RootCommand GenerateRootCommand()
		{
			var version = GetType().Assembly.GetName().Version;
			var rootCommand = new RootCommand
			{
				new Option<FileInfo>(new[] {"--file", "-f"}, "Path to the UTF-8 encoded JSON file"),
				new Option<string>(new[] {"--parameter-path", "-p"}, "JSON path of the parameter in the file."),
				new Option<string>(new[] {"--value", "-v"}, "The new value to replace with the existing value."),
				new Option<LogLevel>(new[] {"--log-level", "-l"}, () => LogLevel.Information, "Logging level."),
			};

			rootCommand.Description = $"Variable Substitution Tool v{version}";
			rootCommand.Name = "varsub";
			rootCommand.TreatUnmatchedTokensAsErrors = true;
			return rootCommand;
		}

		private void ConfigureLogging(LogLevel logLevel)
		{
			try
			{
				_loggerFactory = LoggerFactory.Create(config =>
				{
					config.AddSimpleConsole(consoleConfig =>
					{
						consoleConfig.ColorBehavior = LoggerColorBehavior.Enabled;
						consoleConfig.TimestampFormat = "yyyy-MM-ddThh:mm:ss ";
						consoleConfig.UseUtcTimestamp = true;
						consoleConfig.IncludeScopes = true;
						consoleConfig.SingleLine = false;
					});
					config.Configure(builder => { });
					config.SetMinimumLevel(logLevel);
				});
			}
			catch (Exception ex)
			{
				//can't log this as the error occurred setting up logging. Just emit to stderr
				System.Console.Error.WriteLine(
					"Failed to configure logging mechanism.  The exception is as follows: \r\n{0}", ex);
				throw;
			}
		}
	}
}
