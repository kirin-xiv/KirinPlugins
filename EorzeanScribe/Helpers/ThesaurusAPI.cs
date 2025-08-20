using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
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
        _httpClient.Timeout = TimeSpan.FromSeconds(10); // Add 10 second timeout
        
        // Set a User-Agent header
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "EorzeanScribe/1.5.0 FFXIV-Plugin");
        
        // Only add API key header if it's not empty
        if (!string.IsNullOrWhiteSpace(EorzeanScribe.Configuration.ThesaurusApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", EorzeanScribe.Configuration.ThesaurusApiKey);
        }
        
        EorzeanScribe.PluginLog.Debug($"ThesaurusAPI initialized. API Key configured: {!string.IsNullOrWhiteSpace(EorzeanScribe.Configuration.ThesaurusApiKey)}");
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
            State = ApiState.Failed;
            return noKeyResult;
        }
        
        // Clear and re-add the API key header to ensure it's current
        _httpClient.DefaultRequestHeaders.Remove("X-Api-Key");
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", EorzeanScribe.Configuration.ThesaurusApiKey);
        
        EorzeanScribe.PluginLog.Debug($"Making API request for word: {word}");
        EorzeanScribe.PluginLog.Debug($"API Key present: {!string.IsNullOrWhiteSpace(EorzeanScribe.Configuration.ThesaurusApiKey)}");

        Loading = true;
        State = ApiState.Searching;
        _progress = 0.0f;

        try
        {
            string url = $"https://api.api-ninjas.com/v1/thesaurus?word={Uri.EscapeDataString(word)}";
            EorzeanScribe.PluginLog.Debug($"Request URL: {url}");
            
            _progress = 0.5f;
            
            var response = await _httpClient.GetAsync(url);
            
            _progress = 0.8f;
            
            EorzeanScribe.PluginLog.Debug($"API Response Status: {response.StatusCode}");
            EorzeanScribe.PluginLog.Debug($"API Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");
            
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                EorzeanScribe.PluginLog.Debug($"API Response Content: {json}");
                var apiResponse = JsonConvert.DeserializeObject<ApiNinjasThesaurusResponse>(json);
                
                _progress = 1.0f;
                
                var result = new WordSearchResult(word);
                var thesaurusEntry = new ThesaurusEntry();
                thesaurusEntry.Word = word;
                thesaurusEntry.Type = "Thesaurus"; // API Ninjas doesn't provide word type
                thesaurusEntry.Definition = word; // Use the word itself since no definition is provided
                
                // Add synonyms
                if (apiResponse?.Synonyms != null && apiResponse.Synonyms.Count > 0)
                    thesaurusEntry.AddSynonyms(apiResponse.Synonyms);
                
                // Add antonyms  
                if (apiResponse?.Antonyms != null && apiResponse.Antonyms.Count > 0)
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
                string errorContent = await response.Content.ReadAsStringAsync();
                EorzeanScribe.PluginLog.Error($"API request failed with status {response.StatusCode}: {errorContent}");
                
                State = ApiState.Failed;
                Loading = false;
                var errorResult = new WordSearchResult(word);
                var errorEntry = new ThesaurusEntry();
                errorEntry.Word = $"API request failed: {response.StatusCode} - {response.ReasonPhrase}";
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
        try
        {
            EorzeanScribe.PluginLog.Debug($"SearchThesaurus called for word: {word}");
            await SearchAsync(word).ConfigureAwait(false);
            EorzeanScribe.PluginLog.Debug($"SearchThesaurus completed for word: {word}");
        }
        catch (Exception ex)
        {
            EorzeanScribe.PluginLog.Error($"Thesaurus search failed: {ex.Message}");
            EorzeanScribe.PluginLog.Error($"Stack trace: {ex.StackTrace}");
            State = ApiState.Failed;
            Loading = false;
            
            // Add error to history
            var errorResult = new WordSearchResult(word);
            var errorEntry = new ThesaurusEntry();
            errorEntry.Word = $"Search failed: {ex.Message}";
            errorResult.AddEntry(errorEntry);
            _history.Insert(0, errorResult);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}