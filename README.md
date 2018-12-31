# Elite Dangerous Binds Validation

Why? Because I wanted to do it.

This is an XML schema covering the keybinds ("bindings") files of Elite Dangerous. While creating it, I found that it's not a terribly designed thing with much redundancy and little coherence.

Additionally there is functionality in a the `validate_binds.ps1` PowerShell script and in the main program that traverses binds files in the user profile and makes sure the preset name and version correspond to the file name, and that no duplicates are present to cunfuse the game.

## Assumptions

Lots, basically. Documentation around file format is very sparse (a `Help.txt` shipping with the game that has a December 2015 timestamp) and done by inference.

### Basic information

The `Root` element carries attributes in generated files, and different attributes in files shipping with the game:

* `PresetName`, `MajorVersion`, `MinorVersion` (only in generated files) is used by the game to display and manage files in the user profile. This information is missing in vendor profiles, I assume that in those cases, the game fills in the preset name from the file name and simply ignores version information.
* `SortOrder` (only in vendor files and always 0) is probably used to make those profiles show up in a specific order in the menu.

### Binding Types

For input assignments, the game appears to use two or three distinct input types, depending on how you count. Each input is assigned to a button or axis on a device, where a device can be "Keyboard", "Mouse", or a HID device specified by a given name from `DeviceMappings.xml` or hexadecimal USB VID/PID. Unbound controls are stored as `{NoDevice}`.

* Analogue "axes" with an input, an inversion flag, and a deadzone.
* Pushbuttons with primary and secondary inputs (each can have several modifier keys too), and in some cases a boolean "ToggleOn" flag to indicate if the control needs to be held down or toggles something on/off.

There are also a good number of more or less simple settings where the user can enable or disable functionality or select from a number of options.

## What would make sense?

Add annotations to the schema that group items into logical groups like the game menu does. There's also potential to build an "autofill" function that takes primary flight controls and fills in multi-crew, SRV, camera, and scanner controls.