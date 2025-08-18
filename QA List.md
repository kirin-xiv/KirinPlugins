# Description
This file is a checklist of important things to check during quality assurance prior to the public release of future updates. The purpose of this is to prevent as many bugs as possible in said future updates. The intention is that if all of the following items are working correctly and as intended then there should be no major issues with any future updates.

After having several releases with blatant issues that were quickly noticed, identified, and patched I decided it would be wise to have a QA list to ensure this is minimized in the future.

This list can and will certainly change over time to ensure that the quality of EorzeaScribe remains to its highest standard when and where possible.

To ensure quality for the end user, all testing should be done a RELEASE build of EorzeaScribe NOT a DEBUG build. This is to ensure that the version shipped to users works properly and not just the testing version.

# General
    [ ] Performance test

# Text Composer
    [ ] Aliases
        * Can use aliases

    [ ] Chat Headers
        * Can select headers
        * Can parse headers
        * Header lock engages/disengages
        * Linkshell names show

    [ ] Menu Bar
        * Text Composers
            ** New Text Composer
            ** Does not show current Text Composer
            ** Shows hidden composers

        * Text
            ** Clear/Undo Clear
            ** Spell Check triggers spell checking
            ** Properly copies text to clipboard.

        * Spell Check
        * Chunks
            ** Copy options copy correctly
            ** Refresh on header change
            ** Refresh on OOC change

        * View History
            ** Reload Composer State
            ** Copy Text To Clipboard
            ** Delete
            ** Delete All
            
        * Thesaurus
        * Settings
        * Help

    [ ] OOC
        * Engages and disengages properly

    [ ] Replacing Text
        * Displays correct text
        * Replaces correct text

    [ ] Text Highlighting
        * Highlight correct spelling errors
        * Highlight headers
    
    [ ] Word Wrap
        * When typing
        * When pasting on new document
        * When pasting between text
        * When resizing

# Settings
    [ ] Aliases
        * Able to create new aliases
            ** Tell target is case sensitive

        * Able to delete old aliases

    [ ] All settings affect expected parts of plugin

    [ ] All settings save

    [ ] All settings reset properly

    [ ] Bug report button


    [ ] Custom dictionary
        * Can add items
        * Can remove first item
        * Can remove last item
        * Can remove any middle item

    [ ] Custom text markers
        * Able to add markers
        * Markers show in correct positions
        * Markers show with correct ShowOn tag
        * Markers show on correct chunk mode
        * Markers show on correct OOC mode

    [ ] Dictionary list is accurate
        * Web manifest correct
        * Local files correct

    [ ] Dicionary loads properly
        * loads all entries
        * reloads with failure

# Spell Check
    [ ] Correctly identifies spelling errors
        * Does not detect contractions as errors
        * Does not count punctuation as errors
        * Does not count dates as errors
        * Does not count numerals as errors
        * Does not count time as errors.

    [ ] Correctly generates spelling error word objects

    [ ] Correctly uses the timer

    [ ] Suggestions are usable

# Thesaurus
    [ ] Searches word
        * Ignores case
    
    [ ] Only keeps the correct amount of searches

    [ ] Can delete the searches