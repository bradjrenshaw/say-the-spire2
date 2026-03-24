# Say the Spire 2

An accessibility mod for Slay the Spire 2 that provides screen reader support for blind players.

## Features

The mod is currently in an early prototype state. This prototype is intended for those who have already purchased the game or want to help test. Most gameplay functionality works at a level similar to Say the Spire 1, though there will be bugs and not everything is fully accessible yet. The following is supported:

* Full keyboard and controller support
* Menus: main menu, settings, character/mode select, the timeline, all event dialogs, pause menu, card selection prompts, game over screens, card pile screens
* Map viewer: tree-based map navigation with room types, traveled/untraveled/unreachable state, and quest icons
* Combat: screen traversal of hand, players, enemies, orbs, relics, potions, and informational tooltips. Readouts of powers gained/lost, block changes, damage, and more (configurable from mod settings)
* Buffer system: detailed review of all information for focused elements and persistent on-screen information, including the player, cards, relics, potions, creatures, and UI tooltips
* Additional keybindings: quickly read out information such as gold, HP, energy, enemy intents, and more
* Mod settings: configure which events are read out, speech settings, and map viewer settings. Keybindings and controller bindings can also be fully customized from within the mod

The following are known to be unsupported:

* Multiplayer (untested, though parts of it likely work)
* Ascensions/seed selection
* Daily run screen
* Custom run screen
* The Compendium
* Credits screen

## Important Notes

* **Controller players:** You must disable Steam Input for Slay the Spire 2. This is a limitation of how the game integrates with Steam Input. See the "Disabling Steam Input" section below for instructions.
* **Keybindings:** The mod has its own input system for keyboard and controller. Changing bindings in the game's settings will not affect the mod. To adjust keybindings, open the mod settings with Ctrl+M (keyboard) or LT+Start (controller).

## Installation

There are two ways to install the mod: using the standalone installer or manually copying files.

### Installer (Recommended)

The installer downloads the latest release, copies files to the correct location, and modifies your game settings to enable mods.

* **Important:** You must run the game at least once before installing, so the game creates its settings file.
* Download the installer from https://github.com/bradjrenshaw/say-the-spire2/releases/latest
* Run the installer. It should detect your game directory automatically. If it doesn't, use the browse button to select it. Click Install.
* If you use JAWS, click "Install JAWS config files" and select your JAWS settings directory.
* Launch the game normally. You should hear the mod start speaking.

To update the mod later, run the installer again and click Update.

### Manual Installation

If you prefer not to use the installer or it doesn't work, you can set things up manually.

* **Important:** You must launch the game at least once first so it creates the necessary settings file.
* Download the latest zip release from https://github.com/bradjrenshaw/say-the-spire2/releases/latest
* Extract the zip to your game's root directory. On Windows, the default is `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2`. You should see screen reader files in the root directory and a `mods` subfolder.
* If you use JAWS, copy the files from the `jaws` subfolder in the game directory to your JAWS settings directory.
* Next, modify the game's settings file. On Windows, this is located in `%APPDATA%/SlayTheSpire2`.
* Open the `steam` subfolder, then open the subfolder with a long number (your Steam account ID).
* Open the `settings.save` file in a text editor (Notepad works well; avoid Word or similar). Search for `mod_settings` (Ctrl+F is helpful as it's a large JSON file). You should find `"mod_settings": null`. Replace `null` with `{"mods_enabled": true}`, so the line reads `"mod_settings": {"mods_enabled": true}`. Take care not to alter any surrounding formatting. Save and close the file.
* Launch the game normally. If everything is set up correctly, the mod should begin speaking.

### Disabling Steam Input

If you want to play with a controller, you must disable Steam Input for Slay the Spire 2. To do this:

* **Big Picture Mode:** Navigate to Slay the Spire 2 in your library. Move right to Manage, select it, then move down to Properties. Under the Controller tab, find the combo box labeled "Override for Slay the Spire 2". Select Disable Steam Input.
* **Regular View:** In your game library, right-click Slay the Spire 2 (or click the manage/gear button). Click Properties, go to the Controller tab, and set the Steam Input override to Disable.

## Getting Started

### Controls

The mod supports both keyboard and controller. Here are the default bindings:

| Action | Keyboard | Controller |
|---|---|---|
| Navigate | Arrow keys | D-pad / Left stick |
| Select | Enter | A |
| Accept | E | Y |
| Cancel / Back | Backspace | B |
| Peek | Space | LS Click |
| View Draw Pile | A | LT+LB |
| View Discard Pile | S | RT+RB |
| View Deck / Tab Left | D | LB |
| View Exhaust / Tab Right | F | RB |
| View Map | M | Back |
| Pause | Escape | Start |
| Top Panel | T | X |
| Announce Gold | Ctrl+G | RT+A |
| Announce HP | Ctrl+H | LT+A |
| Announce Block | Ctrl+B | LT+B |
| Announce Energy | Ctrl+Y | LT+X |
| Announce Powers | Ctrl+P | LT+Y |
| Announce Intents | Ctrl+I | RT+Y |
| Announce Summarized Intents | Alt+I | RT+X |
| Announce Boss | Ctrl+N | RT+B |
| Announce Relic Counters | Ctrl+R | RT+Back |
| Mod Settings | Ctrl+M | LT+Start |
| Toggle Accessibility | Ctrl+Shift+A | — |
| Select Card 1-10 | 1-0 | — |

The buffer system lets you review detailed information about whatever is currently focused. Use Ctrl+Up/Down (or right stick up/down) to cycle through items in the current buffer, and Ctrl+Left/Right (or right stick left/right) to switch between buffers.

All keybindings can be customized from the mod settings menu.

### Map Viewer

When you focus a map node, the mod's map viewer activates. The map is presented as a tree that you navigate with Ctrl+Arrow keys (keyboard) or the right stick (controller):

* **Forward/Back (Ctrl+Up/Down or RS Up/Down):** Move forward (toward the boss) or backward through the map path.
* **Switch Branch (Ctrl+Left/Right or RS Left/Right):** When a path splits, switch between the available branches.
* Each node is announced with its type (Monster, Elite, Shop, Treasure, Rest Site, etc.), coordinates, and travel state (travelable, traveled, or unreachable).
* Nodes marked by quest cards or relics are announced with a "Quest" prefix.
* **Auto-advance** (configurable in mod settings) automatically skips through linear paths, announcing each room along the way and stopping at the next branch point.

## Credits
I would like to thank everyone who has tested the mod so far, the feedback has been invaluable and the mod is much better as a result. I woud also like to thank those who have directly contributed to the mod:

* Rashad Naqeeb for contributing the Jaws configuration files
* QgSama for contributing the Simplified Chinese localization.