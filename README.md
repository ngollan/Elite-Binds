# Elite Dangerous Binds Validation

Why? Because I wanted to do it.

This is an XML schema covering the keybinds ("bindings") files of Elite Dangerous. While creating it, I found that it's not a terribly designed thing with much redundancy and little coherence.

Additionally there is functionality in a the `validate_binds.ps1` PowerShell script and in the main program that traverses binds files in the user profile and makes sure the preset name and version correspond to the file name, and that no duplicates are present to cunfuse the game.

## What would make sense?

Add annotations to the schema that group items into logical groups like the game menu does. There's also potential to build an "autofill" function that takes primary flight controls and fills in multi-crew, SRV, camera, and scanner controls.