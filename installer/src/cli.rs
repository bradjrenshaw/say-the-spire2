use std::io::{self, Write};
use std::path::PathBuf;

use crate::core::{detect, github, install, jaws, settings, uninstall};

pub fn run() {
    println!("=== SayTheSpire2 Installer ===");
    println!();

    let game_path = match get_game_path() {
        Some(p) => p,
        None => {
            println!("Error: Invalid game directory.");
            return;
        }
    };

    println!();
    show_status(&game_path);
    println!();

    loop {
        println!("Options:");
        println!("  1. Install / Update from GitHub");
        println!("  2. Install from local zip file");
        println!("  3. Uninstall");
        println!("  4. Install JAWS config files");
        println!("  5. Exit");
        println!();

        let choice = prompt("Choose an option (1-5): ");

        println!();
        match choice.as_str() {
            "1" => install_from_github(&game_path),
            "2" => install_from_file(&game_path),
            "3" => do_uninstall(&game_path),
            "4" => install_jaws(&game_path),
            "5" => return,
            _ => println!("Invalid option."),
        }
        println!();
    }
}

fn get_game_path() -> Option<PathBuf> {
    if let Some(detected) = detect::detect_game_path() {
        println!("Detected game directory: {}", detected.display());
        let response = prompt("Use this path? (Y/n): ");
        if response != "n" && response != "no" {
            return Some(detected);
        }
    }

    let input = prompt("Press B to browse for the game directory, or type the path: ");
    if input.eq_ignore_ascii_case("b") {
        let path = rfd::FileDialog::new()
            .set_title("Select Slay the Spire 2 game directory")
            .pick_folder();
        match path {
            Some(p) if detect::validate_game_path(&p) => Some(p),
            Some(_) => {
                println!("Error: Selected directory does not appear to be a valid game install.");
                None
            }
            None => {
                println!("Browse cancelled.");
                None
            }
        }
    } else {
        let path = PathBuf::from(&input);
        if detect::validate_game_path(&path) {
            Some(path)
        } else {
            None
        }
    }
}

fn show_status(game_path: &PathBuf) {
    if detect::is_mod_installed(game_path) {
        let version = install::get_installed_version().unwrap_or_else(|| "unknown".to_string());
        println!("Mod is installed (version: {}).", version);
    } else {
        println!("Mod is not currently installed.");
    }
}

fn install_from_github(game_path: &PathBuf) {
    println!("Checking for latest release...");

    let release = match github::fetch_latest_release() {
        Ok(r) => r,
        Err(e) => {
            println!("Error: {}", e);
            return;
        }
    };

    println!("Latest version: {}", release.tag_name);

    let installed = install::get_installed_version();
    if installed.as_deref() == Some(&release.tag_name) && detect::is_mod_installed(game_path) {
        let response = prompt("You already have the latest version. Reinstall anyway? (y/N): ");
        if response != "y" && response != "yes" {
            return;
        }
    }

    if !release.body.is_empty() {
        println!();
        println!("Release notes:");
        println!("{}", release.body);
        println!();
    }

    let confirm = prompt("Proceed with installation? (Y/n): ");
    if confirm == "n" || confirm == "no" {
        return;
    }

    let asset = match github::find_zip_asset(&release.assets) {
        Some(a) => a,
        None => {
            println!("Error: No .zip asset found in the release.");
            return;
        }
    };

    println!("Downloading {}...", asset.name);

    match install::download_and_extract(&asset.browser_download_url, game_path, |pct| {
        print!("\rProgress: {}%   ", pct);
        io::stdout().flush().ok();
    }) {
        Ok(_) => {
            println!();
            if let Err(e) = install::save_installed_version(&release.tag_name) {
                println!("Warning: Failed to save version: {}", e);
            }
            if let Err(e) = install::enable_accessibility() {
                println!("Warning: Failed to enable accessibility: {}", e);
            }
            match settings::enable_mods_in_settings() {
                Ok(msg) => println!("{}", msg),
                Err(e) => {
                    println!("{}", e);
                    println!("You may need to enable mods manually in settings.save.");
                }
            }
            println!("Successfully installed version {}.", release.tag_name);
        }
        Err(e) => {
            println!();
            println!("Error: {}", e);
        }
    }
}

fn install_from_file(game_path: &PathBuf) {
    let input = prompt("Press B to browse for the zip file, or type the path: ");
    let zip_path = if input.eq_ignore_ascii_case("b") {
        match rfd::FileDialog::new()
            .set_title("Select mod zip file")
            .add_filter("Zip files", &["zip"])
            .pick_file()
        {
            Some(p) => p,
            None => {
                println!("Browse cancelled.");
                return;
            }
        }
    } else {
        PathBuf::from(&input)
    };

    if !zip_path.exists() {
        println!("Error: File not found.");
        return;
    }

    match install::install_from_file(&zip_path, game_path) {
        Ok(_) => {
            if let Err(e) = install::enable_accessibility() {
                println!("Warning: Failed to enable accessibility: {}", e);
            }
            match settings::enable_mods_in_settings() {
                Ok(msg) => println!("{}", msg),
                Err(e) => println!("{}", e),
            }
            println!(
                "Installed from {}.",
                zip_path.file_name().unwrap_or_default().to_string_lossy()
            );
        }
        Err(e) => println!("Error: {}", e),
    }
}

fn do_uninstall(game_path: &PathBuf) {
    let confirm = prompt("Remove SayTheSpire2 mod files? (y/N): ");
    if confirm != "y" && confirm != "yes" {
        return;
    }

    let removed = uninstall::uninstall_mod(game_path);
    if removed.is_empty() {
        println!("No mod files found to remove.");
    } else {
        println!("Removed: {}", removed.join(", "));
    }

    let remove_settings = prompt("Also remove mod settings and saved preferences? (y/N): ");
    if remove_settings == "y" || remove_settings == "yes" {
        match uninstall::remove_mod_settings() {
            Ok(_) => println!("Removed mod settings."),
            Err(e) => println!("Error: {}", e),
        }
    }

    println!("Uninstall complete.");
}

fn install_jaws(game_path: &PathBuf) {
    if !jaws::has_jaws_files(game_path) {
        println!("JAWS config files not found in the game directory.");
        println!("Please install the mod first.");
        return;
    }

    let files = jaws::get_jaws_files(game_path);
    println!("Found JAWS config files:");
    for f in &files {
        println!("  {}", f);
    }

    let input = prompt("Press B to browse for your JAWS settings directory, or type the path: ");
    let dest = if input.eq_ignore_ascii_case("b") {
        match rfd::FileDialog::new()
            .set_title("Select JAWS settings directory")
            .pick_folder()
        {
            Some(p) => p,
            None => {
                println!("Browse cancelled.");
                return;
            }
        }
    } else {
        PathBuf::from(&input)
    };

    if !dest.exists() {
        println!("Error: Directory not found.");
        return;
    }

    let (copied, errors) = jaws::install_jaws_config(game_path, &dest);
    if !copied.is_empty() {
        println!("Copied: {}", copied.join(", "));
    }
    if !errors.is_empty() {
        println!("Errors: {}", errors.join("; "));
    } else {
        println!("JAWS config files installed successfully.");
    }
}

fn prompt(msg: &str) -> String {
    print!("{}", msg);
    io::stdout().flush().ok();
    let mut input = String::new();
    io::stdin().read_line(&mut input).ok();
    input.trim().to_lowercase()
}
