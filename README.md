#### A windows-based multi-threaded Minesweeper bot using screen capture and mouse hook technology.

This project solves using only what's available visually in the Windows 7 Minesweeper. While still encountering the occaisonal bug, the bot is relatively steady and capable of solving at a breakneck pace. I predict with enough cycles roughly 25 second solve times are possible.

*The bot needs some generalization to be easy to use for most.* These should be easy to adjust. Current hardware requirements are:
- Single screen of 1920x1080 resolution
- 8 virtual cores

Tasks remaining for anyone looking to contribute and merge:
- Identify cause of flagged tiles unflagging themselves despite the stack merger check
-- alternatively, root cause
- Identify reason why internally understood non-tiles (!= 10) are sent to be checked despite catch.
- Reduce weight of image comparison.
