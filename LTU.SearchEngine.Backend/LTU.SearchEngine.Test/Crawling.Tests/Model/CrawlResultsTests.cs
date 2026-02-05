using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System.Net;

namespace LTU.SearchEngine.Test.Crawling.Tests.Model;

public class CrawlResultsTests
{
	public class CrawlResultTests
	{
		[Fact]
		public void Constructor_ValidArguments_CreatesObject()
		{
			// Arrange
			var url = "https://example.com";
			var title = "Example Page";
			var language = "en";
			var terms = new List<IndexedTerm> { new IndexedTerm("test", TermSource.Title) };
			var type = "text/html";
			var content = new byte[] { 1, 2, 3 };
			var links = new List<string> { "https://link.com" };
			var statusCode = HttpStatusCode.OK;
			var timeTakenMs = 123;

			// Act
			var sut = new CrawlResult(url, title, language, terms, type, content, links, statusCode, timeTakenMs);

			// Assert
			Assert.Equal(url, sut.Url);
			Assert.Equal(title, sut.Title);
			Assert.Equal(language, sut.Language);
			Assert.Equal(terms, sut.IndexedTerms);
			Assert.Equal(type, sut.Type);
			Assert.Equal(content, sut.Content);
			Assert.Equal(links, sut.ExtractedLinks);
			Assert.Equal(statusCode, sut.StatusCode);
			Assert.Equal(timeTakenMs, sut.TimeTakenMs);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData(" ")]
		public void Constructor_InvalidUrl_ThrowsArgumentException(string invalidUrl)
		{
			var ex = Assert.Throws<ArgumentException>(() => new CrawlResult(
				invalidUrl, 
				"title", 
				"en",
				new List<IndexedTerm>(), 
				"text/html", 
				new byte[0],
				new List<string>(), 
				HttpStatusCode.OK, 
				10)
			);

			Assert.Contains("Url cannot be null or whitespace", ex.Message);
		}

		[Fact]
		public void Constructor_TitleCanBeNull()
		{
			var result = new CrawlResult(
				"https://example.com", 
				null, 
				"en",
				new List<IndexedTerm>(), 
				"text/html", 
				new byte[0],
				new List<string>(), 
				HttpStatusCode.OK, 
				10
			);

			Assert.Null(result.Title);
		}


		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData(" ")]
		public void Constructor_InvalidLanguage_ThrowsArgumentException(string invalidLanguage)
		{
			var ex = Assert.Throws<ArgumentException>(() => new CrawlResult(
				"https://example.com",
				"title",
				invalidLanguage,
				new List<IndexedTerm>(),
				"text/html",
				new byte[0],
				new List<string>(),
				HttpStatusCode.OK,
				10)
			);

			Assert.Contains("Language cannot be null or whitespace.", ex.Message);
		}

		
		[Fact]
		public void Constructor_NullIndexedTerms_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => new CrawlResult(
				"https://example.com", 
				"title", 
				"en",
				null!,
				"text/html", 
				new byte[0],
				new List<string>(), 
				HttpStatusCode.OK, 
				10)
			);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData(" ")]
		public void Constructor_InvalidType_ThrowsArgumentException(string invalidType)
		{
			var ex = Assert.Throws<ArgumentException>(() => new CrawlResult(
				"https://example.com",
				"title",
				"en",
				new List<IndexedTerm>(),
				invalidType,
				new byte[0],
				new List<string>(),
				HttpStatusCode.OK,
				10)
			);

			Assert.Contains("Type cannot be null or whitespace.", ex.Message);
		}

		[Fact]
		public void Constructor_ContentNull_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => new CrawlResult(
				"https://example.com",
				"title",
				"en",
				new List<IndexedTerm>(),
				"text/html",
				null!,
				new List<string>(),
				HttpStatusCode.OK,
				10)
			);
		}

		[Fact]
		public void Constructor_NullExtractedLinks_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => new CrawlResult(
				"https://example.com", 
				"title", 
				"en",
				new List<IndexedTerm>(), 
				"text/html", 
				new byte[0],
				null!, 
				HttpStatusCode.OK, 
				10)
			);
		}

		[Fact]
		public void Constructor_NegativeTimeTaken_ThrowsArgumentOutOfRangeException()
		{
			var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new CrawlResult(
				"https://example.com", 
				"title",
				"en",
				new List<IndexedTerm>(), 
				"text/html", 
				new byte[0],
				new List<string>(), 
				HttpStatusCode.OK, 
				-1)
			);

			Assert.Contains("TimeTakenMs cannot be negative.", ex.Message);
		}

		[Fact]
		public void Constructor_DefensiveCopy_IndexedTermsAndLinksAreImmutable()
		{
			// Arrange
			var terms = new List<IndexedTerm> { new IndexedTerm("term1", TermSource.Header) };
			var links = new List<string> { "https://link.com" };

			var result = new CrawlResult(
				"https://example.com", "title", "en",
				terms, "text/html", new byte[0],
				links, HttpStatusCode.OK, 100);

			// Modify original lists
			terms.Add(new IndexedTerm("term2", TermSource.Body));
			links.Add("https://link2.com");

			// Assert the object's lists did not change
			Assert.Single(result.IndexedTerms);
			Assert.Single(result.ExtractedLinks);
		}
	}
}
