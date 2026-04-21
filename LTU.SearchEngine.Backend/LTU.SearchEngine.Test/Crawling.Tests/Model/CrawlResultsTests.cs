using LTU.SearchEngine.Backend.Core.Enums;
using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using LTU.SearchEngine.Test.HelperClasses;
using System.Net;
using System.Text;

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
			var content = Encoding.UTF8.GetBytes("x");
			var links = new List<string> { "https://link.com" };
			var statusCode = HttpStatusCode.OK;
			var timeTakenMs = 123;

			// Act
			var sut = CrawlResultBuilder.BuildCrawlResult(
				url: url,
				title: title,
				language: language,
				indexedTerms: terms,
				type: type,
				content: "x",
				extractedLinks: links,
				statusCode: statusCode,
				timeTakenMs: timeTakenMs,
				hashContent: "FakeHash"
			);

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
	
	
		[Fact]
		public void Constructor_ValidHtmlArguments_AssignsMetaDataCorrectly()
		{
			// Arrange
			var charSet = "UTF-8";
			var docType = "<!DOCTYPE html>";
			var htmlMetaData = new HtmlDocumentMetaData(charSet, docType);
			
			// Act
			var sut = CrawlResultBuilder.BuildCrawlResult(metaData: htmlMetaData);
			
			// Assert
			Assert.NotNull(sut.MetaData);
			Assert.Equal(DocumentMetaDataType.Html, sut.MetaData.MetaDataType);
			
			var htmlMeta = Assert.IsType<HtmlDocumentMetaData>(sut.MetaData);
			Assert.Equal(charSet, htmlMeta.CharSet); 
			Assert.Equal(docType, htmlMeta.DocType);
		}

		[Fact]
		public void Constructor_NullMetaData_ThrowsArgumentNullException()
		{
			// Act & Assert
			var ex = Assert.Throws<ArgumentNullException>(() => 
				CrawlResultBuilder.BuildCrawlResult(metaData: null!)
			);
			
			Assert.Equal("metaData", ex.ParamName);
		}

		[Theory]
		[InlineData("NULL_TEST")]
		[InlineData("")]
		[InlineData(" ")]
		public void Constructor_InvalidUrl_ThrowsArgumentException(string input)
		{
			string? invalidUrl = input.Equals("NULL_TEST") ? null : input;

			var ex = Assert.Throws<ArgumentException>(() => CrawlResultBuilder.BuildCrawlResult(url: invalidUrl!));

			Assert.Contains("Url cannot be null or whitespace", ex.Message);
		}

		[Fact]
		public void Constructor_TitleCanBeNull()
		{
			var result = CrawlResultBuilder.BuildCrawlResult(title: null!);

			Assert.Null(result.Title);
		}


		[Theory]
		[InlineData("NULL_TEST")]
		[InlineData("")]
		[InlineData(" ")]
		public void Constructor_InvalidLanguage_ThrowsArgumentException(string input)
		{

			string? invalidLanguage = input.Equals("NULL_TEST") ? null : input;

			var ex = Assert.Throws<ArgumentException>(() => 
				CrawlResultBuilder.BuildCrawlResult(language: invalidLanguage!)
			);

			Assert.Contains("Language cannot be null or whitespace.", ex.Message);
		}

		
		[Fact]
		public void Constructor_NullIndexedTerms_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => 
				CrawlResultBuilder.BuildCrawlResult(indexedTerms: null!, extractedLinks: new List<string>())
			);
		}

		[Theory]
		[InlineData("NULL_TEST")]
		[InlineData("")]
		[InlineData(" ")]
		public void Constructor_InvalidType_ThrowsArgumentException(string input)
		{

			string? invalidType = input.Equals(input) ? null : input;

			var ex = Assert.Throws<ArgumentException>(() => CrawlResultBuilder.BuildCrawlResult(type: invalidType!));

			Assert.Contains("Type cannot be null or whitespace.", ex.Message);
		}

		[Fact]
		public void Constructor_ContentNull_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => CrawlResultBuilder.BuildCrawlResult(content: null!));
		}

		[Fact]
		public void Constructor_NullExtractedLinks_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => 
				CrawlResultBuilder.BuildCrawlResult(extractedLinks: null!, indexedTerms: new List<IndexedTerm>())
			);
		}

		[Theory]
		[InlineData("NULL_TEST")]
		[InlineData("")]
		[InlineData(" ")]
		public void Constructor_InvalidContentHash_ThrowsArgumentNullException(string input)
		{
			string? invalidContentHash = input.Equals("NULL_TEST") ? null : input;

			Assert.Throws<ArgumentException>(() => 
				CrawlResultBuilder.BuildCrawlResult(hashContent: invalidContentHash!)
			);
		}


		[Fact]
		public void Constructor_NegativeTimeTaken_ThrowsArgumentOutOfRangeException()
		{
			var ex = Assert.Throws<ArgumentOutOfRangeException>(
				() => CrawlResultBuilder.BuildCrawlResult(timeTakenMs: -1
			));

			Assert.Contains("TimeTakenMs cannot be negative.", ex.Message);
		}

		[Fact]
		public void Constructor_DefensiveCopy_IndexedTermsAndLinksAreImmutable()
		{
			// Arrange
			var terms = new List<IndexedTerm> { new IndexedTerm("term1", TermSource.Header) };
			var links = new List<string> { "https://link.com" };

			var result = CrawlResultBuilder.BuildCrawlResult(
				url: "https://example.com", 
				title: "title", 
				language: "en",
				indexedTerms: terms, 
				type: "text/html",
				content: "x",
				extractedLinks: links, 
				statusCode: HttpStatusCode.OK, 
				timeTakenMs:100
			);

			// Modify original lists
			terms.Add(new IndexedTerm("term2", TermSource.Body));
			links.Add("https://link2.com");

			// Assert the object's lists did not change
			Assert.Single(result.IndexedTerms);
			Assert.Single(result.ExtractedLinks);
		}
	}
}
