## V0.1.4
* The Python installer has been replaced with one coded in Rust. This should prevent Windows Defender eroniously flagging the installer as a virus and improve stability of the app overall.
* Fixed numerous focus issues when navigating between rows on the combat screen. Creatures and Defect orbs should now consistently be navigable.
* Added position announcements to combat screen rows (e.g. "card 3 of 5").
* Added left/right wrap-around navigation for the relics row.
* Added support for the Crystal sphere/divination event. I did my best to match what sighted players are able to see but the event is somewhat nonintuitive; any feedback on how I can make this clearer would be greatly appreciated.
* Added "Announce Intent Before HP" setting under UI/Creature. When enabled, creature focus reads intent before HP (e.g. "Slime, attack 7, 50/50 HP" instead of "Slime, 50/50 HP, Intent attack 7").
* Added a setting for each UI element to toggle position announcements on/off (defaults to on for all.)
* There is now a hotkey to read out all relic counters (keyboard: ctrl+r, controller: rt+back)
* fixed incorrect controller default for view exhaust/tab right (it is now rt+rb as intended.)
* Changed the default keyboard binding for view exhaust/tab right from X to F (A/S/D/F row).
* The map viewer can now be opened during combat, events, and other screens by pressing the map key (M / Back). It starts on the next node ahead and optionally announces your current location.
* Added "Announce Current Location When Map Opens" setting under Map (defaults to on).
* View exhaust/tab right on keyboard is now f instead of x by default.
* Pressing the map key to view the map from anywhere now properly allows you to browse the map with the usual controls.

## V0.1.3
This is an emergency release to fix a bug where any events would not be announced during a run if you started a run from the character select screen. The character select screen was not properly being removed from memory, which was causing an uncaught exception that silently aborted the event queue processing (so it never got to the events to announce them.)

## V0.1.2
* Added orb support for the Defect: orbs now read their name, passive/evoke values, and empty slot tooltips. This also fixes a bug where all Defect orbs were reading out as "The Defect"... Oops.
* Added event announcements for orb channeled and orb evoked, with per-type toggles.
* Added timeline top panel shortcut to jump to the first revealable epoch.
* Improved readouts and card prompts for Choose a Card screens (e.g. Survivor discard, Armaments upgrade).
* Added verbose logging toggle under Advanced settings.
* Added per-type toggles for HP change events (announce damage, announce heals).
* Added per-variant toggles for block events (announce gained, announce lost, announce all lost) and a setting to include or hide block totals.
* Added per-type focus string settings to toggle type, subtype, and tooltip announcements for each UI element type (card, relic, potion, creature, etc.)
* Added rarity to relic and potion buffer readouts.
* Improved focus string format for more consistent and informative readouts. For example, if you don't want to hear that each map node is in fact a map node since you already have context, you can toggle that off.
* Removed colon punctuation from focus string outputs (e.g. "Intent Attack 7" instead of "Intent: Attack 7"). This should flow much smoother now and feel faster.
* Improved performance when processing controller input. This should feel significantly better now; please report if anything breaks as a result.
* Merchant items, rewards, and other wrapped elements now use the same buffer and focus string behavior as their underlying type (e.g. a merchant card reads the same as a regular card).
* Fixed star cost readouts not showing on Regent character cards; energy and star costs are now combined on one line. Regent stars are also included in the player buffer and energy hotkey readouts.
* Fixed various bugs with screen state that could cause stale data or unexpected behavior after saving, quitting, or switching screens. This should hopefully finally get rid of the duplicated event announcements in combat.
* Fixed focus issues during card selection screens. This should fix certain selections not reading, such as choosing a card for Well Laid Plans.
* Added announce boss keybind (Ctrl+N / RT+B) to read the current act boss. Supports double boss in higher ascensions.
* Added announce summarized intents keybind (Alt+I / RT+X) to read total incoming damage from all enemies.
* Added ascension level announcements on the character select screen, reading the level, title, and description when changed.
* Added accessibility for the card pack bundle selection screen (Scrollboxes event). Pack contents are read out, and preview cards can be navigated with left/right.
* Added unlock requirements and tooltip text to the random character button.
* Fixed intent names using internal enum names instead of the game's localized names.
* Fixed foul potion "throw at merchant" not completing when using keyboard/controller.
* Fixed creature focus not updating when bosses die and resummon in a new form.
* Fixed block lost event not reading when all block was lost with "Announce All Block Lost" disabled but "Announce Block Lost" enabled.
* Fixed empty treasure chest not announcing when opened.
* Changed the default Top Panel keyboard binding from Tab to T.

## V0.1.1
* Added jaws config files to improve the overall experience using jaws (silencing of annoying sentinel initialized announcement, propper handling for arrow keys/escape, etc.) These can be installed via the installer or as part of the manual process.
* Added better logging for combat events to hopefully track down a duplicate announcement bug.
* Fixed incorrect localization lookups for map nodes and merchant slots.
* Map nodes now only announce traveled state (IE you have been there before); the reachable and unreachable state announcements were irrelevant and causing confusion.
* Fixed an issue where the controller focus could get stuck on the character select screen if the user moved the cursor to panels that aren't yet available (such as the ascension panel.)
* Fixed an issue for rest sites where the focus would move in extremely unpredictable ways. The buttons are now properly a navigable row.
* Added events and announcements for card upgraded, obtained card, obtained potion, and obtained relic.
* End of turn Hand Discarded and start of turn Deck Shuffled announcements can now be toggled under the card piles event.
* Mod settings menus are now sorted alphabetically.
* The default keyboard binding for back is now backspace instead of escape to avoid input conflicts. You will have to adjust this keybinding yourself if you have already played the game though as your settings file doesn't reset to defaults on mod update.
* Fixed an issue where the focus would glitch during the player's end of turn sequence resulting in readouts of random controls.
* Reordered card label to read as "{name}, {cost}, {type}"; also added setting for verbose costs (when unchecked costs simply read as numbers with no label.)
* Fixed an issue where various event announcements and hotkeys (such as player hp) were reading from stale data. This may also fix duplicate event announcements in combat.
* Fixed an issue where the buffer controls would cause a crash if used during the early access explanation screen.
* Fixed an issue where the read all enemy intents hotkey was only reading the numbers and not the actual intent associated with them. Also cleaned up intent formatting to be more natural (e.g. "Attack 12" instead of "Attack: 12").
* Fixed an issue where every single card was announce being added to the draw pile when the deck was reshuffled.
