using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;

namespace EorzeanScribe.Gui;

internal sealed class ScratchPadHelpUI : Window
{

    internal ScratchPadHelpUI() : base($"{EorzeanScribe.APPNAME} - Help")
    {
        this.SizeConstraints = new()
        {
            MinimumSize = new( 300, 450 ),
            MaximumSize = new( 9999, 9999 )
        };
    }
    public override void Draw()
    {
        ImGui.Text("Getting Started");
        ImGui.Separator();
        ImGui.Spacing();
        
        ImGui.TextWrapped("EorzeanScribe helps you write longer messages for roleplay in FFXIV. Since the game limits how much you can type at once, this plugin automatically breaks your text into chunks that fit.");
        
        ImGui.Spacing();
        ImGui.Text("How to Use");
        ImGui.Separator();
        ImGui.Spacing();
        
        ImGui.TextWrapped("1. Choose your chat channel from the dropdown at the top (Say, Party, Tell, etc.)");
        ImGui.TextWrapped("2. Type your message in the large text box - write as much as you want!");
        ImGui.TextWrapped("3. Your text automatically gets split into chunks that fit FFXIV's chat limit");
        ImGui.TextWrapped("4. Each chunk has its own Copy and Post buttons:");
        ImGui.Indent();
        ImGui.TextWrapped("• Copy - Copies that chunk to your clipboard");
        ImGui.TextWrapped("• Post to Chat - Sends it directly to the game chat");
        ImGui.Unindent();
        
        ImGui.Spacing();
        ImGui.Text("Spell Checking");
        ImGui.Separator();
        ImGui.Spacing();
        
        ImGui.TextWrapped("Spell checking happens automatically as you type. When there's a misspelling:");
        ImGui.Indent();
        ImGui.TextWrapped("• The word appears in red in your text");
        ImGui.TextWrapped("• Suggestions appear below the text box");
        ImGui.TextWrapped("• Click a suggestion to fix it instantly");
        ImGui.TextWrapped("• Or type your own correction and press Enter");
        ImGui.TextWrapped("• Click 'Add to Dictionary' to remember custom words");
        ImGui.Unindent();
        
        ImGui.Spacing();
        ImGui.Text("Tips");
        ImGui.Separator();
        ImGui.Spacing();
        
        ImGui.TextWrapped("• The Clear button at the bottom wipes your text (with undo available)");
        ImGui.TextWrapped("• Access the Thesaurus from the menu to find better words");
        ImGui.TextWrapped("• Check Settings to customize chunk markers and text colors");
        ImGui.TextWrapped("• For /tell, enter the recipient as 'Name@Server' or use placeholders like <t> for target");
        
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "Happy roleplaying!");
    }
}
