using Dalamud.Bindings.ImGui;


namespace EorzeanScribe.Gui;

internal sealed class ErrorWindow : MessageBox
{
    private const string MESSAGE = "EorzeanScribe has encountered an error.\nCopy error dump to clipboard and open bug report page?\n\nWARNING: I WILL be able to see anything and everything\ntyped as part of the log.";
    internal Dictionary<string, object> ErrorDump = new();
    public ErrorWindow( Dictionary<string, object> dump ) : base( $"EorzeanScribe Error", MESSAGE, ButtonStyle.YesNo, Callback) { this.ErrorDump = dump; }

    public static void Callback(MessageBox mb)
    {
        if ( mb is ErrorWindow ew )
        {
            try
            {
                if ( ew.Result == DialogResult.Yes )
                {
                    foreach ( string key in ew.ErrorDump.Keys )
                    {
                        if ( ew.ErrorDump[key] is IntPtr )
                            ew.ErrorDump.Remove( key );
                    }
                    ImGui.SetClipboardText( System.Text.Json.JsonSerializer.Serialize( ew.ErrorDump, new System.Text.Json.JsonSerializerOptions() { IncludeFields = true } ) );
                    System.Diagnostics.Process.Start( new System.Diagnostics.ProcessStartInfo( "https://github.com/kirin-xiv/KirinPlugins/issues" ) { UseShellExecute = true } );
                }
            }
            catch ( Exception e )
            {
                EorzeanScribe.PluginLog.Error( e.ToString() );
            }
        }
        EorzeanScribeUI.RemoveWindow( mb );
    }
}
