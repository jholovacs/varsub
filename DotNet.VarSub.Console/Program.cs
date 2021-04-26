using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DotNet.VarSub.Core;
using Microsoft.Extensions.Logging;

namespace DotNet.VarSub.Console
{
	class Program
	{
		static async Task Main(FileInfo file, 
			string parameterName, 
			string value)
		{
			var loggingFactory = ConfigureLogging();
			var logger = loggingFactory.CreateLogger<Program>();
			await using var readStream = File.OpenRead(file.FullName);
			var subber = new Subber(loggingFactory);

			await subber.ReadDocument(readStream);
			subber.Sub(parameterName, value);

			await using var writeStream = File.OpenWrite(file.FullName);
			await subber.WriteDocument(writeStream);

			logger.LogInformation($"Updated the value for parameter '{parameterName}' in file '{file.FullName}'.");
		}

		static ILoggerFactory ConfigureLogging()
		{
			var loggingFactory = LoggerFactory.Create(config =>
			{
				config.AddConsole(consoleConfig =>
				{
					consoleConfig.LogToStandardErrorThreshold = LogLevel.Information;
				});
				config.Configure(builder => { });
			});
			return loggingFactory;
		}
	}
}
