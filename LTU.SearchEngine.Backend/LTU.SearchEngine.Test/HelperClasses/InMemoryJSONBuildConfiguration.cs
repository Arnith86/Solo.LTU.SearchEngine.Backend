using Microsoft.Extensions.Configuration;
using System.Text;

namespace LTU.SearchEngine.Test.HelperClasses;

public class InMemoryJSONBuildConfiguration
{
	public static IConfiguration BuildConfiguration(string json)
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
		return new ConfigurationBuilder()
			.AddJsonStream(stream)
			.Build();
	}
}
