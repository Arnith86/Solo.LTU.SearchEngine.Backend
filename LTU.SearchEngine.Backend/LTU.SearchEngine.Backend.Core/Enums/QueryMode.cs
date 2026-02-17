namespace LTU.SearchEngine.Backend.Core.Enums;

/// <summary>
/// Defines how query terms should be combined.
/// OR is default according to FRQ-3005 (whitespace implies OR).
/// AND is explicit via AND / &&.
/// </summary>
public enum QueryMode
{
    OR,
    AND
}
