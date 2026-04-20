using LTU.SearchEngine.Backend.Core.Enums;

namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

public abstract class DocumentMetaData
{
    public DocumentMetaDataType MetaDataType { get; protected set; }
    
    protected DocumentMetaData(DocumentMetaDataType type)
    {
        MetaDataType = type;
    }
}

public class HtmlDocumentMetaData : DocumentMetaData
{
    public string CharSet { get; }
    public string DocType { get; }

    public HtmlDocumentMetaData(string charSet, string docType) : base (type : DocumentMetaDataType.Html)
    {
        CharSet = charSet;
        DocType = docType;
    }
}

public class PdfDocumentMetaData : DocumentMetaData
{
    public string PdfVersion { get; }
    public string EncodingType { get; }

    public PdfDocumentMetaData(string pdfVersion, string encodingType) : base (type: DocumentMetaDataType.Pdf)
    {
        PdfVersion = pdfVersion;
        EncodingType = encodingType;
    }
}
