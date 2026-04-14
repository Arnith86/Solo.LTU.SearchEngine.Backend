namespace LTU.SearchEngine.Backend.Core.HelperClasses;


public interface ITermMapper<TCollection>//, TKey, TValue> 
{
    void AddTerm(string term);
    TCollection ToReadOnly();
}