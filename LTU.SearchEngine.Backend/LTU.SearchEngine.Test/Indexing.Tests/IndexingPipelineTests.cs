using LTU.SearchEngine.Backend.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LTU.SearchEngine.Test.Indexing.Tests
{
    public class IndexingPipelineTests
    {
        
        //Define Expected Behavior ชัด ๆ
        //Pipeline ต้องทำ:
        //รับ CrawlResult
        //เอา Terms ออกมา
        //Normalize
        //Group ตาม TermSource
        //Count frequency
        //Return IndexDocument
    }
}
