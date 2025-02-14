# Valheim Ext Component Manager

## Abstract

Modding Valheim for VR requires downloading two ZIP files and copying a subset of the files/directories from within each ZIP to the Steam Valheim install directory. Note that some files are overwritten during installation.

Once installed, it always tries to run VR. If you have a headset on and want to play flat, you need to uninstall. Additionally, the mod will need updates when Valheim updates cause incompatibilities.

I've also created a voice-commander type program that runs alongside Valheim (started separately), communicating only via Windows events to the Valheim game window. This requires downloading and unpacking somewhere on the system (not specific). Running this requires remembering to start the program separately from Valheim. Updates require downloading and unzipping over the existing installation, beyond just knowing when a new version is available.

All of this requires some level of comfort with and possibly more technical understanding of Windows/computers, as well as time and inclination.

### Goal

The goal of this app is to provide a front-end interface to all of this, where the user can ideally run a script to:

1. Keep components up to date (excluding Valheim itself)
2. Run any external components
3. Run Valheim itself

The user should simply click a batch file to handle all these tasks.

> **Note:** There will be at least two primary scripts: one for running Valheim with VR mods and one without. There should be a single main program for the manager.

## External Usage

The manager should have options that affect whether it performs various tasks:

```
--ext-component-manager::check-update::{yes|no} # Specifies whether the component-manager should be updated if new versions are available
--valheim-vr::check-update::{yes|no}            # Specifies whether the VR extensions should be updated if new versions are available (or not yet installed)
--valheim::start::{with-vr|without-vr|no}       # Specifies if Valheim should be started and with or without VR mods
--voice-commander::check-update::{yes|no}       # Specifies whether the voice-commander extensions should be updated if new versions are available (or not yet installed)
--voice-commander::manage::{yes|no|start|restart|stop} 
    # Specifies whether the manager will:
    # - Run the voice-commander while Valheim is running
    # - Not manage it at all
    # - Start if not running (non-managed)
    # - Restart whether running or not (non-managed)
    # - Stop it (non-managed)
```

## Internal Scripts

```
StartValheimWithVrAndVoiceCommander --ext-component-manager::check-update yes --valheim-vr::check-update yes --valheim-vr::enabled yes --voice-commander::check-update yes --voice-commander::manage run-while-valheim-runs

StartValheimWithVr --ext-component-manager::check-update yes --valheim-vr::check-update yes --valheim-vr::enabled yes --voice-commander::check-update no --voice-commander::manage no

StartValheimWithoutVr --ext-component-manager::check-update yes --valheim-vr::check-update no --valheim-vr::enabled no --voice-commander::check-update no --voice-commander::manage no

StartValheimWithoutVrWithVoiceCommander --ext-component-manager::check-update yes --valheim-vr::check-update no --valheim-vr::enabled no --voice-commander::check-update no --voice-commander::manage no

RestartVoiceCommander --ext-component-manager::check-update yes --voice-commander::check-update yes --voice-commander::manage restart

RestartVoiceCommanderWithoutUpdate --ext-component-manager::check-update yes --voice-commander::check-update no --voice-commander::manage restart

StopVoiceCommander --ext-component-manager::check-update yes --voice-commander::manage stop

CheckUpdateAll --ext-component-manager::check-update yes --valheim-vr::check-update yes --voice-commander::check-update yes

ResinstallAll ??
```

## Linked Scripts

- StartValheimWithVr
- StartValheimWithVrAndVoiceCommander
- StartValheimWithoutVr
