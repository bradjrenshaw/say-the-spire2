use std::cell::RefCell;
use std::path::PathBuf;
use std::rc::Rc;


use wxdragon::prelude::*;

use crate::core::{detect, github, install, jaws, settings, uninstall};

struct State {
    release: Option<github::ReleaseInfo>,
}

pub fn run() {
    wxdragon::main(|_app| {
        let frame = Frame::builder()
            .with_title("SayTheSpire2 Installer")
            .with_size(Size::new(550, 450))
            .build();

        let panel = Panel::builder(&frame).build();
        let main_sizer = BoxSizer::builder(Orientation::Vertical).build();

        // Status text
        let status = StaticText::builder(&panel)
            .with_label("Detecting game directory...")
            .build();

        // Game path row
        let path_sizer = BoxSizer::builder(Orientation::Horizontal).build();
        let path_label = StaticText::builder(&panel)
            .with_label("Game directory:")
            .build();
        let path_input = TextCtrl::builder(&panel).build();
        let browse_btn = Button::builder(&panel)
            .with_label("Browse...")
            .build();

        path_sizer.add(&path_label, 0, SizerFlag::All, 4);
        path_sizer.add(&path_input, 1, SizerFlag::Expand | SizerFlag::All, 4);
        path_sizer.add(&browse_btn, 0, SizerFlag::All, 4);

        // Log area
        let log = TextCtrl::builder(&panel)
            .with_style(TextCtrlStyle::MultiLine | TextCtrlStyle::ReadOnly | TextCtrlStyle::WordWrap)
            .build();

        // Button row
        let btn_sizer = BoxSizer::builder(Orientation::Horizontal).build();
        let install_btn = Button::builder(&panel)
            .with_label("Install")
            .build();
        let install_file_btn = Button::builder(&panel)
            .with_label("Install from file...")
            .build();
        let uninstall_btn = Button::builder(&panel)
            .with_label("Uninstall")
            .build();
        let jaws_btn = Button::builder(&panel)
            .with_label("Install JAWS config")
            .build();

        btn_sizer.add_stretch_spacer(1);
        btn_sizer.add(&install_btn, 0, SizerFlag::All, 4);
        btn_sizer.add(&install_file_btn, 0, SizerFlag::All, 4);
        btn_sizer.add(&uninstall_btn, 0, SizerFlag::All, 4);
        btn_sizer.add(&jaws_btn, 0, SizerFlag::All, 4);

        // Layout
        main_sizer.add(&status, 0, SizerFlag::Expand | SizerFlag::All, 8);
        main_sizer.add_sizer(&path_sizer, 0, SizerFlag::Expand | SizerFlag::Left | SizerFlag::Right, 8);
        main_sizer.add(&log, 1, SizerFlag::Expand | SizerFlag::All, 8);
        main_sizer.add_sizer(&btn_sizer, 0, SizerFlag::Expand | SizerFlag::All, 4);

        panel.set_sizer(main_sizer, true);

        // Disable action buttons initially
        install_btn.enable(false);
        install_file_btn.enable(false);
        uninstall_btn.enable(false);
        jaws_btn.enable(false);

        // Shared state
        let state = Rc::new(RefCell::new(State { release: None }));

        // Auto-detect game path
        if let Some(detected) = detect::detect_game_path() {
            path_input.set_value(&detected.to_string_lossy());
            log_append(&log, &format!("Game directory: {}", detected.display()));
            update_state(
                &status, &install_btn, &install_file_btn, &uninstall_btn, &jaws_btn,
                &detected, &state, &log,
            );
        } else {
            status.set_label("Game directory not found. Please browse to select it.");
            log_append(&log, "Could not auto-detect game directory.");
        }

        // Fetch release info (before showing window, so no visible delay)
        match github::fetch_latest_release() {
            Ok(info) => {
                log_append(&log, &format!("Latest version: {}", info.tag_name));
                state.borrow_mut().release = Some(info);
                let path = PathBuf::from(path_input.get_value());
                if detect::validate_game_path(&path) {
                    update_state(
                        &status, &install_btn, &install_file_btn,
                        &uninstall_btn, &jaws_btn, &path, &state, &log,
                    );
                }
            }
            Err(e) => {
                log_append(&log, &format!("Failed to check for updates: {}", e));
                status.set_label("Could not connect to GitHub. Install/update unavailable.");
            }
        }

        // Browse button
        {
            let frame_c = frame.clone();
            let path_input_c = path_input.clone();
            let status_c = status.clone();
            let install_btn_c = install_btn.clone();
            let install_file_btn_c = install_file_btn.clone();
            let uninstall_btn_c = uninstall_btn.clone();
            let jaws_btn_c = jaws_btn.clone();
            let log_c = log.clone();
            let state_c = state.clone();

            browse_btn.on_click(move |_| {
                let dialog = DirDialog::builder(&frame_c, "Select Slay the Spire 2 game directory", "")
                    .build();
                if dialog.show_modal() == ID_OK {
                    if let Some(path_str) = dialog.get_path() {
                        let path = PathBuf::from(&path_str);
                        apply_game_path(
                            &path, &path_input_c, &status_c, &install_btn_c,
                            &install_file_btn_c, &uninstall_btn_c, &jaws_btn_c,
                            &log_c, &state_c,
                        );
                    }
                }
            });
        }

        // Install button
        {
            let frame_c = frame.clone();
            let path_input_c = path_input.clone();
            let status_c = status.clone();
            let install_btn_c = install_btn.clone();
            let install_file_btn_c = install_file_btn.clone();
            let uninstall_btn_c = uninstall_btn.clone();
            let jaws_btn_c = jaws_btn.clone();
            let browse_btn_c = browse_btn.clone();
            let log_c = log.clone();
            let state_c = state.clone();

            install_btn.on_click(move |_| {
                let game_path = PathBuf::from(path_input_c.get_value());
                let borrow = state_c.borrow();
                let Some(info) = borrow.release.as_ref() else { return };

                // Show release notes and confirm for updates
                let is_update = detect::is_mod_installed(&game_path);
                if is_update && !info.body.is_empty() {
                    let dialog = MessageDialog::builder(
                        &frame_c,
                        &format!("Release notes:\n\n{}\n\nProceed?", info.body),
                        &format!("Update to {}", info.tag_name),
                    )
                    .with_style(MessageDialogStyle::YesNo | MessageDialogStyle::IconQuestion)
                    .build();
                    if dialog.show_modal() != ID_YES {
                        return;
                    }
                }

                let Some(asset) = github::find_zip_asset(&info.assets) else {
                    log_append(&log_c, "Error: No .zip asset found in the release.");
                    return;
                };

                let url = asset.browser_download_url.clone();
                let version = info.tag_name.clone();
                drop(borrow);

                // Disable buttons during download
                install_btn_c.enable(false);
                install_file_btn_c.enable(false);
                uninstall_btn_c.enable(false);
                jaws_btn_c.enable(false);
                browse_btn_c.enable(false);
                log_append(&log_c, "Downloading...");
                status_c.set_label("Downloading...");

                let result = install::download_and_extract(&url, &game_path, |_pct| {});

                install_btn_c.enable(true);
                install_file_btn_c.enable(true);
                uninstall_btn_c.enable(true);
                jaws_btn_c.enable(true);
                browse_btn_c.enable(true);

                match result {
                    Ok(_) => {
                        if let Err(e) = install::save_installed_version(&version) {
                            log_append(&log_c, &format!("Warning: {}", e));
                        }
                        if let Err(e) = install::enable_accessibility() {
                            log_append(&log_c, &format!("Warning: {}", e));
                        }
                        match settings::enable_mods_in_settings() {
                            Ok(msg) => log_append(&log_c, &msg),
                            Err(e) => {
                                log_append(&log_c, &e);
                                log_append(&log_c, "You may need to enable mods manually.");
                            }
                        }
                        log_append(
                            &log_c,
                            &format!("Successfully installed version {}.", version),
                        );
                        update_state(
                            &status_c, &install_btn_c, &install_file_btn_c,
                            &uninstall_btn_c, &jaws_btn_c, &game_path, &state_c, &log_c,
                        );

                        MessageDialog::builder(
                            &frame_c,
                            &format!(
                                "SayTheSpire2 version {} installed successfully!",
                                version
                            ),
                            "Installation Complete",
                        )
                        .with_style(MessageDialogStyle::OK | MessageDialogStyle::IconInformation)
                        .build()
                        .show_modal();
                    }
                    Err(e) => {
                        log_append(&log_c, &format!("Error: {}", e));
                        MessageDialog::builder(&frame_c, &e, "Installation Failed")
                            .with_style(MessageDialogStyle::OK | MessageDialogStyle::IconError)
                            .build()
                            .show_modal();
                    }
                }
            });
        }

        // Install from file button
        {
            let frame_c = frame.clone();
            let path_input_c = path_input.clone();
            let status_c = status.clone();
            let install_btn_c = install_btn.clone();
            let install_file_btn_c = install_file_btn.clone();
            let uninstall_btn_c = uninstall_btn.clone();
            let jaws_btn_c = jaws_btn.clone();
            let log_c = log.clone();
            let state_c = state.clone();

            install_file_btn.on_click(move |_| {
                let game_path = PathBuf::from(path_input_c.get_value());

                let dialog = FileDialog::builder(&frame_c)
                    .with_message("Select mod zip file")
                    .with_wildcard("Zip files (*.zip)|*.zip")
                    .with_style(FileDialogStyle::Open | FileDialogStyle::FileMustExist)
                    .build();

                if dialog.show_modal() != ID_OK {
                    return;
                }
                let Some(zip_path_str) = dialog.get_path() else { return };
                let zip_path = PathBuf::from(&zip_path_str);

                match install::install_from_file(&zip_path, &game_path) {
                    Ok(_) => {
                        if let Err(e) = install::enable_accessibility() {
                            log_append(&log_c, &format!("Warning: {}", e));
                        }
                        match settings::enable_mods_in_settings() {
                            Ok(msg) => log_append(&log_c, &msg),
                            Err(e) => log_append(&log_c, &e),
                        }
                        log_append(
                            &log_c,
                            &format!(
                                "Installed from {}.",
                                zip_path.file_name().unwrap_or_default().to_string_lossy()
                            ),
                        );
                        update_state(
                            &status_c, &install_btn_c, &install_file_btn_c,
                            &uninstall_btn_c, &jaws_btn_c, &game_path, &state_c, &log_c,
                        );

                        MessageDialog::builder(
                            &frame_c,
                            "SayTheSpire2 installed successfully from file!",
                            "Installation Complete",
                        )
                        .with_style(MessageDialogStyle::OK | MessageDialogStyle::IconInformation)
                        .build()
                        .show_modal();
                    }
                    Err(e) => {
                        log_append(&log_c, &format!("Error: {}", e));
                        MessageDialog::builder(&frame_c, &e, "Error")
                            .with_style(MessageDialogStyle::OK | MessageDialogStyle::IconError)
                            .build()
                            .show_modal();
                    }
                }
            });
        }

        // Uninstall button
        {
            let frame_c = frame.clone();
            let path_input_c = path_input.clone();
            let status_c = status.clone();
            let install_btn_c = install_btn.clone();
            let install_file_btn_c = install_file_btn.clone();
            let uninstall_btn_c = uninstall_btn.clone();
            let jaws_btn_c = jaws_btn.clone();
            let log_c = log.clone();
            let state_c = state.clone();

            uninstall_btn.on_click(move |_| {
                let game_path = PathBuf::from(path_input_c.get_value());

                let dialog = MessageDialog::builder(
                    &frame_c,
                    "Remove SayTheSpire2 mod files?",
                    "Confirm Uninstall",
                )
                .with_style(MessageDialogStyle::YesNo | MessageDialogStyle::IconQuestion)
                .build();

                if dialog.show_modal() != ID_YES {
                    return;
                }

                let removed = uninstall::uninstall_mod(&game_path);
                if removed.is_empty() {
                    log_append(&log_c, "No mod files found to remove.");
                } else {
                    log_append(&log_c, &format!("Removed: {}", removed.join(", ")));
                }

                let remove_settings = MessageDialog::builder(
                    &frame_c,
                    "Also remove mod settings and saved preferences?",
                    "Remove Settings",
                )
                .with_style(MessageDialogStyle::YesNo | MessageDialogStyle::IconQuestion)
                .build()
                .show_modal();

                if remove_settings == ID_YES {
                    match uninstall::remove_mod_settings() {
                        Ok(_) => log_append(&log_c, "Removed mod settings."),
                        Err(e) => log_append(&log_c, &format!("Error: {}", e)),
                    }
                }

                log_append(&log_c, "Uninstall complete.");
                update_state(
                    &status_c, &install_btn_c, &install_file_btn_c,
                    &uninstall_btn_c, &jaws_btn_c, &game_path, &state_c, &log_c,
                );

                MessageDialog::builder(
                    &frame_c,
                    "SayTheSpire2 has been uninstalled.",
                    "Uninstall Complete",
                )
                .with_style(MessageDialogStyle::OK | MessageDialogStyle::IconInformation)
                .build()
                .show_modal();
            });
        }

        // JAWS install button
        {
            let frame_c = frame.clone();
            let path_input_c = path_input.clone();
            let log_c = log.clone();

            jaws_btn.on_click(move |_| {
                let game_path = PathBuf::from(path_input_c.get_value());

                if !jaws::has_jaws_files(&game_path) {
                    MessageDialog::builder(
                        &frame_c,
                        "JAWS config files not found in the game directory.\nPlease install the mod first.",
                        "Error",
                    )
                    .with_style(MessageDialogStyle::OK | MessageDialogStyle::IconError)
                    .build()
                    .show_modal();
                    return;
                }

                let dialog = DirDialog::builder(&frame_c, "Select JAWS settings directory", "")
                    .build();
                if dialog.show_modal() != ID_OK {
                    return;
                }
                let Some(dest_str) = dialog.get_path() else { return };
                let dest = PathBuf::from(&dest_str);

                let (copied, errors) = jaws::install_jaws_config(&game_path, &dest);
                if !copied.is_empty() {
                    log_append(
                        &log_c,
                        &format!("JAWS files copied to {}: {}", dest.display(), copied.join(", ")),
                    );
                }
                if !errors.is_empty() {
                    log_append(&log_c, &format!("Errors: {}", errors.join("; ")));
                    MessageDialog::builder(
                        &frame_c,
                        &format!("Some files failed to copy:\n{}", errors.join("\n")),
                        "Error",
                    )
                    .with_style(MessageDialogStyle::OK | MessageDialogStyle::IconError)
                    .build()
                    .show_modal();
                } else {
                    MessageDialog::builder(
                        &frame_c,
                        &format!("JAWS config files installed to {}.", dest.display()),
                        "JAWS Install Complete",
                    )
                    .with_style(MessageDialogStyle::OK | MessageDialogStyle::IconInformation)
                    .build()
                    .show_modal();
                }
            });
        }

        frame.show(true);
    })
    .expect("Failed to start application");
}

fn apply_game_path(
    path: &std::path::Path,
    path_input: &TextCtrl,
    status: &StaticText,
    install_btn: &Button,
    install_file_btn: &Button,
    uninstall_btn: &Button,
    jaws_btn: &Button,
    log: &TextCtrl,
    state: &Rc<RefCell<State>>,
) {
    if !detect::validate_game_path(path) {
        log_append(log, &format!("Invalid game directory: {}", path.display()));
        status.set_label("Invalid game directory. Please browse to select it.");
        return;
    }

    path_input.set_value(&path.to_string_lossy());
    log_append(log, &format!("Game directory: {}", path.display()));
    update_state(status, install_btn, install_file_btn, uninstall_btn, jaws_btn, path, state, log);
}

fn update_state(
    status: &StaticText,
    install_btn: &Button,
    install_file_btn: &Button,
    uninstall_btn: &Button,
    jaws_btn: &Button,
    game_path: &std::path::Path,
    state: &Rc<RefCell<State>>,
    log: &TextCtrl,
) {
    let mod_installed = detect::is_mod_installed(game_path);
    let installed_version = install::get_installed_version();
    let has_valid_path = detect::validate_game_path(game_path);

    install_file_btn.enable(has_valid_path);
    uninstall_btn.enable(mod_installed);
    jaws_btn.enable(jaws::has_jaws_files(game_path));

    if mod_installed {
        log_append(
            log,
            &format!(
                "Installed version: {}",
                installed_version.as_deref().unwrap_or("unknown")
            ),
        );
    }

    let borrow = state.borrow();
    if let Some(info) = borrow.release.as_ref() {
        let latest = &info.tag_name;
        if !mod_installed {
            install_btn.set_label("Install");
            install_btn.enable(true);
            status.set_label(&format!("Ready to install version {}.", latest));
        } else if installed_version.as_deref() == Some(latest) {
            install_btn.set_label("Install");
            install_btn.enable(false);
            status.set_label(&format!("SayTheSpire2 is up to date (version {}).", latest));
        } else {
            install_btn.set_label("Update");
            install_btn.enable(true);
            status.set_label(&format!(
                "Update available: {} → {}",
                installed_version.as_deref().unwrap_or("unknown"),
                latest
            ));
        }
    }
}

fn log_append(log: &TextCtrl, msg: &str) {
    let current = log.get_value();
    if current.is_empty() {
        log.set_value(msg);
    } else {
        log.set_value(&format!("{}\n{}", current, msg));
    }
}
