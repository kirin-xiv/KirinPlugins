
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace EorzeanScribe.Helpers;

public static class Lang
{
    private static HashSet<string> _dictionary = new();
    
    // Common misspellings database for instant suggestions
    private static readonly Dictionary<string, string[]> _commonMisspellings = new()
    {
        { "apropriate", new[] { "appropriate" } },
        { "seperate", new[] { "separate" } },
        { "definately", new[] { "definitely" } },
        { "occured", new[] { "occurred" } },
        { "recieve", new[] { "receive" } },
        { "begining", new[] { "beginning" } },
        { "embarrasing", new[] { "embarrassing" } },
        { "neccessary", new[] { "necessary" } },
        { "accomodate", new[] { "accommodate" } },
        { "acheive", new[] { "achieve" } },
        { "aquire", new[] { "acquire" } },
        { "belive", new[] { "believe" } },
        { "colleauge", new[] { "colleague" } },
        { "comming", new[] { "coming" } },
        { "commited", new[] { "committed" } },
        { "concious", new[] { "conscious" } },
        { "existance", new[] { "existence" } },
        { "goverment", new[] { "government" } },
        { "happend", new[] { "happened" } },
        { "independant", new[] { "independent" } },
        { "intresting", new[] { "interesting" } },
        { "knowlege", new[] { "knowledge" } },
        { "lenght", new[] { "length" } },
        { "maintainance", new[] { "maintenance" } },
        { "noticable", new[] { "noticeable" } },
        { "occassion", new[] { "occasion" } },
        { "personaly", new[] { "personally" } },
        { "posible", new[] { "possible" } },
        { "preffered", new[] { "preferred" } },
        { "privilige", new[] { "privilege" } },
        { "publically", new[] { "publicly" } },
        { "realy", new[] { "really" } },
        { "reccomend", new[] { "recommend" } },
        { "rythm", new[] { "rhythm" } },
        { "similiar", new[] { "similar" } },
        { "suprise", new[] { "surprise" } },
        { "tommorow", new[] { "tomorrow" } },
        { "untill", new[] { "until" } },
        { "usefull", new[] { "useful" } },
        { "wether", new[] { "whether" } },
        { "teh", new[] { "the" } },
        { "hte", new[] { "the" } },
        { "nad", new[] { "and" } },
        { "adn", new[] { "and" } },
        
        // Roleplay/Gaming specific misspellings
        { "charachter", new[] { "character" } },
        { "aventure", new[] { "adventure" } },
        { "rouge", new[] { "rogue" } },
        { "assasin", new[] { "assassin" } },
        { "gaurd", new[] { "guard" } },
        { "theif", new[] { "thief" } },
        { "warrier", new[] { "warrior" } },
        { "magick", new[] { "magic" } },
        { "armour", new[] { "armor", "armour" } }, // Both spellings valid
        { "favour", new[] { "favor", "favour" } }, // Both spellings valid
        { "colour", new[] { "color", "colour" } }, // Both spellings valid
        
        // More common mistakes
        { "alot", new[] { "a lot" } },
        { "wierd", new[] { "weird" } },
        { "freind", new[] { "friend" } },
        { "buisness", new[] { "business" } },
        { "reccomendation", new[] { "recommendation" } },
        { "tounge", new[] { "tongue" } },
        { "gague", new[] { "gauge" } },
        { "fourty", new[] { "forty" } },
        { "ninty", new[] { "ninety" } },
        { "twelth", new[] { "twelfth" } }
    };

    private static bool _enabled = false;
    /// <summary>
    /// Active becomes true after Init() has successfully loaded a language file.
    /// </summary>
    public static bool Enabled
    {
        get { return _enabled; }
        set
        {
            _enabled = value;
        }
    }

    /// <summary>
    /// Gets the count of words in the dictionary
    /// </summary>
    public static int DictionaryCount => _dictionary.Count;

    /// <summary>
    /// Verifies that the string exists in the hash table
    /// </summary>
    /// <param name="key">String to search for.</param>
    /// <returns><see langword="true""/> if the word is in the dictionary</returns>
    public static bool isWord(string key) => isWord(key, true);

    /// <summary>
    /// Verifies that the string exists in the hash table
    /// </summary>
    /// <param name="key">String to search for.</param>
    /// <param name="lowercase">If <see langword="true"/> then the string is made lowercase.</param>
    /// <returns><see langword="true""/> if the word is in the dictionary</returns>
    public static bool isWord(string key, bool lowercase)
    {
        string searchKey = lowercase ? key.ToLower() : key;
        return _dictionary.Contains(searchKey);
    }

    private static void ValidateAndAddWord(string candidate)
    {
        // Split and trim the candidate into all possible words. This should break entries with multiple words into single entries.
        string[] splits = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string s in splits)
            _dictionary.Add(s.ToLower());
    }


    /// <summary>
    /// Initialize the dictionary with SCOWL word list and custom user entries.
    /// </summary>
    public static void Init()
    {
        _dictionary.Clear();
        
        // Load SCOWL dictionary
        LoadSCOWLDictionary();
        
        // Add all of the custom dictionary entries to the dictionary
        foreach (string word in EorzeanScribe.Configuration.CustomDictionaryEntries)
            ValidateAndAddWord(word);
        
        Enabled = true;
    }

    /// <summary>
    /// Load dictionary from master dictionary file
    /// </summary>
    private static void LoadSCOWLDictionary()
    {
        try
        {
            string pluginDir = EorzeanScribe.PluginInterface.AssemblyLocation.Directory?.FullName ?? "";
            string masterDictionaryPath = Path.Combine(pluginDir, "master_dictionary.txt");
            
            if (!File.Exists(masterDictionaryPath))
            {
                EorzeanScribe.PluginLog.Error($"Master dictionary not found at: {masterDictionaryPath}");
                return;
            }

            EorzeanScribe.PluginLog.Info("Loading master dictionary...");
            LoadWordListFile(masterDictionaryPath);
            EorzeanScribe.PluginLog.Info($"Loaded {_dictionary.Count} words from master dictionary");
        }
        catch (Exception ex)
        {
            EorzeanScribe.PluginLog.Error($"Failed to load master dictionary: {ex.Message}");
        }
    }

    /// <summary>
    /// Load words from a dictionary file
    /// </summary>
    private static void LoadWordListFile(string filePath)
    {
        try
        {
            string[] words = File.ReadAllLines(filePath);
            foreach (string word in words)
            {
                string cleanWord = word.Trim().ToLower();
                if (!string.IsNullOrEmpty(cleanWord) && !cleanWord.StartsWith("#"))
                {
                    _dictionary.Add(cleanWord);
                }
            }
        }
        catch (Exception ex)
        {
            EorzeanScribe.PluginLog.Error($"Failed to load word list {filePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Reinitialize the dictionary.
    /// </summary>
    public static void Reinit() => Init();


    /// <summary>
    /// Attempts to add a word to the master dictionary.
    /// </summary>
    /// <param name="word">String to add.</param>
    /// <returns><see langword="true"/> if the word was not in the dictionary already.</returns>
    public static bool AddDictionaryEntry(string word)
    {
        string cleanWord = word.Trim().ToLower();
        
        // Check if word already exists
        if (_dictionary.Contains(cleanWord))
            return false;

        try
        {
            // Add to in-memory dictionary
            _dictionary.Add(cleanWord);

            // Append to master dictionary file
            string pluginDir = EorzeanScribe.PluginInterface.AssemblyLocation.Directory?.FullName ?? "";
            string masterDictionaryPath = Path.Combine(pluginDir, "master_dictionary.txt");
            
            File.AppendAllText(masterDictionaryPath, Environment.NewLine + cleanWord);
            
            // Also add to configuration for backward compatibility
            EorzeanScribe.Configuration.CustomDictionaryEntries.Add(cleanWord);
            EorzeanScribe.Configuration.Save();
            
            EorzeanScribe.PluginLog.Info($"Added word '{cleanWord}' to master dictionary");
            return true;
        }
        catch (Exception ex)
        {
            EorzeanScribe.PluginLog.Error($"Failed to add word '{cleanWord}' to master dictionary: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Attempt to remove a word from the custom dictionary
    /// </summary>
    /// <param name="word">String to remove</param>
    public static void RemoveDictionaryEntry(string word)
    {
        _dictionary.Remove(word.Trim().ToLower());
        EorzeanScribe.Configuration.CustomDictionaryEntries.Remove(word.Trim().ToLower());
        EorzeanScribe.Configuration.Save();
    }


    internal static IReadOnlyList<string> GetSuggestions(string word)
    {
        if ( word.Length == 0 )
            throw new Exception( $"GetSuggestions({word}) failed. Word must have length." );

        // Debug logging
        EorzeanScribe.PluginLog.Debug($"GetSuggestions called for: '{word}', Dictionary enabled: {Enabled}, Dictionary size: {_dictionary.Count}");

        // Check if the first character is capitalized.
        bool isCapped = Regex.Match(word, @"^\s*[A-Z].*").Success;

        // Get the lowercase version of the word for the remaining tests.
        string originalWord = word;
        word = word.ToLower();
        
        // FIRST: Check common misspellings database for instant suggestions
        if (_commonMisspellings.TryGetValue(word, out string[]? commonSuggestions))
        {
            List<string> commonResults = new();
            foreach (string suggestion in commonSuggestions)
            {
                // Apply proper capitalization
                string finalSuggestion = isCapped ? suggestion.CaplitalizeFirst() : suggestion;
                commonResults.Add(finalSuggestion);
            }
            EorzeanScribe.PluginLog.Debug($"Found common misspelling: '{word}' → [{string.Join(", ", commonResults)}]");
            return commonResults;
        }
        
        // Test if basic words are in dictionary for debugging
        EorzeanScribe.PluginLog.Debug($"Dictionary tests - 'the': {isWord("the")}, 'appropriate': {isWord("appropriate")}, 'test': {isWord("test")}");

        // Generate all of the possible suggestions. We start the GenerateAway thread first as it
        // is by far the longest process.
        Task<List<string>> aways = new(() => { return GenerateAway(word, 2, isCapped, true); });
        aways.Start();

        Task<List<string>> transpose = new(() => { return GenerateTranspose(word, isCapped, true); } );
        transpose.Start();

        Task<List<string>> splits = new(() => { return GenerateSplits(word); });
        splits.Start();

        Task<List<string>> deletes = new(() => { return GenerateDeletes(word, isCapped, true); });
        deletes.Start();

        List<string> results = new();

        void AddResults(Task<List<string>> t)
        {
            int index = 0;
            EorzeanScribe.PluginLog.Debug($"AddResults called with {t.Result.Count} candidates");
            while ( results.Count <= EorzeanScribe.Configuration.MaximumSuggestions && index < t.Result.Count && isWord( t.Result[index] ) )
            {
                EorzeanScribe.PluginLog.Debug($"Adding valid suggestion: '{t.Result[index]}'");
                results.Add( t.Result[index++] );
            }
            // Log some rejected candidates for debugging
            for (int i = index; i < Math.Min(index + 5, t.Result.Count); i++)
            {
                EorzeanScribe.PluginLog.Debug($"Rejected candidate: '{t.Result[i]}' (isWord: {isWord(t.Result[i])})");
            }
        }
        // Collect the transposes.
        transpose.Wait();
        AddResults( transpose );

        // Collect the aways.
        aways.Wait();
        AddResults( aways );

        // Collect the splits.
        splits.Wait();
        AddResults( splits );

        // Collect the deleted characters.
        deletes.Wait();
        AddResults( deletes );

        EorzeanScribe.PluginLog.Debug($"GetSuggestions (algorithmic) returning {results.Count} suggestions for '{word}': [{string.Join(", ", results)}]");
        return results;
    }

    private static List<string> GenerateTranspose(string word, bool isCapped, bool filter)
    {
        List<string> results = new();
        EorzeanScribe.PluginLog.Debug($"GenerateTranspose starting for '{word}', filter={filter}");

        // Letter swaps
        for (int x = 0; x < word.Length - 1; ++x)
        {
            // Get the chars.
            char[] chars = word.ToCharArray();

            // Get the char at x
            char y = chars[x];

            // Move the char from x+1 to x
            chars[x] = chars[x + 1];

            // Overwite char at x+1 with x.
            chars[x + 1] = y;

            string candidate = new string(chars);
            bool isValidWord = isWord(candidate);
            
            EorzeanScribe.PluginLog.Debug($"  Transpose candidate: '{candidate}', isWord={isValidWord}");
            
            if (!filter || isValidWord)
            {
                string finalCandidate = isCapped ? candidate.CaplitalizeFirst() : candidate;
                results.Add(finalCandidate);
                EorzeanScribe.PluginLog.Debug($"    Added: '{finalCandidate}'");
            }
        }
        
        EorzeanScribe.PluginLog.Debug($"GenerateTranspose returning {results.Count} results: [{string.Join(", ", results)}]");
        return results;
    }

    private static List<string> GenerateDeletes(string word, bool isCapped, bool filter)
    {
        if (word.Length == 0)
            return new();

        List<string> results = new();
        for (int i = 0; i < word.Length; ++i)
        {
            if (!filter || isWord(word.Remove(i, 1)))
                results.Add(isCapped ? word.Remove(i, 1).CaplitalizeFirst() : word.Remove(i, 1));
        }

        return results;
    }

    private static List<string> GenerateSplits(string word)
    {
        // for index
        // split into two words
        // if both splits are words
        // if check word one
        // then if check word two
        // add word one + word two
        // return results
        List<string> results = new();
        for (int i = 1; i < word.Length - 1; ++i)
        {
            string[] splits = new string[] { word[0..i], word[i..^0] };
            if (isWord(splits[0]) && isWord(splits[1]))
                results.Add($"{splits[0]} {splits[1]}");
        }
        return results;
    }

    private static List<string> GenerateAway(string word, int depth, bool isCapped, bool filter)
    {
        string letters = "abcdefghijklmnopqrstuvwxyz";
        List<string> results = new();
        try
        {
            // This will toggle between vowel and consonant generation
            for ( int z = 0; z < 2; z++ )
            {
                for ( int x = 0; x < word.Length; ++x )
                {
                    for ( int y = 0; y < letters.Length; ++y )
                    {
                        char[] chars = word.ToCharArray();

                        // Start with vowel replacements, these are more common than
                        // consonant mistakes.
                        if ( "aAeEiIoOuUyY".Contains( chars[x] ) == (z == 0) )
                        {
                            chars[x] = letters[y];
                            string test = new(chars);

                            if ( (!filter || isWord( test )) && !results.Contains( test ) )
                                results.Add( isCapped ? test.CaplitalizeFirst() : test );
                        }

                        // For optimization break out of the y loop to avoid checking this
                        // 26 different times each time the chars[x] is the wrong character type.
                        // i.e. consant when z==0 or vowel when z==1.
                        else
                            break;
                    }
                }
            }

            for ( int y = 0; y < letters.Length; ++y )
            {
                // Insert a character before the word
                string foretest = $"{letters[y]}{word}";

                // If the inserted character makes a word or not filtering then add it if
                // it is not already in the results.
                if ( (!filter || isWord( foretest )) && !results.Contains( foretest ) )
                    results.Add( foretest );

                // Append a character to the word
                string afttest = $"{word}{letters[y]}";

                // If the appended character makes a word or not filtering then add it if
                // it is not already in the results.
                if ( (!filter || isWord( afttest )) && !results.Contains( afttest ) )
                    results.Add( afttest );
            }

            if ( depth > 1 )
            {
                List<string> parents = new(results);
                foreach ( string s in parents )
                    results.AddRange( GenerateAway( s, depth - 1, isCapped, depth > 2 ) );
            }
        }
        catch ( Exception e )
        {
            EorzeanScribe.PluginLog.Error( e.ToString() );
        }
        return results;
    }
}
