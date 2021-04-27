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
			try
			{
				ConfigureLogging();
				_logger = _loggerFactory.CreateLogger<Program>();

				var rootCommand = GenerateRootCommand();

				rootCommand.Handler = CommandHandler.Create<FileInfo, string, string>(
					async (file, parameterName, value) =>
					{
						try
						{
							await using var readStream = File.OpenRead(file.FullName);
							var subber = new Subber(_loggerFactory);

							await subber.ReadDocument(readStream);
							subber.Sub(parameterName, value);

							await using var writeStream = File.OpenWrite(file.FullName);
							await subber.WriteDocument(writeStream);

							_logger.LogInformation(
								$"Updated the value for parameter '{parameterName}' in file '{file.FullName}'.");
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

		private static RootCommand GenerateRootCommand()
		{
			var rootCommand = new RootCommand
			{
				new Option<FileInfo>("--file", "path to the UTF-8 encoded JSON file"),
				new Option<string>("--parameterPath", "path of the parameter in the JSON file."),
				new Option<string>("--value", "the new value to replace with the existing value.")
			};

			rootCommand.Description = "variable substitution tool";
			rootCommand.Name = "varsub";
			return rootCommand;
		}

		private static void ConfigureLogging()
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