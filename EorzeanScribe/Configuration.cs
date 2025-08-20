using System.ComponentModel;
using System.Reflection;
using Dalamud.Configuration;
using Dalamud.Plugin.Services;

namespace EorzeanScribe;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{

    /// <summary>
    /// A variable requried by Dalamud. This is used to
    /// identify the versioning of the configuration in case there
    /// are any breaking changes.
    /// </summary>
    public int Version { get; set; } = 0;
    #region General Settings
    public int SearchHistoryCount { get; set; } = 10;
    public bool ResearchToTop { get; set; } = true;


    /// <summary>
    /// The API key for API Ninjas Thesaurus API (unrestricted word support)
    /// Get your free key at: https://api.api-ninjas.com/
    /// </summary>
    public string ThesaurusApiKey { get; set; } = "IKvpY7mhExTydfNLj8oh5w==yroLZCLNcg8gu600";

    /// <summary>
    /// This is enabled when a save is performed to notify that changes
    /// have been commited to the configuration file.
    /// </summary>
    internal bool RecentlySaved { get; set; } = false;


    
    /// <summary>
    /// If <see langword="true"/> shows confirmation dialog before copying to public channels.
    /// </summary>
    public bool ConfirmPublicChannels { get; set; } = true;
    #endregion

    #region Text Editor Settings
    //
    // Text Editor Settings
    //

    /// <summary>
    /// If true, the spellchecker will not attempt to match words ending in a hyphen.
    /// This is because people often write a hyphen to indicate their sentence being
    /// cut off (i.e. "How dare yo-").
    /// </summary>
    public bool IgnoreWordsEndingInHyphen { get; set; } = true;

    /// <summary>
    /// If enabled, uses a custom label layout to display highlighted text.
    /// </summary>
    public bool EnableTextHighlighting { get; set; } = true;

    /// <summary>
    /// Toggles displaying text in copy chunks.
    /// </summary>
    public bool ShowTextInChunks { get; set; } = true;

    /// <summary>
    /// Attempts to break text chunks at the nearest sentence rather than the nearest space.
    /// </summary>
    public bool SplitTextOnSentence { get; set; } = true;

    /// <summary>
    /// The symbols to consider the end of a sentence.
    /// </summary>
    public string SentenceTerminators { get; set; } = ".?!";

    /// <summary>
    /// The symbols that count as encapsulation characters. These can be next to SplitPoints.
    /// </summary>
    public string EncapsulationTerminators { get; set; } = "\"'*-";

    /// <summary>
    /// Specifies the continuation marker to use at the end of each chunk.
    /// </summary>
    public string ContinuationMarker { get; set; } = "(#c/#m)";

    private List<ChunkMarker> _chunkMarkers = new();
    public List<ChunkMarker> ChunkMarkers
    {
        get => this._chunkMarkers;
        set => this._chunkMarkers = ChunkMarker.SortList(value);
    }


    /// <summary>
    /// When enabled, it puts the continuation marker on the last chunk as well. This is useful
    /// when someone uses a continuation marker that has something (1/3) and they want (3/3) on
    /// the last chunk.
    /// </summary>
    public bool ContinuationMarkerOnLast { get; set; } = true;

    /// <summary>
    /// If true, the editor will automatically clear text after copying the last block.
    /// </summary>
    public bool AutomaticallyClearAfterLastCopy { get; set; } = false;

    /// <summary>
    /// Maximum length of input in the text editor
    /// </summary>
    public int TextEditorMaximumLength { get; set; } = 16384;

    /// <summary>
    /// Automatically replace double spaces in the text.
    /// </summary>
    public bool ReplaceDoubleSpaces { get; set; } = true;


    /// <summary>
    /// A dictionary containing the different colors for chat headers.
    /// </summary>
    public Dictionary<int, Vector4> HeaderColors = new()
    {
        {(int)Enums.ChatType.Emote, new(0.9f, 0.9f, 0.9f, 1f) },
        {(int)Enums.ChatType.Reply, new(1f, 0.35f, 0.6f, 1f) },
        {(int)Enums.ChatType.Say, new(1f, 1f, 1f, 1f) },
        {(int)Enums.ChatType.Party, new(0f, 0.5f, 0.6f, 1f) },
        {(int)Enums.ChatType.FC, new(0.6f, 0.75f, 1f, 1f) },
        {(int)Enums.ChatType.Shout, new(1f, 0.5f, 0.2f, 1f) },
        {(int)Enums.ChatType.Yell, new(0.9f, 1f, 0.2f, 1f) },
        {(int)Enums.ChatType.Tell, new(1f, 0.35f, 0.6f, 1f) },
        {(int)Enums.ChatType.Echo, new(0.75f, 0.75f, 0.75f, 1f) },
        {(int)Enums.ChatType.Linkshell, new(0.8f, 1f, 0.6f, 1f) }
    };


    /// <summary>
    /// The limit to the history size that the text editor keeps.
    /// </summary>
    public int TextEditorHistoryLimit { get; set; } = 5;

    /// <summary>
    /// The upper limit to the size of the input in the text editor
    /// </summary>
    public int TextEditorInputLineHeight { get; set; } = 5;
    #endregion

    #region Spell Checker Settings
    /// <summary>
    /// Holds the dictionary of words added by the user.
    /// </summary>
    public List<string> CustomDictionaryEntries { get; set; } = new();


    /// <summary>
    /// The color to display a mispelled word as.
    /// </summary>
    public Vector4 SpellingErrorHighlightColor { get; set; } = new( 0.9f, 0.2f, 0.2f, 1f );

    /// <summary>
    /// Default list that spell checker will attempt to delete.
    /// </summary>
    private const string PUNCTUATION_CLEAN_LIST_DEFAULT = @",.:;'*""(){}[]!?<>`~♥@#$%^&*_=+\\/←→↑↓《》■※☀★★☆♡ヅツッシ☀☁☂℃℉°♀♂♠♣♦♣♧®©™€$£♯♭♪✓√◎◆◇♦■□〇●△▽▼▲‹›≤≥<«“”─＼～";
    
    /// <summary>
    /// The spell checker will attempt to delete these punctuation marks from the beginning and end of every word
    /// </summary>
    public string PunctuationCleaningList { get; set; } = PUNCTUATION_CLEAN_LIST_DEFAULT;

    /// <summary>
    /// The maximum number of suggestions that a word will generate
    /// </summary>
    public int MaximumSuggestions { get; set; } = 5;


    /// <summary>
    /// The <see cref="float"/> delay between when the user stops typing and
    /// when the text editor runs the spell check.
    /// </summary>
    public float AutoSpellCheckDelay { get; set; } = 1f;
    #endregion

    #region Linkshell Settings
    /// <summary>
    /// Contains the nicknames of all Cross-World Linkshells
    /// </summary>
    public string[] CrossWorldLinkshellNames { get; set; } = new string[] { "1", "2", "3", "4", "5", "6", "7", "8" };

    /// <summary>
    /// Contains the names of all normal Linkshells.
    /// </summary>
    public string[] LinkshellNames { get; set; } = new string[] { "1", "2", "3", "4", "5", "6", "7", "8" };
    #endregion

    /// <summary>
    /// Saves the current configuration to file.
    /// </summary>
    /// <param name="notify"><see cref="bool"/> indicating if the user should be notified that settings were saved.</param>
    internal void Save(bool notify = true)
    {
        EorzeanScribe.PluginInterface.SavePluginConfig(this);
        if (notify)
            EorzeanScribe.NotificationManager.AddNotification(new()
            {
                Content = "Configuration saved!",
                Title = "EorzeanScribe",
                Type = Dalamud.Interface.ImGuiNotification.NotificationType.Success
            });
            //EorzeanScribe.PluginInterface.UiBuilder.AddNotification("Configuration saved!", "EorzeanScribe", Dalamud.Interface.Internal.Notifications.NotificationType.Success);
        this.RecentlySaved = true;
    }
}