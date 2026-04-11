## V0.4.0
* Significantly refactored the code to allow more of the mod's text to be localized. This should allow most, if not all, text to be localized for real this time. I suspect some strings may be still missing though, please report any that are.
* Added better logging for some errors that may occur in rare circumstances (failed Harmony patches to game methods, etc.) If game updates change anything this should make catching it easier.
* The keybindings in settings are now grouped into categories to make browsing the list much easier (thanks to @amerikrainian).
* Added propper support for the card compendium thanks to @amerikrainian.
* Added a show enemy intent setting to the enemy moves combat event. This will announce the intent of the enemy's move at the start of its turn.
* Added player turn start, player turn end, enemy turn start, enemy turn end, and show rounds settings to the turns event.
* Added help messages to the hand select screen and the Crystal Sphere divination event.
* Added support for the dev console (use ` on any screen to access it by default.)
* Added support for relic vote announcements as well as announcements of map node votes when focusing a map node (thanks @amerikrainian)
* Added badges to leaderboard screen (thanks @amerikrainian)

## V0.3.0
* Fixed an issue where localization was only using English and not the current game language.
* Fixed an issue where the Chinese localization would not load due to using the wrong localization code.
* All mod text should now be localizable (added roughly 120 missing strings to the locale files.)
* The game's act banner text is now announced (the act name and number is displayed at the start of each act visually.)
* Fixed a bug where card upgrades would be read out at incorrect times (for example, reward screens with upgraded cards would trigger the upgrade event erroniously)
* Various map improvements thanks to @amerikrainian:
    * You can now browse points of interest (shops, elites, etc) with dedicated controls.
    * You can mark or unmark those points. Doing so will modify the text of map nodes, telling you whether or not that node will lead you towards that target or diverge away from it.
* Various multiplayer improvements and fixes thanks to @amerikrainian:
    * Player intents are now read
    * Fixed a bug where the player buffer would not properly update to match the current player when unhovering another player.
    * Fixed a bug where player names in multiplayer would be read out as their steam ID (a string of numbers) instead of their display name.
    * Fixed a bug where player join announcements would not work when hosting a saved game.
* Added several screens thanks to @amerikrainian:
    * The daily run screen, including leaderboard, is now fully supported.
    * The custom run screen is now fully supported.
    * Added support for run history, stats, potion lab, and relics screens in the compendium.
* You can now press f1 for context-sensitive help on any screen. Use the up/down controls to navigate through all available messages on each screen. If a message contains multiple actions, use left/right to browse each action individually.
* Fixed a bug where UI elements that yielded the same text (such as card reward buttons with identical labels) would be treated as the same element.
* Fixed a bug where epochs within each timeline era were listed in reverse order.
* Fixed a bug where controller focus would act strangely in rest sites if you have the ability to choose multiple actions (control focus neighbors were not being continuously updated.)
* Fixed a bug where ascension text on the character select screen would be read before newly focused characters. The text is now also part of the tooltip for the character button.
* Fixed a bug where post-combat rewards were often missing their type and tooltip.
* Fixed a bug where creatures would have misleading potion info when targeting them with potions or cards. For example, with 3 enemies, the first would be labeled "2 of 4" instead of "1 of 3" due to allies being included in the creature count.

## V0.2.0
* The mod now has documentation. It can be viewed on the github for the latest version [here](https://bradjrenshaw.github.io/say-the-spire2). Alternatively you can select view Documentation from the mod menu.
* The mod now has a Simplified Chinese localization (thanks to QgSama.) Note that not all text is localized; more text will be localizable in future mod updates.
* Fixed a number of focus issues with potions and relics outside of combat. The potion slots are now properly treated as separate elements and the relics row now wraps properly.
* Fixed players in multiplayer missing most information from their buffers.
* Fixed the hand card selection screen not wrapping horizontally.
* Fixed a bug where the character select screen was not properly removed from memory when it was closed, leading to a number of focus issues when attempting to start more than one run in a single session. Again. I think for real this time.
* Fixed an issue where all card hover tips in buffers were reported as "stolen card".
* Implemented missing events for Card Played, Potion Used, and End Turn. These default to not reading for the player but reading for other players; this can be configured from event settings.
* You can now press confirm or select on a player in multiplayer to open their expanded screen. this displays their hand, relics, and potions.
* Fixed an issue where power gained announcements for stackless powers (such as Shrink) would not trigger.
* Fixed an issue where certain settings in the settings menu were listed in the wrong order (such as the speech handler selection.)

## V0.1.7
* Updated mod manifest to new game format (added `id` field). The mod now works with the latest Slay the Spire 2 update.
* Suppressed mouse hover from stealing focus during keyboard and controller navigation. Previously, if the mouse cursor was on screen, it could cause erratic focus jumping. This could happen even if you didn't have a mouse plugged in or you were using a laptop trackpad for various reasons. Hopefully this fixes the last of the eratic focus issues but please report anything acting oddly.
* Fixed character select screen container announcing "Lobby" in singleplayer (now says "Characters").
* Fixed an issue where reported map coordinates were inconsistent between various screens (for example multiplayer votes were off by 1 in both x and y.)

## V0.1.6
* Emergency fix for a bug introduced in V0.1.5 which caused the shop items to be processed by our mod as generic buttons, making the shop basically unusable.
* fixed an issue where star costs for Regent cards were not including any modifiers (such as Void Form.)
* Fixed an issue where card rewards has their cards reversed.

## V0.1.5
* The installer has received significant updates and it is highly recommended that you download the new version of it. You can now choose which version of the mod to install (including test releases.) You can also choose to disable screenreader support in the installer itself; this is for sighted players so they can play with you in multiplayer (everyone must have the same mods installed.)
* Fixed a bug in the installer where it would consider earlier mod versions as updates.
* Fixed controller bindings: View Exhaust / Tab Right now defaults to just RB (was RT+RB, conflicting with View Discard Pile). Note that if you were using an earlier version of the mod, you must update this binding yourself or reset bindings to defaults.
* The card buffer now condenses name, type, and rarity onto the first line (e.g. "Strike, Attack, Basic"). Feedback would be appreciated for this one.
* Added multiplayer lobby accessibility: character select buttons, player list buffer (shows connected players with character and ready status), join/leave/character change announcements, and ready state announcements.
* Added multiplayer voting announcements for map path voting and shared event voting. Announces who voted for what and the final result.
* Added act ready-up announcements ("Waiting for other players", "All players ready") and network timeout warnings.
* Added event source filtering: events with creature sources now have per-source-type toggles (Current Player, Other Players, Enemies) under a Sources subcategory in settings. If you don't want to hear other players losing block for example, you can turn that off. Events from sources the game doesn't provide visual feedback for are silently dropped in multiplayer (matches game behavior; you don't see when another player draws a card for example.)
* Multiplayer shared events now announced with "Shared event." prefix (some events are per user, others require a vote.)
* Fixed Defect orbs not being focusable in multiplayer (was sometimes checking the wrong player's orbs).
* Fixed various incorrect readouts in multiplayer (such as you losing gold when it was, in fact, another player losing gold.)
* Removed end-of-turn focus suppression that was blocking card popups and other UI. You may still hear the occasional erronious control read out at end of turn as a result, but it is better than the alternative for now.
* Fixed power events: decreasing a power to 0 no longer double-announces (only "lost PowerName" is announced, not both decreased and removed).
* Non-stacking powers (permanent duration, such as Shrink) no longer show misleading -1 stack count in announcements and buffers.
* Stolen cards (Swipe power) now show in the creature buffer as "Stolen card: CardName".
* Removed the redundant "Announce All Block Lost" setting; "lost all Block" is now always announced when block hits 0.
* switching to the events buffer now jumps to the most recent item.
* Fixed hand card selection screen (e.g. Well Laid Plans retain) to include the selected cards row. You can now arrow down to see selected cards and back up to the hand. Note that these directions are swapped in game too compared to Slay the Spire 1; this isn't a mod bug.
* Fixed grid position announcements to use coordinate order to (x, y) instead of (y, x). This should also fix inconsistencies with the Crystal Sphere divination event.
* Refactored the focus system to a centralized update loop, fixing issues where container context wasn't announced when backing out of settings subcategories or when controls moved between containers.
* Fixed Ctrl+Shift+R (Reset Bindings) to reset the mod's own keybindings instead of the game's.
* Fixed card and relic buffers not listing rarity on the first item.
* Fixed certain card selection screens not reading (for example the Dreamcatcher relic card reward)
* Grid card selection screens now announce card selected state and how many cards are selected.
* Fixed various focus issues on the mode select screen, as well as mode buttons lacking tooltips.

## V0.1.4
* The Python installer has been replaced with one coded in Rust. This should prevent Windows Defender erroniously flagging the installer as a virus and improve stability of the app overall.
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
* Accessibility is now off by default for Workshop installs. The installer automatically enables it. You can also toggle accessibility with Ctrl+Shift+A (requires a game restart to take effect).

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
