using LTU.SearchEngine.Backend.Core.Model;
using LTU.SearchEngine.Backend.Core.Model.ValueObjects;
using System;
using Xunit;

namespace LTU.SearchEngine.Test.Indexing.Tests.Model
{
    public class IndexDocumentTests
    {
        private IndexDocument CreateDocument()
        {
            var id = "test-guid-id";
            var url = "https://www.ltu.se/education/programmes/software-engineering";

            return new IndexDocument(
                docId: id,
                url: url,
                title: "Software Engineering Programme"
            );
        }

        [Fact]
        public void Constructor_ShouldAssignPropertiesCorrectly()
        {
            var id = "id-123";
            var url = "https://test.com";
            var title = "Test Title";

            var document = new IndexDocument(id, url, title);

            Assert.Equal(id, document.DocId);
            Assert.Equal(url, document.Url);
            Assert.Equal(title, document.Title);

            Assert.Empty(document.TitleTerms);
            Assert.Empty(document.HeaderTerms);
            Assert.Empty(document.ContentTerms);
        }

        [Fact]
        public void AddTerm_GivenNullTerm_ShouldThrowArgumentNullException()
        {
            var document = CreateDocument();

            Assert.Throws<ArgumentNullException>(() =>
                document.AddTerm(null, TermSource.Title));
        }

        [Fact]
        public void AddTerm_GivenNewTitleTerm_ShouldStartFrequencyAtOne()
        {
            var document = CreateDocument();

            document.AddTerm("c++", TermSource.Title);

            Assert.Equal(1, document.TitleTerms["c++"]);
            Assert.Single(document.TitleTerms);
        }

        [Fact]
        public void AddTerm_GivenExistingTitleTerm_ShouldIncrementFrequency()
        {
            var document = CreateDocument();

            document.AddTerm("c++", TermSource.Title);
            document.AddTerm("c++", TermSource.Title);

            Assert.Equal(2, document.TitleTerms["c++"]);
            Assert.Single(document.TitleTerms);
        }

        [Fact]
        public void AddTerm_GivenExistingBodyTerm_ShouldIncrementFrequency()
        {
            var document = CreateDocument();

            document.AddTerm("run", TermSource.Body);
            document.AddTerm("run", TermSource.Body);

            Assert.Equal(2, document.ContentTerms["run"]);
        }

        [Fact]
        public void AddTerm_GivenBodyTerm_ShouldOnlyExistInContentTerms()
        {
            var document = CreateDocument();

            document.AddTerm("run", TermSource.Body);

            Assert.True(document.ContentTerms.ContainsKey("run"));
            Assert.False(document.TitleTerms.ContainsKey("run"));
            Assert.False(document.HeaderTerms.ContainsKey("run"));
            Assert.Single(document.ContentTerms);
        }

        [Fact]
        public void AddTerm_GivenNewHeaderTerm_ShouldStartFrequencyAtOne()
        {
            var document = CreateDocument();

            document.AddTerm("api", TermSource.Header);

            Assert.Equal(1, document.HeaderTerms["api"]);
            Assert.Single(document.HeaderTerms);
        }
        [Fact]
        public void AddTerm_GivenExistingHeaderTerm_ShouldIncrementFrequency()
        {
            var document = CreateDocument();

            document.AddTerm("api", TermSource.Header);
            document.AddTerm("api", TermSource.Header);

            Assert.Equal(2, document.HeaderTerms["api"]);
        }

        [Fact]
        public void AddTerm_SameWordDifferentSources_ShouldStoreSeparately()
        {
            var document = CreateDocument();

            document.AddTerm("engineer", TermSource.Title);
            document.AddTerm("engineer", TermSource.Body);

            Assert.Equal(1, document.TitleTerms["engineer"]);
            Assert.Equal(1, document.ContentTerms["engineer"]);
        }
        [Fact]
        public void AddTerm_GivenEmptyString_ShouldThrow()
        {
            var document = CreateDocument();

            Assert.Throws<ArgumentException>(() =>
                document.AddTerm("", TermSource.Title));
        }

        [Fact]
        public void AddTerm_GivenWhitespace_ShouldThrow()
        {
            var document = CreateDocument();

            Assert.Throws<ArgumentException>(() =>
                document.AddTerm(" ", TermSource.Title));
        }
        [Fact]
        public void AddTerm_GivenUnknownTermSource_ShouldThrow()
        {
            var document = CreateDocument();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                document.AddTerm("x", (TermSource)999));
        }
    }
}
