# Multiplayer

Say the Spire 2 has full multiplayer support. The mod markse itself as not effecting multiplayer, so those you are playing with won't require it to be installed. This page provides a general overview of the multiplayer features Say the Spire 2 supports.

## Lobby

After selecting a game mode and character, you enter the multiplayer lobby. The **lobby buffer** shows all connected players with their character and ready status. You can cycle to it with the buffer navigation keys.

The mod announces:
- Players joining and leaving the lobby
- Players changing their character
- Ready/unready state changes
- When all players are ready

Character buttons show how many other players have selected the same character.

## Expanded Screens

You can click on a player in multiplayer to view their expanded screen, which shows you their hand, deck, potions, and relics. Either click on their creature in the creature row in combat, or click on their creature below the relics bar out of combat to access this.

## Voting

### Map Voting
When choosing the next map node, all players vote. The mod announces:
- Which player voted for which node (with coordinates)
- The final destination when travel begins

### Event Voting
Shared events require all players to vote on options. The mod announces:
- "Shared event" prefix on event descriptions
- Which player voted for which option
- The chosen option result

### Act Ready-Up
After rewards, players ready up to proceed to the next act. The mod announces when other players are ready and when all players are ready.

## Combat

### Remote Player Info
When focusing another player's creature in the creature row, the **player buffer** shows their full info: HP, block, energy, gold, card pile counts, and powers — the same info you see for yourself.

### Card Plays and Potions
The mod can announce when other players play cards or use potions. By default, your own plays are silent (you know what you played) and other players' plays are announced. These can be toggled in the event settings.

### End Turn
The mod announces when other players end their turn or cancel their end turn decision.

### Expanded Player View
Press **Accept** on a player creature in the creature row to open their expanded view, showing their full deck, relics, and potions with position announcements.

## Rest Site (Mend)
When using the Mend option to heal another player, focusing each player character announces their name and current HP.

## Settings

All multiplayer events can be toggled individually in the mod settings under Events. Each event with creature sources has a **Sources** subcategory where you can enable/disable announcements for:
- **Current Player** — your own actions
- **Other Players** — other players' actions
- **Enemies** — enemy actions

Events only announce sources that the game provides visual feedback for. For example, gold changes are only shown for the current player, so "Other Players" isn't available for gold events.
