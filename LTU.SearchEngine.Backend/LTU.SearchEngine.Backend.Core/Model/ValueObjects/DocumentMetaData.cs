using LTU.SearchEngine.Backend.Core.Enums;

namespace LTU.SearchEngine.Backend.Core.Model.ValueObjects;

/// <summary>
/// Provides a base class for document-specific metadata.
/// This is a Value Object used to store technical details about different file formats.
/// </summary>
public abstract class DocumentMetaData
{
    /// <summary>
    /// Gets the type discriminator that identifies the format of the document metadata.
    /// </summary>
    public DocumentMetaDataType MetaDataType { get; protected set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentMetaData"/> class.
    /// </summary>
    /// <param name="type">The specific type of document metadata being created.</param>
    protected DocumentMetaData(DocumentMetaDataType type)
    {
        MetaDataType = type;
    }
}

/// <summary>
/// Contains metadata specific to HTML documents, such as character encoding and document type.
/// </summary>
public class HtmlDocumentMetaData : DocumentMetaData
{
    public string CharSet { get; }
    public string DocType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlDocumentMetaData"/> class.
    /// </summary>
    /// <param name="charSet">The detected character encoding.</param>
    /// <param name="docType">The full DOCTYPE declaration string.</param>
    public HtmlDocumentMetaData(string charSet, string docType) : base (type : DocumentMetaDataType.Html)
    {
        CharSet = charSet;
        DocType = docType;
    }
}

/// <summary>
/// Contains metadata specific to PDF documents, such as versioning and internal encoding.
/// </summary>
public class PdfDocumentMetaData : DocumentMetaData
{
    public string PdfVersion { get; }
    public string EncodingType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfDocumentMetaData"/> class.
    /// </summary>
    /// <param name="pdfVersion">The PDF version string.</param>
    /// <param name="encodingType">The encoding format used in the file.</param>
    public PdfDocumentMetaData(string pdfVersion, string encodingType) : base (type: DocumentMetaDataType.Pdf)
    {
        PdfVersion = pdfVersion;
        EncodingType = encodingType;
    }
}
