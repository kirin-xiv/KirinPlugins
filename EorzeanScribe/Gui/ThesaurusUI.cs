using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility;
using Dalamud.Interface;
using EorzeanScribe.Helpers;
using EorzeanScribe;
using System.Numerics;
using System.Collections.Generic;
using static Dalamud.Interface.UiBuilder;

namespace EorzeanScribe.Gui;

internal sealed class ThesaurusUI : Window, IDisposable
{
    private string _query = "";

    private ThesaurusAPI _searchHelper;

    internal static string GetWindowName() => $"{EorzeanScribe.APPNAME} - Thesaurus";
    /// <summary>
    /// Instantiates a new ThesaurusUI object.
    /// </summary>
    public ThesaurusUI() : base(GetWindowName())
    {
        this._searchHelper = new ThesaurusAPI();

        this.SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new( 375, 330 ),
            MaximumSize = new( 9999, 9999 )
        };

        this.Flags |= ImGuiWindowFlags.NoScrollbar;
        this.Flags |= ImGuiWindowFlags.NoScrollWithMouse;
        //Flags |= ImGuiWindowFlags.MenuBar;
    }

    #region Drawing
    /// <summary>
    /// The Draw entry point for Dalamud.Interface.Windowing
    /// </summary>
    public override void Draw()
    {
        // Create a child element for the word search.
        if ( ImGui.BeginChild( "Word Search Window" ) )
        {
            DrawSearchBar();
            if ( ImGui.BeginChild( "SearchResultWindow" ) )
            {
                DrawSearchErrors();
                for ( int i = 0, c = this._searchHelper.History.Count; i < c; )
                {
                    WordSearchResult result = this._searchHelper.History[i];
                    if ( DrawSearchResult( result ) )
                        i++;
                    else
                    {
                        c--;
                        this._searchHelper.DeleteResult(result);
                    }
                }

                // End the child element.
                ImGui.EndChild();
            }
            ImGui.EndChild();
        }
    }

    /// <summary>
    /// Draws the search bar on the UI.
    /// </summary>
    private void DrawSearchBar()
    {
        float btnWidth = 100*ImGuiHelpers.GlobalScale;
        ImGui.SetNextItemWidth( ImGui.GetWindowContentRegionMax().X - btnWidth - ImGui.GetStyle().FramePadding.X * 2 );

        if ( ImGui.InputTextWithHint( "###ThesaurusSearchBar", "Search...", ref _query, 128, ImGuiInputTextFlags.EnterReturnsTrue ) )
        {
            this._searchHelper.SearchThesaurus( this._query );
            this._query = "";
        }

        ImGui.SameLine();
        if ( ImGui.Button( "Search##ThesaurusSearchButton", new( btnWidth, 0 ) ) )
        {
            this._searchHelper.SearchThesaurus( this._query );
            this._query = "";
        }

        ImGui.Separator();
    }

    /// <summary>
    /// Draws the last search's data to the UI.
    /// </summary>
    private void DrawSearchErrors()
    {
        if ( this._searchHelper.State == ApiState.Failed )
        {
            ImGui.TextColored( new Vector4( 255, 0, 0, 255 ), $"Search failed. Try again or use a different word." );
            ImGui.Separator();
        }
        else if ( this._searchHelper.State == ApiState.Searching )
        {
            ImGui.Text( "Searching..." );
            ImGui.Separator();
        }
    }

    /// <summary>
    /// Draws one search result item to the UI.
    /// </summary>
    /// <param name="result">The search result to be drawn</param>
    /// <returns><see langword="true"/> if visible; otherwise <see langword="false"/>.</returns>
    private bool DrawSearchResult(WordSearchResult result)
    {
        if (result != null)
        {
            // Default to visible.
            bool vis = true;
            if (ImGui.CollapsingHeader($"{result.Query.Trim().CaplitalizeFirst()}##{result.ID}", ref vis, ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();
                foreach (ThesaurusEntry entry in result?.Entries ?? Array.Empty<ThesaurusEntry>())
                    DrawEntry(entry);
                ImGui.Unindent();
            }

            // return the visibility state.
            return vis;
        }
        return false;
    }

    /// <summary>
    /// Draws a search result's entry data. One search result can have multiple data entries.
    /// </summary>
    /// <param name="entry">The data to draw.</param>
    private void DrawEntry(ThesaurusEntry entry)
    {
        if ( ImGui.CollapsingHeader($"{entry.Type.Trim().CaplitalizeFirst()} - {entry.Definition.Replace("{it}", "").Replace("{/it}", "")}##{entry.ID}"))
        {
            ImGui.Indent();

            if (entry.Synonyms.Count > 0)
            {
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(new Vector4(0.4f, 0.8f, 0.4f, 1.0f), FontAwesomeIcon.Heart.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.4f, 0.8f, 0.4f, 1.0f), " Synonyms");
                ImGui.Spacing();
                DrawWordButtons(entry.Synonyms, new Vector4(0.3f, 0.7f, 0.3f, 1.0f));
            }

            if (entry.Related.Count > 0)
            {
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(new Vector4(0.4f, 0.6f, 0.8f, 1.0f), FontAwesomeIcon.Link.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.4f, 0.6f, 0.8f, 1.0f), " Related Words");
                ImGui.Spacing();
                DrawWordButtons(entry.Related, new Vector4(0.3f, 0.5f, 0.7f, 1.0f));
            }

            if (entry.NearAntonyms.Count > 0)
            {
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(new Vector4(0.8f, 0.6f, 0.4f, 1.0f), FontAwesomeIcon.ExclamationTriangle.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.8f, 0.6f, 0.4f, 1.0f), " Near Antonyms");
                ImGui.Spacing();
                DrawWordButtons(entry.NearAntonyms, new Vector4(0.7f, 0.5f, 0.3f, 1.0f));
            }

            if (entry.Antonyms.Count > 0)
            {
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(new Vector4(0.8f, 0.4f, 0.4f, 1.0f), FontAwesomeIcon.HeartBroken.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.8f, 0.4f, 0.4f, 1.0f), " Antonyms");
                ImGui.Spacing();
                DrawWordButtons(entry.Antonyms, new Vector4(0.7f, 0.3f, 0.3f, 1.0f));
            }
            ImGui.Unindent();
        }
    }

    /// <summary>
    /// Draws a collection of words as clickable buttons in a flowing layout
    /// </summary>
    /// <param name="words">The collection of words to display</param>
    /// <param name="buttonColor">The color for the buttons</param>
    private void DrawWordButtons(IReadOnlyList<string> words, Vector4 buttonColor)
    {
        float windowWidth = ImGui.GetContentRegionAvail().X;
        float buttonPadding = ImGui.GetStyle().ItemSpacing.X;
        float currentLineWidth = 0f;
        
        ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(buttonColor.X + 0.1f, buttonColor.Y + 0.1f, buttonColor.Z + 0.1f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(buttonColor.X + 0.2f, buttonColor.Y + 0.2f, buttonColor.Z + 0.2f, 1.0f));
        
        for (int i = 0; i < words.Count; i++)
        {
            string word = words[i].Trim();
            if (string.IsNullOrEmpty(word)) continue;
            
            Vector2 buttonSize = ImGui.CalcTextSize(word);
            buttonSize.X += ImGui.GetStyle().FramePadding.X * 2;
            buttonSize.Y += ImGui.GetStyle().FramePadding.Y * 2;
            
            // Check if button fits on current line
            if (currentLineWidth > 0 && currentLineWidth + buttonSize.X + buttonPadding > windowWidth)
            {
                // Start new line
                currentLineWidth = 0f;
            }
            
            // Add button to same line if not the first on the line
            if (currentLineWidth > 0)
            {
                ImGui.SameLine();
            }
            
            // Draw the button
            if (ImGui.Button($"{word}##thes_btn_{i}"))
            {
                // When clicked, search for this word
                this._query = word;
                this._searchHelper.SearchThesaurus(word);
            }
            
            // Add tooltip showing the word (useful for long words that might be truncated)
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"Click to search for '{word}'");
            }
            
            currentLineWidth += buttonSize.X + buttonPadding;
        }
        
        ImGui.PopStyleColor(3);
        ImGui.Spacing();
    }
    #endregion

    /// <summary>
    ///  Disposes of the SearchHelper child.
    /// </summary>
    public void Dispose() => this._searchHelper.Dispose();
}
