using System.Text;
using HtmlAgilityPack;

/// <summary>
/// Provides utility methods for extracting human-readable text from HTML nodes 
/// while preserving logical spacing between block-level elements.
/// </summary>
public static class HtmlTextExtractor
{
    private static readonly HashSet<string> BlockElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "div", "li", "br", "tr", "h1", "h2", "h3", "h4", "h5", "h6", "section", "article", "aside"
    };

    /// <summary>
    /// Traverses the HTML tree and extracts text, ensuring that block-level elements 
    /// (like div, p, li) are separated by spaces to prevent word concatenation.
    /// </summary>
    /// <param name="node">The root node to extract text from.</param>
    /// <param name="sb">The StringBuilder to append text to.</param>
    public static void ExtractTextWithSpaces(HtmlNode node, StringBuilder sb)
    {
        foreach (var child in node.ChildNodes)
        {
            if (child.NodeType == HtmlNodeType.Text)
            {
                // Append text directly from text nodes
                sb.Append(child.InnerText);
            }
            else
            {
                // If when entering a new block element there already is content in 
                // the string builder add a space before adding the next element
                // Example scenario: <li>Support<div>FAQ</div></li>
                bool isBlock = IsBlockElement(child.Name);

                if (isBlock && sb.Length > 0 && !char.IsWhiteSpace(sb[sb.Length - 1]))
                {
                    sb.Append(" ");
                }

                // Recursively visit child nodes
                ExtractTextWithSpaces(child, sb);

                // If the element is a block-level tag, inject a space to ensure 
                // logical separation (e.g., prevent "HomeAbout" from <li>Home</li><li>About</li>)
                if (isBlock && sb.Length > 0 && !char.IsWhiteSpace(sb[sb.Length - 1]))
                {
                    sb.Append(" ");
                }
            }
        }
    }

    /// <summary>
    /// Determines if an HTML element is a block-level element that visually 
    /// separates content in a browser.
    /// </summary>
    private static bool IsBlockElement(string name) => BlockElements.Contains(name);
}