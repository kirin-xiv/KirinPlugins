using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using EorzeanScribe.Enums;
using EorzeanScribe.Helpers;

namespace EorzeanScribe.Gui;

internal sealed class SettingsUI : Window
{
    private const int MAX_MARKER_FRAME_Y = 10;

    /// <summary>
    /// Gets the available size for tab pages while leaving room for the footer.
    /// </summary>
    /// <returns>Returns a float representing the available canvas height for settings tabs.</returns>
    private float GetCanvasSize() => ImGui.GetContentRegionMax().Y - ImGui.GetCursorPosY() - (EorzeanScribe.BUTTON_Y*ImGuiHelpers.GlobalScale) - (this._style.FramePadding.Y * 2);


    // General settings. 
    private int _searchHistoryCountChange = EorzeanScribe.Configuration.SearchHistoryCount;
    private bool _researchToTopChange = EorzeanScribe.Configuration.ResearchToTop;
    private bool _trackWordStats = EorzeanScribe.Configuration.TrackWordStatistics;

    // Scratch Pad settings.
    private bool _autoClear = EorzeanScribe.Configuration.AutomaticallyClearAfterLastCopy;
    private bool _deleteClosed = EorzeanScribe.Configuration.DeleteClosedScratchPads;
    private bool _confirmDeleteClosed = EorzeanScribe.Configuration.ConfirmDeleteClosePads;
    private bool _ignoreHypen = EorzeanScribe.Configuration.IgnoreWordsEndingInHyphen;
    private bool _showChunks = EorzeanScribe.Configuration.ShowTextInChunks;
    private bool _onSentence = EorzeanScribe.Configuration.SplitTextOnSentence;
    private bool _detectHeader = EorzeanScribe.Configuration.ParseHeaderInput;
    private string _sentenceTerminators = EorzeanScribe.Configuration.SentenceTerminators;
    private string _encapTerminators = EorzeanScribe.Configuration.EncapsulationTerminators;
    private List<ChunkMarker> _chunkMarkers = EorzeanScribe.Configuration.ChunkMarkers;
    private ChunkMarker _newMarker = new();
    private string _continuationMarker = EorzeanScribe.Configuration.ContinuationMarker;
    private bool _markLastChunk = EorzeanScribe.Configuration.ContinuationMarkerOnLast;
    private int _scratchMaxTextLen = EorzeanScribe.Configuration.ScratchPadMaximumTextLength;
    private int _scratchInputLineHeight = EorzeanScribe.Configuration.ScratchPadInputLineHeight;


    // Spellcheck Settings
    private bool _fixDoubleSpace = EorzeanScribe.Configuration.ReplaceDoubleSpaces;
    private int _maxSuggestions = EorzeanScribe.Configuration.MaximumSuggestions;
    private bool _autospellcheck = EorzeanScribe.Configuration.AutoSpellCheck;
    private float _autospellcheckdelay = EorzeanScribe.Configuration.AutoSpellCheckDelay;
    private string _punctuationCleaningString = EorzeanScribe.Configuration.PunctuationCleaningList;

    // Linkshell Settings
    private string[] _cwlinkshells = EorzeanScribe.Configuration.CrossWorldLinkshellNames;
    private string[] _linkshells = EorzeanScribe.Configuration.LinkshellNames;

    // Colors Settings
    private Vector4 _backupColor = new();
    private bool _enableTextColor = EorzeanScribe.Configuration.EnableTextHighlighting;
    private Vector4 _spellingErrorColor = EorzeanScribe.Configuration.SpellingErrorHighlightColor;
    private Dictionary<int, Vector4> _headerColors = EorzeanScribe.Configuration.HeaderColors.Clone();

    private ImGuiStylePtr _style = ImGui.GetStyle();

    internal static string GetWindowName() => $"{EorzeanScribe.APPNAME} - Settings";

    public SettingsUI() : base( GetWindowName() )
    {
        this._searchHistoryCountChange = EorzeanScribe.Configuration.SearchHistoryCount;
        this._researchToTopChange = EorzeanScribe.Configuration.ResearchToTop;
        //Size = new(375, 350);
        this.SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new( 510, 450 ),
            MaximumSize = new( 9999, 9999 )
        };

        this.Flags |= ImGuiWindowFlags.NoScrollbar;
        this.Flags |= ImGuiWindowFlags.NoScrollWithMouse;
    }

    public override void Update()
    {
        base.Update();

        if (!this.IsOpen)
            EorzeanScribeUI.RemoveWindow(this);
        if ( EorzeanScribe.Configuration.RecentlySaved )
            ResetValues();
    }

    public override void Draw()
    {
        try
        {
            if ( EorzeanScribe.Configuration.RecentlySaved )
                ResetValues();

            if ( ImGui.BeginTabBar( "SettingsUITabBar" ) )
            {
                DrawGeneralTab();
                DrawScratchPadTab();
                DrawSpellCheckTab();
                DrawLinkshellTab();
                DrawColorsTab();
                ImGui.EndTabBar();
            }

            ImGui.Separator();
            DrawFooter();
        }
        catch ( Exception e )
        {
            OnException( e );
        }
    }

    private void DrawGeneralTab()
    {
        if ( ImGui.BeginTabItem( "General##SettingsUITabItem" ) )
        {
            if ( ImGui.BeginChild( "GeneralSettingsChildFrame", new( -1, GetCanvasSize() ) ) )
            {
                ImGui.Checkbox( "Track Word Usage.", ref this._trackWordStats );
                ImGuiExt.SetHoveredTooltip( "This is a metric to help you avoid using the same words too often by counting each time you use a word." );

                //Search history count
                //ImGui.DragInt("Search History Size", ref _searchHistoryCountChange, 0.1f, 1, 50);
                ImGui.SetNextItemWidth( ImGui.GetContentRegionMax().X - this._style.WindowPadding.X - ImGui.CalcTextSize("Thesaurus History Size").X );
                ImGui.DragInt( "Thesaurus History Size", ref this._searchHistoryCountChange, 1, 1, 100 );
                ImGuiExt.SetHoveredTooltip( "This is the number of searches to keep in memory at one time. Setting to 0 is unlimited.\nNote: The more you keep, them more memory used." );

                //Re-search to top
                ImGui.Checkbox( "Move repeated search to top of history.", ref this._researchToTopChange );
                ImGuiExt.SetHoveredTooltip( "If enabled, when searching for a word you've searched\nalready, it will move it to the top of the list." );

                ImGui.EndChild();
            }
            ImGui.EndTabItem();
        }
    }

    private void DrawScratchPadTab()
    {
        if ( ImGui.BeginTabItem( "Scratch Pad##SettingsUITabItem" ) )
        {
            if ( ImGui.BeginChild( "SettingsUIScratchPadChildFrame", new( -1, GetCanvasSize() ) ) )
            {
                //ImGui.Text( "General Behavior" );
                if ( ImGui.CollapsingHeader( "General Behavior" ) )
                {
                    ImGui.Indent();
                    if ( ImGui.BeginTable( "SettingsUiScratchPadBehaviorTable", 2, ImGuiTableFlags.BordersH | ImGuiTableFlags.BordersInnerV ) )
                    {
                        ImGui.TableSetupColumn( "SettingsUiScratchPadBehaviorTableLeftColumn" );
                        ImGui.TableSetupColumn( "SettingsUiScratchPadBehaviorTableRightColumn" );

                        ImGui.TableNextColumn();
                        // Auto-Clear Scratch Pad
                        ImGui.Checkbox( "Auto-clear Scratch Pad", ref this._autoClear );
                        ImGuiExt.SetHoveredTooltip( "Automatically clears text from scratch pad after copying last chunk." );

                        ImGui.TableNextColumn();
                        // Auto Delete Scratch Pads
                        ImGui.Checkbox( "Auto-Delete Scratch Pads On Close##SettingsUICheckbox", ref this._deleteClosed );
                        ImGuiExt.SetHoveredTooltip( "When enabled it will delete the scratch pad on close.\nWhen disabled you will have a delete button at the bottom." );

                        ImGui.TableNextColumn();
                        // Auto Delete Scratch Pads
                        ImGui.Checkbox( "Confirm Scratch Pad Delete##SettingsUICheckbox", ref this._confirmDeleteClosed );
                        ImGuiExt.SetHoveredTooltip( "When enabled a confirmation window will appear before deleting the Scratch Pad." );

                        ImGui.TableNextColumn();
                        // Show text in chunks.
                        ImGui.Checkbox( "Show Text In Chunks##SettingsUICheckbox", ref this._showChunks );
                        ImGuiExt.SetHoveredTooltip( "When enabled it will display a large box with text above your entry form.\nThis box will show you how the text will be broken into chunks.\nIn single-line input mode this text will always show but without chunking." );

                        ImGui.TableNextColumn();
                        // Split on sentence
                        ImGui.Checkbox( "Split Text On Sentence##SettingsUICheckbox", ref this._onSentence );
                        ImGuiExt.SetHoveredTooltip( "When enabled, Scratch Pad attempts to do chunk breaks at the end of sentences instead\nof between any words." );

                        ImGui.TableNextColumn();
                        ImGui.Checkbox( "Parse Header From Text##SettingsUICheckbox", ref this._detectHeader );
                        ImGuiExt.SetHoveredTooltip( "When enabled, typing a header into the input text of a scratch pad will cause\nthe scratchpad to try to parse the desired header automatically." );

                        ImGui.EndTable();
                    }
                    ImGui.Unindent();
                }
                ImGui.Spacing();

                if (ImGui.CollapsingHeader( "Text Parsing & Interpolation##settingsheader" ) )
                    {
                        ImGui.Indent();
                        if ( ImGui.BeginTable( "SettingsUiScratchPadBehaviorTable", 2, ImGuiTableFlags.BordersH | ImGuiTableFlags.BordersInnerV ) )
                        {
                            ImGui.TableSetupColumn( "SettingsUiScratchPadChildTableLeftColumn" );
                            ImGui.TableSetupColumn( "SettingsUiScratchPadChildTableRightColumn" );


                            // The string of X's is just a placeholder that gives room for the largest expected text size.
                            float width = ImGui.GetColumnWidth() - ImGui.CalcTextSize("XXXXXXXXXXXXXXXXXXXXXXXXXXX").X;

                            ImGui.TableNextColumn();
                            // Sentence terminators
                            ImGui.SetNextItemWidth( ImGui.GetColumnWidth() - ImGui.CalcTextSize( "Sentence Terminators" ).X );
                            ImGui.InputText( "Sentence Terminators##ScratchPadSplitCharsText", ref this._sentenceTerminators, 32 );
                            ImGuiExt.SetHoveredTooltip( "Each of these characters can mark the end of a sentence when followed by a space of encapsulator.\ni.e. \"A.B\" is not a sentence terminator but \"A. B\" is." );


                            ImGui.TableNextColumn();
                            // Encapsulation terminators
                            ImGui.SetNextItemWidth( ImGui.GetColumnWidth() - ImGui.CalcTextSize( "Encpasulation Terminators" ).X );
                            ImGui.InputText( $"Encpasulation Terminators##ScratchPadEncapCharsText", ref this._encapTerminators, 32 );
                            ImGuiExt.SetHoveredTooltip( $"Each of these characters ends an encapsulator.\nThis is used with sentence terminators in case of encapsulation for chunk breaks\ni.e. \"A) B\" will not count but \"A.) B\" will." );
                            ImGui.EndTable();
                        }
                        ImGui.Unindent();
                    }
                    ImGui.Spacing();

                if ( ImGui.CollapsingHeader( "Marks & Tags##settingsheader" ) )
                {
                    ImGui.Indent();


                    ImGui.SetNextItemWidth( ImGui.CalcTextSize( "############" ).X );
                    ImGui.InputText( "Continuation Marker", ref this._continuationMarker, 64 );

                    ImGui.SameLine();
                    ImGui.Checkbox( "On Last Chunk", ref this._markLastChunk );

                    ImGui.Unindent();
                }
                ImGui.Spacing();
                }
                ImGui.Spacing();

                if ( ImGui.CollapsingHeader( "Text Input Options##settingsheader" ) )
                {
                    ImGui.Indent();
                    // Max Text Length
                    float dragWidth = ImGui.GetContentRegionAvail().X - (125 * ImGuiHelpers.GlobalScale);
                    ImGui.SetNextItemWidth( dragWidth );
                    ImGui.DragInt( "Max Text Length##ScratchPadSettingsSlider", ref this._scratchMaxTextLen, 8f, 512, EorzeanScribe.MAX_SCRATCH_LENGTH );
                    if ( this._scratchMaxTextLen > EorzeanScribe.MAX_SCRATCH_LENGTH )
                        this._scratchMaxTextLen = EorzeanScribe.MAX_SCRATCH_LENGTH;

                    ImGuiExt.SetHoveredTooltip( $"This is the buffer size for text input.\nThe higher this value is the more\nmemory consumed up to a maximum of\n{EorzeanScribe.MAX_SCRATCH_LENGTH / 1024}KB per Scratch Pad." );

                    ImGui.Separator();
                    ImGui.SetNextItemWidth( dragWidth );
                    ImGui.DragInt( "Input Height##ScratchPadInputLineHeight", ref this._scratchInputLineHeight, 0.1f, 3, 25 );
                    ImGuiExt.SetHoveredTooltip( "This is the maximum height of the text input (in lines).\nThe text input will grow up to the maximum size as\nlong as there is room for it to do so." );

                    ImGui.Unindent();
                    ImGui.Separator();
                }
                ImGui.Spacing();

                ImGui.EndChild();
            }
            ImGui.EndTabItem();
        }

    private void DrawSpellCheckTab()
    {
        if (ImGui.BeginTabItem("Spell Check##SettingsUITabItem"))
        {
            if (ImGui.BeginChild("DictionarySettingsChild", new(-1, GetCanvasSize() ) ))
            {
                ImGui.Checkbox( "Auto-Spell Check", ref this._autospellcheck );
                ImGuiExt.SetHoveredTooltip( "When enabled, spell check will automatically run after a pause in typing is detected." );
                ImGui.SameLine();

                // Ignore Hyphen terminated words.
                ImGui.Checkbox("Ignore Hyphen-Terminated Words##SettingsUICheckbox", ref this._ignoreHypen);
                ImGuiExt.SetHoveredTooltip( "This is useful in roleplay for emulating cut speech.\ni.e. \"How dare yo-,\" she was cut off by the rude man." );
                ImGui.SameLine();

                // Auto-Fix Spaces
                ImGui.Checkbox("Fix Spacing.", ref this._fixDoubleSpace);
                ImGuiExt.SetHoveredTooltip( "When enabled, Scratch Pads will programmatically remove extra\nspaces from your text for you." );
                ImGui.Separator();

                // Get half the width
                float bar_width = ImGui.GetWindowContentRegionMax().X / 2.0f;
                ImGui.SetNextItemWidth( bar_width - 170 * ImGuiHelpers.GlobalScale );
                ImGui.DragInt( "Maximum Suggestions", ref this._maxSuggestions, 0.1f, 0, 100 );
                ImGuiExt.SetHoveredTooltip( "The number of spelling suggestions to return with spell checking. 0 is unlimited results." );
                ImGui.SameLine();

                ImGui.SetNextItemWidth( bar_width - 160 * ImGuiHelpers.GlobalScale );
                ImGui.DragFloat( "Auto-Spellcheck Delay (Seconds)", ref this._autospellcheckdelay, 0.1f, 0.1f, 100f );
                ImGuiExt.SetHoveredTooltip( "The time in seconds to wait after typing stops to spell check." );
                ImGui.Separator();

                // Dictionary Management

                if ( ImGui.Button( "Reload Dictionary##ReinitLangButton" ) )
                    Lang.Reinit();

                ImGuiExt.SetHoveredTooltip( $"Reload the master dictionary and custom entries." );
                
                ImGui.Text($"Dictionary loaded with {Lang.DictionaryCount:N0} words");
                ImGui.Separator();

                // Custom Dictionary Table
                if (ImGui.BeginTable($"CustomDictionaryEntriesTable", 2, ImGuiTableFlags.BordersH))
                {
                    ImGui.TableSetupColumn("CustomDictionaryWordColumn", ImGuiTableColumnFlags.WidthStretch, 2);
                    ImGui.TableSetupColumn("CustomDictionaryDeleteColumn", ImGuiTableColumnFlags.WidthFixed, 65 * ImGuiHelpers.GlobalScale);

                    ImGui.TableNextColumn();
                    ImGui.TableHeader( "Custom Dictionary Entries" );

                    // Delete all
                    ImGui.TableNextColumn();
                    if (ImGui.Button("Delete All##DeleteAllDictionaryEntriesButton", new(-1, EorzeanScribe.BUTTON_Y.Scale() ) ))
                        EorzeanScribeUI.ShowMessageBox(
                            $"{EorzeanScribe.APPNAME} - Reset Dictionary",
                            "This will delete all entries that you added to the\ndictionary.This cannot be undone.\nProceed?",
                            MessageBox.ButtonStyle.OkCancel,
                            ( mb ) => {
                                if ( (mb.Result & MessageBox.DialogResult.Ok) == MessageBox.DialogResult.Ok )
                                {
                                    EorzeanScribe.Configuration.CustomDictionaryEntries = new();
                                    EorzeanScribe.Configuration.Save();
                                }
                            } );
                    ImGuiExt.SetHoveredTooltip( $"Deletes all dictionary entries. This action cannot be undone." );

                    // display each entry.
                    string sRemoveEntry = "";
                    for (int i = 0; i < EorzeanScribe.Configuration.CustomDictionaryEntries.Count; ++i)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text(EorzeanScribe.Configuration.CustomDictionaryEntries[i]);

                        ImGui.TableNextColumn();
                        if ( ImGui.Button( $"Delete##CustomDictionaryDelete{i}Buttom", new( -1, EorzeanScribe.BUTTON_Y.Scale() ) ) )
                            sRemoveEntry = EorzeanScribe.Configuration.CustomDictionaryEntries[i]; //Lang.RemoveDictionaryEntry( EorzeanScribe.Configuration.CustomDictionaryEntries[i] );

                        ImGuiExt.SetHoveredTooltip($"Permanently deletes {EorzeanScribe.Configuration.CustomDictionaryEntries[i]} from your custom dictionary.");
                    }
                    if ( sRemoveEntry.Length > 0 )
                    {
                        Lang.RemoveDictionaryEntry( sRemoveEntry );
                        sRemoveEntry = "";
                    }
                    ImGui.EndTable();
                }


                ImGui.EndChild();
            }
            ImGui.EndTabItem();
        }
    }

    private void DrawLinkshellTab()
    {
        if (ImGui.BeginTabItem("Linkshells##SettingsUITabItem"))
        {
            if (ImGui.BeginChild("LinkshellsSettingsChildFrame", new(-1, GetCanvasSize() ) ))
            {
                ImGui.Text("Linkshell Names");
                
                if (ImGui.BeginTable("LinkshellsNamesTable", 3, ImGuiTableFlags.BordersH))
                {
                    ImGui.TableSetupColumn("LinkshellRowHeaderColumn", ImGuiTableColumnFlags.WidthFixed, 10 * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("LinkshellNameColumn", ImGuiTableColumnFlags.WidthStretch, 1);
                    ImGui.TableSetupColumn("CrossworldLinkshellNameColumn", ImGuiTableColumnFlags.WidthStretch, 1);

                    ImGui.TableNextColumn();
                    ImGui.TableHeader("#");

                    ImGui.TableNextColumn();
                    ImGui.TableHeader("Linkshell");

                    ImGui.TableNextColumn();
                    ImGui.TableHeader("Cross-World");

                    // For each linkshell, create an id | custom name row.
                    for(int i = 0; i<8; ++i)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text($"{i + 1}");

                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputText($"##SettingsLinkshellName{i}", ref this._linkshells[i], 32);

                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputText($"##SettingsCWLinkshellName{i}", ref this._cwlinkshells[i], 32);
                    }

                    ImGui.EndTable();
                }

                ImGui.EndChild();
            }

            ImGui.EndTabItem();
        }
    }

    private void DrawColorsTab()
    {
        if ( ImGui.BeginTabItem( "Colors##SettingsUITabItem" ) )
        {
            if ( ImGui.BeginChild( "ColorsSettingsChildFrame", new( -1, GetCanvasSize() ) ) )
            {
                ImGui.Checkbox( "Enable Text Colorization##SettingsUICheckbox", ref this._enableTextColor );
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                bool spellingErrorColorPopup = false;
                if ( this._enableTextColor )
                    spellingErrorColorPopup = ImGui.ColorButton( "##ColorPreviewButton", this._spellingErrorColor );
                else
                    ImGui.ColorButton( "##ColorPreviewButtonDisabled", new( 0.2f, 0.2f, 0.2f, 0.5f ), ImGuiColorEditFlags.NoTooltip );
                if ( spellingErrorColorPopup )
                {
                    ImGui.OpenPopup( "SettingsUIErrorHighlightingColorPickerPopup" );
                    this._backupColor = this._spellingErrorColor;
                }
                if ( ImGui.BeginPopup( "SettingsUIErrorHighlightingColorPickerPopup" ) )
                {
                    if ( ImGui.ColorPicker4( "##SettingsUIErrorHighlightingPicker", ref this._backupColor ) )
                        this._spellingErrorColor = this._backupColor;

                    ImGui.EndPopup();
                }
                ImGui.SameLine( 0, 5 * ImGuiHelpers.GlobalScale );
                ImGui.Text( $"Spelling Error Color" );
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.Text( "Chat Header Colors" );

                if (ImGui.BeginTable("ColorSettingsTable", 10))
                {
                    ImGui.TableSetupColumn( "LeftColorOuterColorColumn", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale );
                    ImGui.TableSetupColumn( "LeftColorOuterChatColumn" );
                    ImGui.TableSetupColumn( "LeftColorInnerColorColumn", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale );
                    ImGui.TableSetupColumn( "LeftColorInnerChatColumn" );
                    ImGui.TableSetupColumn( "CenterColorColorColumn", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale );
                    ImGui.TableSetupColumn( "CenterColorChatColumn" );
                    ImGui.TableSetupColumn( "RightColorInnerColorColumn", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale );
                    ImGui.TableSetupColumn( "RightColorInnerChatColumn" );
                    ImGui.TableSetupColumn( "RightColorOuterColorColumn", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale );
                    ImGui.TableSetupColumn( "RightColorOuterChatColumn" );

                    string[] options = Enum.GetNames(typeof(ChatType));
                    for ( int i = 0; i < options.Length-1; ++i )
                    {
                        if ( !this._headerColors.ContainsKey( i ) )
                            continue;

                        ImGui.TableNextColumn();

                        bool highlightColorPopup = false;
                        if ( this._enableTextColor )
                            highlightColorPopup = ImGui.ColorButton( $"##ColorPreviewButton{options[i]}", this._headerColors[i] );
                        else
                            ImGui.ColorButton( $"##ColorPreviewButtonDisabled{options[i]}", new( 0.2f, 0.2f, 0.2f, 0.5f ), ImGuiColorEditFlags.NoTooltip );

                        if ( highlightColorPopup )
                        {
                            ImGui.OpenPopup( $"SettingsUIHighlightingColorPickerPopup{options[i]}" );
                            this._backupColor = this._headerColors[i];
                        }
                        if ( ImGui.BeginPopup( $"SettingsUIHighlightingColorPickerPopup{options[i]}" ) )
                        {

                            if ( ImGui.ColorPicker4( "##SettingsUIErrorHighlightingPicker", ref this._backupColor ) )
                                this._headerColors[i] = this._backupColor;

                            ImGui.EndPopup();
                        }

                        ImGui.TableNextColumn();
                        ImGui.Text( $"{options[i]}" );
                    }
                    ImGui.EndTable();
                }
                ImGui.EndChild();
            }
            ImGui.EndTabItem();
        }
    }

    private void DrawFooter()
    {
        if (ImGui.BeginTable("SettingsUISaveCloseCancelButtonTable", 5))
        {
            ImGui.TableSetupColumn( "SettingsUIFoundBugColumn", ImGuiTableColumnFlags.WidthFixed, 100 * ImGuiHelpers.GlobalScale );
            ImGui.TableSetupColumn( "SettingsUITableSpacerColumn", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn( "SettingsUISaveAndCloseButtonColumn", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelpers.GlobalScale );
            ImGui.TableSetupColumn( "SettingsUIDefaultsButtonColumn", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelpers.GlobalScale );
            ImGui.TableSetupColumn( "SettingsUICancelButtonColumn", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelpers.GlobalScale );



            // Leave the first column blank for spacing.
            ImGui.TableNextColumn();
            if ( ImGui.Button( $"Found A Bug?", new(-1, EorzeanScribe.BUTTON_Y.Scale() ) ))
            {
                EorzeanScribeUI.ShowMessageBox( "Found a bug?", "If you found a bug, please post as much useful information as possible.\nThe more you are able to share with me the faster I can find the problem and fix it.\nUseful information could be:\n\t* Screenshots\n\t* Description of what you were doing\n\t* Number of pads open\n\t* Dalamud.log file\n\t* EorzeanScribe.json config file\n\nGo to GitHub to report the bug?", MessageBox.ButtonStyle.YesNo, (m) =>
                {
                    if ( m.Result == MessageBox.DialogResult.Yes )
                        System.Diagnostics.Process.Start( new System.Diagnostics.ProcessStartInfo( "https://github.com/MythicPalette/Wordsmith-DalamudPlugin/issues" ) { UseShellExecute = true } );
                } );                
            }


            //Skip the next column.
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            // Save and close buttons
            if (ImGui.Button("Apply", new( -1, EorzeanScribe.BUTTON_Y.Scale() ) ))
                Save();

            ImGui.TableNextColumn();
            // Reset settings to default.
            if (ImGui.Button("Defaults", new( -1, EorzeanScribe.BUTTON_Y.Scale() ) ))
            {
                EorzeanScribeUI.ShowMessageBox( $"{EorzeanScribe.APPNAME} - Restore Default Settings",
                    "Restoring defaults resets all settings to their original values\n(not including words added to your dictionary).\nProceed?",
                    buttonStyle: MessageBox.ButtonStyle.OkCancel,
                    ( mb ) =>
                    {
                        if ( (mb.Result & MessageBox.DialogResult.Ok) == MessageBox.DialogResult.Ok )
                            EorzeanScribe.ResetConfig();

                        this.IsOpen= true;
                    });
                this.IsOpen = false;
            }

            ImGui.TableNextColumn();
            // Cancel button
            if (ImGui.Button("Close", new( -1, EorzeanScribe.BUTTON_Y.Scale() ) ))
                this.IsOpen = false;

            ImGui.EndTable();
        }
    }

    public override void OnOpen()
    {
        base.OnOpen();
        ResetValues();
    }

    public void OnException(Exception e)
    {
        EorzeanScribe.PluginLog.Error( e.ToString() );
        this.IsOpen = false;
        Dictionary<string, object> dump = this.Dump();
        dump["Exception"] = new Dictionary<string, object>()
                                        {
                                            { "Error", e.ToString() },
                                            { "Message", e.Message }
                                        };
        dump["Window"] = "SettingsUI";
        EorzeanScribeUI.ShowErrorWindow( dump );
    }
    
    private void ResetValues()
    {
        // General settings.
        this._searchHistoryCountChange = EorzeanScribe.Configuration.SearchHistoryCount;
        this._researchToTopChange = EorzeanScribe.Configuration.ResearchToTop;
        this._trackWordStats = EorzeanScribe.Configuration.TrackWordStatistics;

        // Scratch Pad settings.
        this._autoClear = EorzeanScribe.Configuration.AutomaticallyClearAfterLastCopy;
        this._deleteClosed = EorzeanScribe.Configuration.DeleteClosedScratchPads;
        this._confirmDeleteClosed = EorzeanScribe.Configuration.ConfirmDeleteClosePads;
        this._ignoreHypen = EorzeanScribe.Configuration.IgnoreWordsEndingInHyphen;
        this._showChunks = EorzeanScribe.Configuration.ShowTextInChunks;
        this._onSentence = EorzeanScribe.Configuration.SplitTextOnSentence;
        this._detectHeader = EorzeanScribe.Configuration.ParseHeaderInput;
        this._sentenceTerminators = EorzeanScribe.Configuration.SentenceTerminators;
        this._encapTerminators = EorzeanScribe.Configuration.EncapsulationTerminators;
        this._chunkMarkers = EorzeanScribe.Configuration.ChunkMarkers;
        this._continuationMarker = EorzeanScribe.Configuration.ContinuationMarker;
        this._markLastChunk = EorzeanScribe.Configuration.ContinuationMarkerOnLast;
        this._scratchMaxTextLen = EorzeanScribe.Configuration.ScratchPadMaximumTextLength;
        this._scratchInputLineHeight = EorzeanScribe.Configuration.ScratchPadInputLineHeight;


        // Spell Check Settings
        this._fixDoubleSpace = EorzeanScribe.Configuration.ReplaceDoubleSpaces;
        this._maxSuggestions = EorzeanScribe.Configuration.MaximumSuggestions;
        this._autospellcheck = EorzeanScribe.Configuration.AutoSpellCheck;
        this._autospellcheckdelay = EorzeanScribe.Configuration.AutoSpellCheckDelay;
        this._punctuationCleaningString = EorzeanScribe.Configuration.PunctuationCleaningList;

        // Linkshell Settings
        this._linkshells = EorzeanScribe.Configuration.LinkshellNames;
        this._cwlinkshells = EorzeanScribe.Configuration.CrossWorldLinkshellNames;

        // Color Settings
        this._enableTextColor = EorzeanScribe.Configuration.EnableTextHighlighting;
        this._spellingErrorColor = EorzeanScribe.Configuration.SpellingErrorHighlightColor;
        this._headerColors = EorzeanScribe.Configuration.HeaderColors.Clone();
}

    private void Save()
    {
        // General Settings.
        EorzeanScribe.Configuration.SearchHistoryCount = this._searchHistoryCountChange;
        EorzeanScribe.Configuration.ResearchToTop = this._researchToTopChange;
        EorzeanScribe.Configuration.TrackWordStatistics = this._trackWordStats;

        // Scratch Pad settings.
        EorzeanScribe.Configuration.AutomaticallyClearAfterLastCopy = _autoClear;
        EorzeanScribe.Configuration.DeleteClosedScratchPads = this._deleteClosed;
        EorzeanScribe.Configuration.ConfirmDeleteClosePads = this._confirmDeleteClosed;
        EorzeanScribe.Configuration.IgnoreWordsEndingInHyphen = this._ignoreHypen;
        EorzeanScribe.Configuration.ShowTextInChunks = this._showChunks;
        EorzeanScribe.Configuration.SplitTextOnSentence = this._onSentence;
        EorzeanScribe.Configuration.ParseHeaderInput = this._detectHeader;
        EorzeanScribe.Configuration.SentenceTerminators = this._sentenceTerminators;
        EorzeanScribe.Configuration.EncapsulationTerminators = this._encapTerminators;
        EorzeanScribe.Configuration.ChunkMarkers = this._chunkMarkers;
        EorzeanScribe.Configuration.ContinuationMarker = this._continuationMarker;
        EorzeanScribe.Configuration.ContinuationMarkerOnLast = this._markLastChunk;
        EorzeanScribe.Configuration.ScratchPadMaximumTextLength = this._scratchMaxTextLen;
        EorzeanScribe.Configuration.ScratchPadInputLineHeight = this._scratchInputLineHeight;


        // Spell Check settings.
        EorzeanScribe.Configuration.ReplaceDoubleSpaces = this._fixDoubleSpace;
        EorzeanScribe.Configuration.AutoSpellCheck = this._autospellcheck;
        EorzeanScribe.Configuration.MaximumSuggestions = this._maxSuggestions;
        EorzeanScribe.Configuration.AutoSpellCheckDelay = this._autospellcheckdelay;
        EorzeanScribe.Configuration.PunctuationCleaningList = this._punctuationCleaningString;



        // Linkshell settings
        EorzeanScribe.Configuration.LinkshellNames = this._linkshells;
        EorzeanScribe.Configuration.CrossWorldLinkshellNames = this._cwlinkshells;

        // Color settings
        EorzeanScribe.Configuration.EnableTextHighlighting = this._enableTextColor;
        EorzeanScribe.Configuration.SpellingErrorHighlightColor = this._spellingErrorColor;
        EorzeanScribe.Configuration.HeaderColors = this._headerColors;

        // Save the configuration
        EorzeanScribe.Configuration.Save();
    }
}
