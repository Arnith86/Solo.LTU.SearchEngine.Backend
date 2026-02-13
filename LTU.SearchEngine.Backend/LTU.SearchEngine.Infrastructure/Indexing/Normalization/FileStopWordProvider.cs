using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Indexing.Normalization
{
    public class FileStopWordProvider : IStopWordProvider
    {
        private readonly HashSet<string> _stopWords;

        public FileStopWordProvider(string filePath) 
        {
            if(string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty. ",nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Stopword file not found.", filePath);

            _stopWords = new HashSet<string>();

            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                var word = line.Trim();
                if(string.IsNullOrWhiteSpace(word))
                    { continue; }
                word = word.ToLowerInvariant();
                _stopWords.Add(word);
            }
        }
        
        public HashSet<string> GetStopWords()
        {
            return _stopWords; 
        }
    }
}
