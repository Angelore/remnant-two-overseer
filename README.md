# <img src="RemnantOverseer/Assets/.github/icon-gh.png" width="24"> Remnant Overseer

A companion tool for **Remnant 2** that allows you to view the information about your current save.<br/>
Select your character and see which items they are missing and which ones you can get in your currently rolled worlds.

This tool only **reads** the save file, it can not make any changes to it.

# Screenshots
<p float="left">
  <img src="RemnantOverseer/Assets/.github/character-view.png" width="400" />
  <img src="RemnantOverseer/Assets/.github/world-view.png" width="400" />
</p>

# Features
- Displays the items that are available in the current rolled world
- Displays the current location of the player
- Indicates whether Trait book and/or Simulacrum are available in the location
- Displays whether an item is only obtainable in co-op
- Allows to filter by item names and injectable events
- Updates the state whenever the save file changes
- Gamepass support
- ... and more?

# Installation
- Download and install [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or greater
  - You only need a .NET Desktop Runtime x64, but you can install the whole SDK if you want to
- Download the [latest release](https://github.com/Angelore/remnant-two-overseer/releases)
- Unpack the archive to a folder. To avoid issues, use a non-system folder, such as `C:\Games\RemnantOverseer`
- Launch the .exe file

# Finding your save
- If playing on the Xbox/Gamepass release of the game, you'll need to go into settings and update your save file location.
- This is typically found in:
```
C:\Users\[YourUser]\AppData\Local\Packages\PerfectWorldEntertainment.GFREMP2_jrajkyc4tsa6w\SystemAppData\wgs
```
- In that folder, there should be a folder named `t` and then a folder with a long ID for the name, open that folder
- Inside, there should be a `containers.index` file, that's the save you're looking for

If that doesn't work, you can try searching your drive for `containers.index` file. (The path should include `PerfectWorldEntertainment.GFREMP2`)

# Compilation Options

To aid in package management efforts the following options may be set at compile time:

| Option                                     | Effect                                                                                 |
|--------------------------------------------|----------------------------------------------------------------------------------------|
| `REMNANTOVERSEER_NO_DEFAULT_VERSION_CHECK` | The version check will be disabled by **default**, can still be reenabled by the user. |
| `REMNANTOVERSEER_USER_DIRECTORIES`         | Use per-user directories (e.g. XDG on Linux, AppData on Windows).                      |

Options can be provided as such:

```bash
dotnet build -p:DefineConstants=REMNANTOVERSEER_NO_DEFAULT_VERSION_CHECK
```

# Acknowledgements
- Razzmatazzz for his [project](https://github.com/Razzmatazzz/RemnantSaveGuardian), which served as an inspiration for this one and made me look into working on Remnant in the first place
- AndrewSav for [https://github.com/AndrewSav/lib.remnant2.analyzer](https://github.com/AndrewSav/lib.remnant2.analyzer), which is used as an underlying parsing library
- Avalonia (EN) telegram channel and specifically Steve for answering many of my AvaloniaUI-related questions
- And you! For taking an interest in the project

# Known issues
- When viewing a big list of items application is stuttering on refresh. This is due to my control of choice (treeview) not having virtualization, so building a visual tree takes a while. I learned about this quite late in development, so I decided to stick with it for now. I will try to optimize it when I have extra time (this will require a non-trivial rework)
