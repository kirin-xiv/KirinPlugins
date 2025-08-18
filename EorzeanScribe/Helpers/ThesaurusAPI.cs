using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EorzeanScribe.Helpers;

internal enum ApiState { Idle, Searching, Failed }

// JSON response structure for API Ninjas Thesaurus API
internal class ApiNinjasThesaurusResponse
{
    [JsonProperty("synonyms")]
    public List<string> Synonyms { get; set; } = new();
    
    [JsonProperty("antonyms")]
    public List<string> Antonyms { get; set; } = new();
}

internal sealed class ThesaurusAPI : IDisposable
{
    public bool Loading { get; private set; } = false;
    private float _progress = 0.0f;
    public float Progress => _progress;

    internal ApiState State { get; private set; }

    private List<WordSearchResult> _history = new();

    public List<WordSearchResult> History => _history;

    private readonly HttpClient _httpClient;

    public ThesaurusAPI()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", EorzeanScribe.Configuration.ThesaurusApiKey);
        State = ApiState.Idle;
    }

    public async Task<WordSearchResult> SearchAsync(string word)
    {
        if (string.IsNullOrWhiteSpace(EorzeanScribe.Configuration.ThesaurusApiKey))
        {
            var noKeyResult = new WordSearchResult(word);
            var errorEntry = new ThesaurusEntry();
            errorEntry.Word = "No API key configured. Please add your API Ninjas key in settings.";
            noKeyResult.AddEntry(errorEntry);
            _history.Insert(0, noKeyResult);
            return noKeyResult;
        }

        Loading = true;
        State = ApiState.Searching;
        _progress = 0.0f;

        try
        {
            string url = $"https://api.api-ninjas.com/v1/thesaurus?word={Uri.EscapeDataString(word)}";
            
            _progress = 0.5f;
            
            var response = await _httpClient.GetAsync(url);
            
            _progress = 0.8f;
            
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ApiNinjasThesaurusResponse>(json);
                
                _progress = 1.0f;
                
                var result = new WordSearchResult(word);
                var thesaurusEntry = new ThesaurusEntry();
                thesaurusEntry.Word = word;
                
                // Add synonyms
                if (apiResponse?.Synonyms != null)
                    thesaurusEntry.AddSynonyms(apiResponse.Synonyms);
                
                // Add antonyms  
                if (apiResponse?.Antonyms != null)
                    thesaurusEntry.AddAntonyms(apiResponse.Antonyms);
                
                result.AddEntry(thesaurusEntry);
                
                _history.Insert(0, result);
                if (_history.Count > EorzeanScribe.Configuration.SearchHistoryCount)
                    _history.RemoveAt(_history.Count - 1);
                
                State = ApiState.Idle;
                Loading = false;
                return result;
            }
            else
            {
                State = ApiState.Failed;
                Loading = false;
                var errorResult = new WordSearchResult(word);
                var errorEntry = new ThesaurusEntry();
                errorEntry.Word = $"API request failed: {response.StatusCode}";
                errorResult.AddEntry(errorEntry);
                _history.Insert(0, errorResult);
                return errorResult;
            }
        }
        catch (Exception ex)
        {
            State = ApiState.Failed;
            Loading = false;
            var errorResult = new WordSearchResult(word);
            var errorEntry = new ThesaurusEntry();
            errorEntry.Word = $"Error: {ex.Message}";
            errorResult.AddEntry(errorEntry);
            _history.Insert(0, errorResult);
            return errorResult;
        }
    }

    public void DeleteResult(WordSearchResult result)
    {
        _history.Remove(result);
    }

    public async void SearchThesaurus(string word)
    {
        await SearchAsync(word);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}