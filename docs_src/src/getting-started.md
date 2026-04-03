# Getting Started

After installing the mod, launch Slay the Spire 2. You should hear the mod announce Say the Spire and the mod version.

## How It Works

The mod acts as a screenreader for Slay the Spire 2, narrating focused UI elements and announcing gameplay events. Navigating the UI should feel fairly intuitive for the most part. Use the arrow keys, d-pad, or left stick to navigate menus. Use enter/a/x to confirm, backspace/b/circle to cancel. To read more information on an item, use control up/down (keyboard) or right stick up/down (controller) to review the current buffer of information (we will get to buffers in a moment.) The game does use quite a few bindings, so I highly recommend you consult the bindings documentation or check your mod settings menu (ctrl m keyboard, r2+start xbox, rt+start ps5.)

A note when playing your first game: the tutorial dialogs are a little unintuitive for the moment. For the multi-page tutorials, you must use right and left to navigate the pages; navigate right of the last page to exit and return to regular gameplay.

## Help

If you want context-sensitive help on any screen, press f1 (keyboard) or left trigger plus back (controller). This will provide help text and available controls for the screen you're currently on.

## Buffers

To access additional statistics and tooltips you can use the buffer system. There is an extra set of controller and keyboard inputs to provide information. This is generally refered to as the buffer system and is mapped to the right stick or control plus the arrow keys on keyboard by default. As you move over various UI Elements, contextual buffers will appear. These are just lists of various pieces of information about the element you're focusing on. For example, if you hover over a card, the card buffer will be automatically focused and will contain information such as the card's name, energy cost, description, etc. Use ctrl up/down (keyboard) or right stick up/down (controller) to review the contents of the currently selected buffer.

Even though you are focused on the card element for example, other buffers of information will also be present. You can use ctrl left/right (keyboard) or right stick left/right (controller) to navigate between these. This lets you inspect various information you could see visually without having to move to the corresponding element. These include the player buffer (for hp, energy, gold, etc), the events buffer (for reviewing things that have happened), the lobby screen in multiplayer, etc.

## The Map

The buffer controls are also used for the map. Using ctrl arrows (keyboard) or right stick (controller) you can browse all map nodes. The map is often the least intuitive part of the game for new players as it doesn't directly correspond to a simple grid of adjacent rooms. If you find the following section overwhelming don't worry too much about it; just play the game and it will make sense over time.

Visually the map is a grid of rooms of various types (shop, combat, questionmark/event/unknown, etc.) You can see the entire map for your current act. The map consists of a number of icons with lines connecting them to other rooms, showing you the paths you can take. You can only travel to a room connected to yours by a line and you can only travel forward/higher up the map, never in reverse.

To browse the map accessibly, the mod provides a map view cursor. Note that this is separate from your regular movement cursor; moving the map view cursor to a map node and pressing confirm will not click on the element focused by the map view cursor. think of it like a virtual review cursor your screenreader uses.

The map viewer always starts at the map room you are currently in when it opens; it then puts your view cursor on the connected room your regular cursor last focused. Use ctrl left/right (keyboard) or right stick left/right (controller) to navigate the list of rooms that you can reach from your current room. If you press ctrl up (keyboard) or right stick up (controller), you will move into that room and are now looking at the rooms it is connected to. This way you can browse the map in its entirety. Using ctrl down (keyboard) or right stick down (controller) will allow you to move in reverse.