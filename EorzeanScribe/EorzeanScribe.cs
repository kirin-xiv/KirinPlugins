// Author: Lady Defile
// Description: EorzeanScribe is a plugin for Dalamud; a plugin API for Final Fantasy XIV Online.
// The purpose of EorzeanScribe is to make roleplay easier. Roleplay in FFXIV requires typing
// into a small, single horizontal line with very little width. This makes it impossible to
// proofread your text for spelling and grammatical errors. The other option is to type in
// external programs but then copy/paste is a bit of guess work. EorzeanScribe aims to solve this
// by being the perfect-ish roleplay text editor.
//
// A few notes to anyone looking to explore this code and/or my future self if I forget how all of
// this code works:
// *    EorzeanScribe.cs is the main plugin interface and the entry point for Dalamud. It handles the
//          initialization of the plugin and text commands.
// *    EorzeanScribeUI.cs manages the creation, showing/hiding, and disposal of GUI windows.
// *    Extensions.cs has several extension methods.
//
// Files in the Helpers namespace are processing code removed from the file that uses them.
// This was done to help cut down on file bloat and separate functions from UI where possible.
//
// Files in the Gui namespace are GUI windows that are displayed to the user at one point or
// another for a variety of reasons.
//
// While the code may appear complicated, I've done my best to simplify and compartmentalize
// anything that I can to keep things easy to pick up. It should not be too difficult for
// others or my future self to (re)visit the code in these files and quickly rediscover
// the functions of each file as well as the flow of the program.
#region GLOBAL USING
global using System;
global using System.IO;
global using System.Linq;
global using System.Text;
global using System.Numerics;
global using System.Collections.Generic;
global using Dalamud.Logging;
global using System.Text.RegularExpressions;
#endregion

using Dalamud.Game.Command;
using Dalamud.Data;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using EorzeanScribe.Gui;
using EorzeanScribe.Helpers;
using System.Threading;
using Dalamud.Interface;
using ECommons;
using ECommons.Automation;

namespace EorzeanScribe;

public sealed class EorzeanScribe : IDalamudPlugin
{
    #region Constants
    /// <summary>
    /// Plugin name.
    /// </summary>
    internal const string APPNAME = "EorzeanScribe";

    /// <summary>
    /// The default height of a button.
    /// </summary>
    internal const int BUTTON_Y = 25;

    /// <summary>
    /// Command to open a new or specific scratch pad.
    /// </summary>
    private const string SCRATCH_CMD_STRING = "/scratchpad";

    /// <summary>
    /// Command to open the settings window.
    /// </summary>
    private const string SETTINGS_CMD_STRING = "/wordsmith";

    /// <summary>
    /// Command to open the thesaurus.
    /// </summary>
    private const string THES_CMD_STRING = "/thesaurus";

    /// <summary>
    /// Main unified command for EorzeanScribe.
    /// </summary>
    private const string MAIN_CMD_STRING = "/eorzeascribe";

    /// <summary>
    /// Short alias for the main command.
    /// </summary>
    private const string SHORT_CMD_STRING = "/es";

    /// <summary>
    /// The maximum length of scratch text.
    /// </summary>
    internal const int MAX_SCRATCH_LENGTH = 32768 ;
    #endregion

    /// <summary>
    /// Plugin name interface property.
    /// </summary>
    public string Name => APPNAME;

    #region Plugin Services
    // All plugin services are automatically populated by Dalamud. These are important
    // classes that are used to directly interface with the plugin API.
    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    internal static IDataManager DataManager { get; private set; } = null!;
    #endregion

    [PluginService]
    internal static IPluginLog PluginLog { get; private set; } = null!;

    [PluginService]
    internal static ITextureProvider TextureProvider { get; private set; } = null!;

    [PluginService]
    internal static INotificationManager NotificationManager { get; private set; } = null!;
    
    [PluginService]
    internal static IChatGui ChatGui { get; private set; } = null!;


    /// <summary>
    /// <see cref="Configuration"/> holding all configurable data for the plugin.
    /// </summary>
    internal static Configuration Configuration { get; private set; } = null!;



    #region Constructor and Disposer
    /// <summary>
    /// Default constructor and initializer for the EorzeanScribe plugin.
    /// </summary>
    public EorzeanScribe()
    {
        // Initialize ECommons for chat functionality
        ECommonsMain.Init(PluginInterface, this);
        
        // Get the configuration.
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();


        // Add unified commands for EorzeanScribe
        CommandManager.AddHandler(MAIN_CMD_STRING, new CommandInfo( OnEorzeanScribeCommand )
        {
            HelpMessage = "Main EorzeanScribe command. Use '/eorzeascribe help' for available options."
        });
        CommandManager.AddHandler(SHORT_CMD_STRING, new CommandInfo( OnEorzeanScribeCommand )
        {
            HelpMessage = "Short alias for EorzeanScribe. Use '/es help' for available options."
        });

        // Add legacy commands for backward compatibility
        CommandManager.AddHandler(THES_CMD_STRING, new CommandInfo( ( c, a ) => { EorzeanScribeUI.ShowThesaurus(); } ) { HelpMessage = "[Legacy] Display the thesaurus window. Use '/es thesaurus' instead." });
        CommandManager.AddHandler(SETTINGS_CMD_STRING, new CommandInfo( ( c, a ) => { EorzeanScribeUI.ShowSettings(); }) { HelpMessage = "[Legacy] Display the configuration window. Use '/es settings' instead." });
        CommandManager.AddHandler(SCRATCH_CMD_STRING, new CommandInfo( OnScratchCommand )
        {
            HelpMessage = "[Legacy] Opens or creates a scratch pad. Use '/es' or '/es pad [name]' instead."
        });

        PluginInterface.UiBuilder.OpenMainUi += EorzeanScribeUI.ShowScratchPad;



        // Register handlers for draw and openconfig events.
        PluginInterface.UiBuilder.Draw += EorzeanScribeUI.Draw;
        PluginInterface.UiBuilder.Draw += EorzeanScribeUI.Update;
        PluginInterface.UiBuilder.OpenConfigUi += EorzeanScribeUI.ShowSettings;

        // Initialize the dictionary.
        Lang.Init();
    }

    /// <summary>
    /// Disposal method for cleaning the plugin.
    /// </summary>
    public void Dispose()
    {
        // Remove events first to prevent any further UI operations
        PluginInterface.UiBuilder.Draw -= EorzeanScribeUI.Draw;
        PluginInterface.UiBuilder.Draw -= EorzeanScribeUI.Update;
        PluginInterface.UiBuilder.OpenConfigUi -= EorzeanScribeUI.ShowSettings;
        PluginInterface.UiBuilder.OpenMainUi -= EorzeanScribeUI.ShowScratchPad;

        // Remove command handlers.
        CommandManager.RemoveHandler(MAIN_CMD_STRING);
        CommandManager.RemoveHandler(SHORT_CMD_STRING);
        CommandManager.RemoveHandler(THES_CMD_STRING);
        CommandManager.RemoveHandler(SETTINGS_CMD_STRING);
        CommandManager.RemoveHandler(SCRATCH_CMD_STRING);

        // Dispose of the UI after removing draw events
        EorzeanScribeUI.Dispose();
        
        // Dispose ECommons
        ECommonsMain.Dispose();
    }

    internal static void ResetConfig()
    {
        Configuration = new();
        Configuration.Save();
    }
    #endregion
    #region Event Callbacks
    private void OnScratchCommand(string command, string args)
    {
        if ( int.TryParse( args.Trim(), out int x ) )
            EorzeanScribeUI.ShowScratchPad( x );
        else if ( args.Trim().Length > 3 )
            EorzeanScribeUI.ShowScratchPad( args );
        else
            EorzeanScribeUI.ShowScratchPad();
    }

    private void OnEorzeanScribeCommand(string command, string args)
    {
        var parts = args.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var subcommand = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";
        var subargs = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "";

        switch (subcommand)
        {
            case "pad":
            case "scratch":
            case "scratchpad":
                if (int.TryParse(subargs.Trim(), out int padId))
                    EorzeanScribeUI.ShowScratchPad(padId);
                else if (subargs.Trim().Length > 0)
                    EorzeanScribeUI.ShowScratchPad(subargs.Trim());
                else
                    EorzeanScribeUI.ShowScratchPad();
                break;

            case "settings":
            case "config":
            case "configuration":
                EorzeanScribeUI.ShowSettings();
                break;

            case "thesaurus":
            case "thes":
                EorzeanScribeUI.ShowThesaurus();
                break;

            case "help":
            case "?":
                ShowHelpMessage();
                break;

            case "":
                // No subcommand - show main scratch pad
                EorzeanScribeUI.ShowScratchPad();
                break;

            default:
                // Unknown subcommand - show help
                PluginLog.Information($"Unknown EorzeanScribe subcommand: {subcommand}");
                ShowHelpMessage();
                break;
        }
    }

    private void ShowHelpMessage()
    {
        var helpText = "EorzeanScribe Commands:\n" +
                      "/es or /eorzeascribe - Open main writing pad\n" +
                      "/es pad [id/name] - Open specific writing pad\n" +
                      "/es settings - Open settings window\n" +
                      "/es thesaurus - Open thesaurus window\n" +
                      "/es help - Show this help message\n\n" +
                      "Legacy commands still work:\n" +
                      "/scratchpad, /wordsmith, /thesaurus";
        
        NotificationManager.AddNotification(new()
        {
            Content = helpText,
            Title = "EorzeanScribe Help",
            Type = Dalamud.Interface.ImGuiNotification.NotificationType.Info
        });
    }

    #endregion
}