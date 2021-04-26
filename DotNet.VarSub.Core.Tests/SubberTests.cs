using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotNet.VarSub.Core.Tests
{
	public class SubberTests
	{
		private readonly Mock<ILoggerFactory> _loggerFactoryMock = new Mock<ILoggerFactory>();
		private readonly Mock<ILogger> _loggerMock = new Mock<ILogger>();

		public SubberTests()
		{
			_loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
				.Returns(() => _loggerMock.Object);
		}

		private Subber GetService()
		{
			var subber = new Subber(_loggerFactoryMock.Object);
			return subber;
		}

		private const string SampleJson = @"
			{
				""ConnectionStrings"":{
					""ApplicationDb"": ""server=localhost;port=4321;username=devuser;password=devsarecool123"",
					""ServiceBus"": ""Endpoint=devenv.cloudservice.com;SharedSecret=KJSDLKFJSDFKJSDL;Port=1234"",
					""Cdn"": ""devenv.mycdn.com""
				},
				""Environment"": ""DEV""
			}
			";

		private const string ExpectedJson = @"{
  ""ConnectionStrings"": {
    ""ApplicationDb"": ""server=localhost;port=4321;username=devuser;password=devsarecool123"",
    ""ServiceBus"": ""Endpoint=devenv.cloudservice.com;SharedSecret=KJSDLKFJSDFKJSDL;Port=1234"",
    ""Cdn"": ""test.cdn.com""
  },
  ""Environment"": ""DEV""
}";

		private async Task<Stream> GetJsonStream()
		{
			var stream = new MemoryStream();
			var writer = new StreamWriter(stream, Encoding.UTF8);
			await writer.WriteAsync(SampleJson);
			await writer.FlushAsync();
			stream.Position = 0;
			return stream;
		}

		private async Task<string> GetStringFromStream(Stream stream)
		{
			using var reader = new StreamReader(stream, Encoding.UTF8, true, -1, true);
			return await reader.ReadToEndAsync();
		}

		[Fact]
		public async Task SubberEmitsChangedDom()
		{
			//arrange
			var subber = GetService();
			await using var jsonStream = await GetJsonStream();
			await subber.ReadDocument(jsonStream);
			const string expected = "test.cdn.com";

			//act
			subber.Sub("ConnectionStrings.Cdn", expected);
			var dom = subber.CurrentDocument();
			string actual = dom.ConnectionStrings.Cdn;
			await using var outStream = new MemoryStream();
			await subber.WriteDocument(outStream);
			outStream.Position = 0;
			var json = await GetStringFromStream(outStream);

			//assert
			Assert.True(subber.IsLoaded);
			Assert.NotNull(dom);
			Assert.Equal(expected, actual);
			Assert.NotNull(json);
			Assert.Equal(ExpectedJson, json);
		}
	}
}