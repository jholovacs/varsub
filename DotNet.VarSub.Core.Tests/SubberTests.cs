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

		private async Task<Stream> GetJsonStream()
		{
			var stream = new MemoryStream();
			var writer = new StreamWriter(stream, Encoding.UTF8);
			await writer.WriteAsync(SampleJson);
			await writer.FlushAsync();
			stream.Position = 0;
			return stream;
		}

		[Fact]
		public void SubberInstantiates()
		{
			//arrange
			var subber = GetService();

			//act
			//yeah this is just making sure everything builds ok and the service exists.  No action to take.

			//assert
			Assert.NotNull(subber);
		}

		[Fact]
		public async Task SubberLoadsJson()
		{
			//arrange
			var subber = GetService();
			await using var jsonStream = await GetJsonStream();

			//act
			await subber.ReadDocument(jsonStream);

			//assert
			Assert.True(subber.IsLoaded);
		}

		[Fact]
		public async Task SubstitutionHappens()
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

			//assert
			Assert.NotNull(dom);
			Assert.Equal(expected, actual);
		}
	}
}