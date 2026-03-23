using Microsoft.AspNetCore.Components;
using System.Collections;

namespace PiedraAzul.Client.Services.AutoCompleteService
{
    public interface IAutoCompleteService
    {
        TypeSearch TypeSearch { get; set; }
        Task<ICollection> GetResultAsync(string query);
        MarkupString GetRender(object item);
    }
    public interface IAutoCompleteResult
    {
        string HtmlString();
    }
    public enum TypeSearch
    {
        Doctor,
        Patient,
        Specialty
    }
    public class AutoComplete
    {
    }
}
