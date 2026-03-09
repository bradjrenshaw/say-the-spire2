## V0.1.2
* Added better logging for events to hopefully allow identification of bugs causing repeated announcements for some events.
* Fixed incorrect localization lookups for map nodes and merchant slots.
* Map nodes now only announce traveled state (IE you have been there before); the reachable and unreachable state announcements were irrelevant and causing confusion.
* Fixed an issue where the controller focus could get stuck on the character select screen if the user moved the cursor to panels that aren't yet available (such as the ascension panel.)