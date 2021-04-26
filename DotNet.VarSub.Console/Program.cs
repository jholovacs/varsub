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
		public static int Main(string[] args)
		{
			var rootCommand = new RootCommand
			{
				new Option<FileInfo>("--file", "path to the UTF-8 encoded JSON file"),
				new Option<string>("--parameterPath", "path of the parameter in the JSON file."),
				new Option<string>("--value", "the new value to replace with the existing value.")
			};

			rootCommand.Description = "variable substitution tool";
			rootCommand.Name = "varsub";

			rootCommand.Handler = CommandHandler.Create<FileInfo, string, string>(async (file, parameterName, value) =>
			{
				var loggingFactory = ConfigureLogging();
				var logger = loggingFactory.CreateLogger<Program>();
				try
				{
					await using var readStream = File.OpenRead(file.FullName);
					var subber = new Subber(loggingFactory);

					await subber.ReadDocument(readStream);
					subber.Sub(parameterName, value);

					await using var writeStream = File.OpenWrite(file.FullName);
					await subber.WriteDocument(writeStream);

					logger.LogInformation(
						$"Updated the value for parameter '{parameterName}' in file '{file.FullName}'.");
					return 0;
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Cannot run the variable substitution. The exception is included.");
					return -1;
				}
			});

			return rootCommand.InvokeAsync(args).Result;
		}

		private static ILoggerFactory ConfigureLogging()
		{
			var loggingFactory = LoggerFactory.Create(config =>
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
			return loggingFactory;
		}
	}
}