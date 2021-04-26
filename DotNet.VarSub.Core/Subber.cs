using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotNet.VarSub.Core
{
	/// <summary>
	///     Responsible for the basic variable substitution
	/// </summary>
	public class Subber
	{
		private readonly ILogger _logger;
		public bool IsLoaded { get; private set; }
		private JObject _jsonDocument;

		public Subber(ILoggerFactory loggerFactory)
		{
			_logger = loggerFactory.CreateLogger<Subber>();
		}

		/// <summary>
		///     Loads the JSON data as an object from a UTF-8 encoded readable data stream.
		/// </summary>
		/// <param name="jsonData"></param>
		/// <returns></returns>
		public async Task ReadDocument(Stream jsonData)
		{
			try
			{
				using var reader = new StreamReader(jsonData, Encoding.UTF8);
				var jsonText = await reader.ReadToEndAsync();
				_jsonDocument = JObject.Parse(jsonText);
				IsLoaded = true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex,
					"Failed to load JSON file for variable substitution. See the exception for more details.");
			}
		}

		/// <summary>
		///     Writes the modified JSON to the provided writable stream as UTF-8 encoded data.
		/// </summary>
		/// <param name="jsonOut"></param>
		/// <returns></returns>
		public async Task WriteDocument(Stream jsonOut)
		{
			try
			{
				await using var textWriter = new StreamWriter(jsonOut);
				using var jsonWriter = new JsonTextWriter(textWriter) {Formatting = Formatting.Indented};
				await _jsonDocument.WriteToAsync(jsonWriter);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to write the JSON stream. See the exception for details.");
			}
		}

		/// <summary>
		///     Substitutes parameter values based upon the variable path.
		/// </summary>
		/// <param name="variablePath"></param>
		/// <param name="value"></param>
		public void Sub(string variablePath, string value)
		{
			if (!IsLoaded) throw new InvalidOperationException("No document loaded.  Cannot sub a blank document.");
			var token = _jsonDocument.SelectToken(variablePath);
			if (token != null)
			{
				token.Replace(value);
			}
			else
			{
				_logger.LogWarning($"Could not find a value at '{variablePath}' to replace.");
			}
		}

		/// <summary>
		/// Allows the end user to browse the JSON document in its current state.
		/// </summary>
		/// <returns></returns>
		public dynamic CurrentDocument()
		{
			dynamic rVal = _jsonDocument;
			return rVal;
		}
	}
}