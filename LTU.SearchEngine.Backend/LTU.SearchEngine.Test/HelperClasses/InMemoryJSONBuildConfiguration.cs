using Microsoft.Extensions.Configuration;
using System.Text;

namespace LTU.SearchEngine.Test.HelperClasses;

/// <summary>
/// Provides helper methods to create an <see cref="IConfiguration"/> object
/// from a JSON string in memory. Useful for unit tests where configuration
/// values need to be mocked without using physical files.
/// </summary>
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
