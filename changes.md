## V0.1.1
* Added jaws config files to improve the overall experience using jaws (silencing of annoying sentinel initialized announcement, propper handling for arrow keys/escape, etc.) These can be installed via the installer or as part of the manual process.
* Added better logging for combat events to hopefully track down a duplicate announcement bug.
* Fixed incorrect localization lookups for map nodes and merchant slots.
* Map nodes now only announce traveled state (IE you have been there before); the reachable and unreachable state announcements were irrelevant and causing confusion.
* Fixed an issue where the controller focus could get stuck on the character select screen if the user moved the cursor to panels that aren't yet available (such as the ascension panel.)
* Fixed an issue for rest sites where the focus would move in extremely unpredictable ways. The buttons are now properly a navigable row.
* Added events and announcements for card upgraded, obtained card, obtained potion, and obtained relic.
* Mod settings menus are now sorted alphabetically.
* The default keyboard binding for back is now backspace instead of escape to avoid input conflicts. You will have to adjust this keybinding yourself if you have already played the game though as your settings file doesn't reset to defaults on mod update.
* Fixed an issue where the focus would glitch during the player's end of turn sequence resulting in readouts of random controls.
* Reordered card label to read as "{name}, {cost}, {type}"; also added setting for verbose costs (when unchecked costs simply read as numbers with no label.)
* Fixed an issue where various event announcements and hotkeys (such as player hp) were reading from stale data. This may also fix duplicate event announcements in combat.
* Fixed an issue where the buffer controls would cause a crash if used during the early access explanation screen.
* Fixed an issue where the read all enemy intents hotkey was only reading the numbers and not the actual intent associated with them. Also cleaned up intent formatting to be more natural (e.g. "Attack 12" instead of "Attack: 12").
* Fixed an issue where every single card was announce being added to the draw pile when the deck was reshuffled.