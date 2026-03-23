# Installation

There are two approaches to installing Say the Spire 2. You can either use the installer program (recommended) or manually install.

## Using the Installer (Recommended)

The recommended way to install Say the Spire 2 is using the provided installer program. This downloads the latest release, extracts all required files to the right directory, and modifies game settings to enable mods accessibly. You will not need to download an installer to update your mod to a newer version and can just simply use the same program (unless the installer itself is updated.) You can download it from the [latest release page](https://github.com/bradjrenshaw/say-the-spire2/releases/latest).

### First-Time Setup

1. Important: **Launch Slay the Spire 2 at least once** before running the installer. The game needs to generate its settings file.
2. Run the installer. It will auto-detect the game directory. If it doesn't, you can browse to find it with the browse button next to the input field.
3. Click **Install** and select the version you want.
4. The installer will prompt you with two options:
   - **Screen reader support**: Enable this if you are blind or visually impaired. Sighted players playing with blind players should select No.
   - **Disable Godot UIA**: Recommended Yes for screen reader users. Select No if you are sighted or use other accessibility tools that rely on UIA.
5. The installer will enable mods in your game settings automatically. If it can't find the settings file, it will prompt you to launch the game first and retry.

### JAWS Configuration

If you use JAWS, the installer can copy JAWS configuration files to your JAWS settings directory. This is highly recommended. The jaws configuration files allow keyboard input to pass through to the program that jaws would otherwise prevent, such as the arrow keys. They also disable some annoying repeating announcements due to some UIA glitches (frequent reporting of an invisible UI control that handles the game's crash reporting.)

Click **Install JAWS config** after installing the mod and the mod will prompt you to select your Jaws settings directory. This will be something like C:\Users\user\AppData\Roaming\Freedom Scientific\JAWS\year\Settings\locale.

### Updating

Click Install and select the newer version. The installer uses proper version comparison and will show "Up to date" if you already have the latest.

### Modifying Options

Click **Modify** to change the screen reader and UIA settings after installation.

### Uninstalling

Click **Uninstall** to remove the mod files. You'll be asked if you also want to remove saved settings and preferences.

## Manual Installation

Extract the release zip into your game directory. The `mods/` folder should contain `SayTheSpire2.json`, `SayTheSpire2.dll`, `SayTheSpire2.pck`, and supporting DLLs.

You'll need to manually enable mods in `%APPDATA%/SlayTheSpire2/steam/steamid/settings.save` by setting `mod_settings.mods_enabled` to `true`.

### Jaws

If you use jaws, configuration files are provided that significantly improve the experience. The jaws configuration files allow keyboard input to pass through to the program that jaws would otherwise prevent, such as the arrow keys. They also disable some annoying repeating announcements due to some UIA glitches (frequent reporting of an invisible UI control that handles the game's crash reporting.)

The jaws scripts are located in the jaws subfolder of the release zip. Open this folder, copy them all to clipboard, and then paste them in your Jaws settings directory. This will be something like C:/Users/user/AppData/Roaming/Freedom Scientific/JAWS/year/Settings/locale.