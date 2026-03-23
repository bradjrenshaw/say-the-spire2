# Settings

Press **Ctrl+M** (or LT+Start/L2+Options) to open the mod settings screen. Navigate with Up/Down arrows, change values with Left/Right or Enter.

## UI Settings

Per-UI-element-type settings for controlling what gets announced:
- **Announce Type**: Whether to announce the element type (button, card, etc.)
- **Announce Subtype**: Whether to announce subtypes (attack, skill, etc.)
- **Announce Tooltip**: Whether to announce tooltip text
- **Announce Position**: Whether to announce position in lists/grids

## Event Settings

Each event type can be individually configured:
- **Announce**: Whether to speak the event when it happens
- **Add to buffer**: Whether to add the event to the events buffer

### Source Filtering

Events that apply to a creature have a **Sources** subcategory with toggles for:
- **Current Player**: Your own actions
- **Other Players**: Other players' actions (multiplayer)
- **Enemies**: Enemy actions

Not all sources are available for every event — the mod only shows sources that the game provides visual feedback for.

## Speech Settings

- **Speech Handler**: Choose between Auto, Tolk (NVDA/JAWS), SAPI, or Clipboard
- Per-handler settings (rate, volume, etc.)

## Map Settings

- **Automatically Follow Paths until Choice Node**: Auto-advance on forward navigation
- **Automatically Follow Paths Backward until Choice Node**: Auto-advance on backward navigation
- **Read Intermediate Nodes on Backward Paths**: Announce nodes while auto-advancing backward
- **Announce Current Location When Map Opens**: Read your current position when opening the map

## Keybindings

All keybindings are listed and can be customized from the settings screen. Select a binding to change it.

## Advanced

- **Verbose Logging**: Enable detailed event logging for debugging
- **Performance Profiling**: Enable timing measurements for mod subsystems
