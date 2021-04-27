using System;
using System.Globalization;
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
				_logger.LogDebug($"Loaded {jsonText.Length} bytes of text from the data stream.");
				_jsonDocument = JObject.Parse(jsonText);
				_logger.LogDebug($"JSON DOM loaded, {_jsonDocument.Count} child tokens discovered.");
				IsLoaded = true;
				_logger.LogDebug("IsLoaded=true");
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
				await using var textWriter = new StreamWriter(jsonOut, Encoding.UTF8, 2048, true);
				using var jsonWriter = new JsonTextWriter(textWriter) { Formatting = Formatting.Indented };
				await _jsonDocument.WriteToAsync(jsonWriter);
				_logger.LogDebug("JSON DOM has been written to the data stream.");
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
			_logger.LogTrace($"Navigating token path '{variablePath}'...");
			var token = _jsonDocument.SelectToken(variablePath);
			if (token != null)
			{
				_logger.LogTrace($"Token discovered in DOM, of type {token.Type}.");
				_logger.LogTrace($"Token parent is of type '{token.Parent?.Type}'");
				token.Replace(ConvertSimpleTypes(token, value));
			}
			else
			{
				_logger.LogWarning($"Could not find a value at '{variablePath}' to replace.");
			}
		}

		private JToken ConvertSimpleTypes(JToken token, string newValue)
		{
			switch (token.Type)
			{
				case JTokenType.Integer:
					if (int.TryParse(newValue, out var intResult))
					{
						_logger.LogDebug($"JSON token is an integer type, as is the new value. Not quoting.");
						return JToken.FromObject(intResult);
					}
					break;
				case JTokenType.Float:
					if (double.TryParse(newValue, out var dblResult))
					{
						_logger.LogDebug("JSON token is a float type, as is the new value.  Not quoting.");
						return JToken.FromObject(dblResult);
					}
					break;
				case JTokenType.Boolean:
					if (bool.TryParse(newValue, out var boolResult))
					{
						_logger.LogDebug("JSON token is a boolean type, as is the new value.  Not quoting.");
						return JToken.FromObject(boolResult);
					}
					break;
				case JTokenType.Date:
					if (DateTime.TryParseExact(newValue, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None,
						out var dateTimeResult))
					{
						_logger.LogDebug("JSON token is a datetime type, as is the new value. Formatting for ISO 8601.");
						return JToken.FromObject(dateTimeResult);
					}
					break;
			}

			return JToken.FromObject(newValue);
		}

		private static readonly string[] Formats = { 
			// Basic formats
			"yyyyMMddTHHmmsszzz",
			"yyyyMMddTHHmmsszz",
			"yyyyMMddTHHmmssZ",
			// Extended formats
			"yyyy-MM-ddTHH:mm:sszzz",
			"yyyy-MM-ddTHH:mm:sszz",
			"yyyy-MM-ddTHH:mm:ssZ",
			// All of the above with reduced accuracy
			"yyyyMMddTHHmmzzz",
			"yyyyMMddTHHmmzz",
			"yyyyMMddTHHmmZ",
			"yyyy-MM-ddTHH:mmzzz",
			"yyyy-MM-ddTHH:mmzz",
			"yyyy-MM-ddTHH:mmZ",
			// Accuracy reduced to hours
			"yyyyMMddTHHzzz",
			"yyyyMMddTHHzz",
			"yyyyMMddTHHZ",
			"yyyy-MM-ddTHHzzz",
			"yyyy-MM-ddTHHzz",
			"yyyy-MM-ddTHHZ"
		};

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