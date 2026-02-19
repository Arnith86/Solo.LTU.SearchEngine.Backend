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
            var url = "https://www.ltu.se/education/programmes/software-engineering";

            return new IndexDocument(
                docId: url,
                url: url,
                title: "Software Engineering Programme"
            );
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
        public void AddTerm_GivenUnknownTermSource_ShouldDoNothing()
        {
            var document = CreateDocument();

            document.AddTerm("x", (TermSource)999);

            Assert.Empty(document.TitleTerms);
            Assert.Empty(document.HeaderTerms);
            Assert.Empty(document.ContentTerms);
        }
    }
}
