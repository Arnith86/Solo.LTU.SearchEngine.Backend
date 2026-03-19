
using LTU.SearchEngine.Backend.Core.Model.DTOs;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Infrastructure.Configuration;
using LTU.SearchEngine.Test.HelperClasses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;

namespace LTU.SearchEngine.Test.Configuration.Tests;

public class JsonCrawlerSettingsLoaderTests
{
	private string _validJsonConfig;
	private const string _c_userAgent = "LTUSearchCrawler / 1.0(Academic project; contact: some.mail @student.ltu.se)";
	private const int _c_maxConcurrencyPerDomain = 2;
	private const int _c_minDelayMs = 500;
	private const string _c_retryIntervals = "[\"01:00:00\", \"1.00:00:00\", \"7.00:00:00\"]";

	public JsonCrawlerSettingsLoaderTests()
	{
		_validJsonConfig = $$"""
		{
			"CrawlerSettings": {
				"UserAgent": "{{_c_userAgent}}",
				"MaxConcurrencyPerDomain": {{_c_maxConcurrencyPerDomain}},
				"MinDelayMs": {{_c_minDelayMs}},
				"RetryIntervals":  {{_c_retryIntervals}},
				"seedUrls": [
			    "ltu.se",
		        "www.ltu.se"
		    	],
				"WhiteList":[
				"ltu.se"
				]
			}
		}
		""";
	}



	[Fact]
	public void Load_ValidConfiguration_ShouldReturnCrawlerSettings()
	{
		// Arrange
		IConfiguration configuration = InMemoryJSONBuildConfiguration.BuildConfiguration(_validJsonConfig);
		var dto = configuration.GetSection("CrawlerSettings").Get<CrawlerSettingsDTO>();
		var monitor = Options.Create(dto!);

		var mockMonitor = new Mock<IOptionsMonitor<CrawlerSettingsDTO>>();
		mockMonitor.Setup(m => m.CurrentValue).Returns(dto!);

		ICrawlerSettingsLoader sut = new JsonCrawlerSettingsLoader(mockMonitor.Object);
	
		// Act
		CrawlerSettings crawlerSettings = sut.Load();

		IReadOnlyList<TimeSpan> retryIntervalsList = new List<TimeSpan>
		{
			TimeSpan.FromSeconds(3600),    // 01:00:00		// 1 hour
			TimeSpan.FromSeconds(86400),   // 1.00:00:00	// 1 day
			TimeSpan.FromSeconds(604800)   // 7.00:00:00	// 1 week
		};

        var expectedSeedUrls = new List<string> { "ltu.se", "www.ltu.se" };

        // Assert
        Assert.Equal(_c_userAgent, crawlerSettings.UserAgent);
		Assert.Equal(_c_maxConcurrencyPerDomain, crawlerSettings.MaxConcurrencyPerDomain);
		Assert.Equal(_c_minDelayMs, crawlerSettings.MinDelayMs);
		Assert.Equal(retryIntervalsList, crawlerSettings.RetryIntervals);
        Assert.Equal(expectedSeedUrls, crawlerSettings.SeedUrls);
    }
	
	
	
	[Fact]
	public void Load_HotSwapSettings_ShouldReturnDifferentCrawlerSettings()
	{
		// Arrange

		// First version
		IReadOnlyList<TimeSpan> expectedRetryIntervalsList = new List<TimeSpan>
		{
			TimeSpan.FromSeconds(3600),    // 01:00:00		// 1 hour
			TimeSpan.FromSeconds(86400),   // 1.00:00:00	// 1 day
			TimeSpan.FromSeconds(604800)   // 7.00:00:00	// 1 week
		};

 		var expectedSeedUrls1 = new List<string> { "ltu.se", "www.ltu.se" };
 		var expectedWhiteListDomains1 = new List<string> { "ltu.se" };
		
		IConfiguration configuration = InMemoryJSONBuildConfiguration.BuildConfiguration(_validJsonConfig);
		var dto = configuration.GetSection("CrawlerSettings").Get<CrawlerSettingsDTO>();


		// Second version 
		string secondAgent = "secondAgent";
		int second_maxConcurrencyPerDomain = 1;
		int second_minDelayMs = 3000;
		string second_retryIntervals = "[\"02:00:00\", \"2.00:00:00\", \"17.00:00:00\"]";
		
		var secondConfig = $$"""
		{
			"CrawlerSettings": {
				"UserAgent": "{{secondAgent}}",
				"MaxConcurrencyPerDomain": {{second_maxConcurrencyPerDomain}},
				"MinDelayMs": {{second_minDelayMs}},
				"RetryIntervals":  {{second_retryIntervals}},
				"seedUrls": [
			    "umu.se",
		        "www.umu.se"
		    	],
				"WhiteList":[
				"umu.se"
				]
			}
		}
		""";

		IReadOnlyList<TimeSpan> expectedRetryIntervalsList2 = new List<TimeSpan>
		{
			TimeSpan.FromHours(2),          // 02:00:00 (2 timmar)
			TimeSpan.FromDays(2),           // 2.00:00:00 (2 dagar)
			TimeSpan.FromDays(17)           // 17.00:00:00 (17 dagar)
		};

		var expectedSeedUrls2 = new List<string> { "umu.se", "www.umu.se" };
		var expectedWhiteListDomains2 = new List<string> { "umu.se" };


		IConfiguration configuration2 = InMemoryJSONBuildConfiguration.BuildConfiguration(secondConfig);
		var dto2 = configuration2.GetSection("CrawlerSettings").Get<CrawlerSettingsDTO>();

		var mockMonitor = new Mock<IOptionsMonitor<CrawlerSettingsDTO>>();
		
		mockMonitor.SetupSequence(m => m.CurrentValue)
			.Returns(dto!)
			.Returns(dto2!);

		// var monitor = Options.Create(dto!);
		
		ICrawlerSettingsLoader sut = new JsonCrawlerSettingsLoader(mockMonitor.Object);
				

		// Act
		CrawlerSettings crawlerSettings1 = sut.Load();
		CrawlerSettings crawlerSettings2 = sut.Load();     

        // Assert
		// First
        Assert.Equal(_c_userAgent, crawlerSettings1.UserAgent);
		Assert.Equal(_c_maxConcurrencyPerDomain, crawlerSettings1.MaxConcurrencyPerDomain);
		Assert.Equal(_c_minDelayMs, crawlerSettings1.MinDelayMs);
		Assert.Equal(expectedRetryIntervalsList, crawlerSettings1.RetryIntervals);
        Assert.Equal(expectedSeedUrls1, crawlerSettings1.SeedUrls);
		Assert.Equal(expectedWhiteListDomains1, crawlerSettings1.WhiteList);

		// Second
		Assert.Equal(secondAgent, crawlerSettings2.UserAgent);
		Assert.Equal(second_maxConcurrencyPerDomain, crawlerSettings2.MaxConcurrencyPerDomain);
		Assert.Equal(second_minDelayMs, crawlerSettings2.MinDelayMs);
		Assert.Equal(expectedRetryIntervalsList2, crawlerSettings2.RetryIntervals);
        Assert.Equal(expectedSeedUrls2, crawlerSettings2.SeedUrls);
		Assert.Equal(expectedWhiteListDomains2, crawlerSettings2.WhiteList);

    }

	
	[Fact]
	public void Load_InValidConfiguration_ShouldThrow_InvalidOperationException()
	{
		// Arrange
		string invalidJsonConfig = $$"""
			{
				"SomeSettings": {}
			}
			""";

		IConfiguration configuration = InMemoryJSONBuildConfiguration.BuildConfiguration(invalidJsonConfig);

		var dto = configuration.GetSection("CrawlerSettings").Get<CrawlerSettingsDTO>();
		var monitor = Options.Create(dto!);

		var mockMonitor = new Mock<IOptionsMonitor<CrawlerSettingsDTO>>();
		mockMonitor.Setup(m => m.CurrentValue).Returns(dto!);

		ICrawlerSettingsLoader sut = new JsonCrawlerSettingsLoader(mockMonitor.Object);


		// Act & Assert
		Assert.Throws<InvalidOperationException>(() => sut.Load());
	}
}
