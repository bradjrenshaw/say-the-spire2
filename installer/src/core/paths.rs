use std::path::PathBuf;

pub const GITHUB_REPO: &str = "bradjrenshaw/say-the-spire2";
pub const GITHUB_API_URL: &str =
    "https://api.github.com/repos/bradjrenshaw/say-the-spire2/releases/latest";
pub const GAME_DIR_NAME: &str = "Slay the Spire 2";

pub const MOD_FILES: &[&str] = &[
    "mods/SayTheSpire2.json",
    "mods/SayTheSpire2.dll",
    "mods/SayTheSpire2.pck",
    "mods/System.Speech.dll",
    "mods/TolkDotNet.dll",
];

pub const ROOT_FILES: &[&str] = &[
    "Tolk.dll",
    "nvdaControllerClient64.dll",
    "SAAPI64.dll",
];

pub fn user_data_dir() -> PathBuf {
    if cfg!(target_os = "windows") {
        dirs::config_dir()
            .unwrap_or_else(|| PathBuf::from("C:\\Users\\Default\\AppData\\Roaming"))
            .join("SlayTheSpire2")
    } else if cfg!(target_os = "macos") {
        dirs::home_dir()
            .unwrap_or_else(|| PathBuf::from("/"))
            .join("Library")
            .join("Application Support")
            .join("SlayTheSpire2")
    } else {
        dirs::home_dir()
            .unwrap_or_else(|| PathBuf::from("/"))
            .join(".local")
            .join("share")
            .join("SlayTheSpire2")
    }
}

pub fn appdata_mod_dir() -> PathBuf {
    user_data_dir().join("mods").join("SayTheSpire2")
}

pub fn version_file() -> PathBuf {
    appdata_mod_dir().join("version")
}

pub fn installation_file() -> PathBuf {
    appdata_mod_dir().join("installation.json")
}

pub fn legacy_accessibility_file() -> PathBuf {
    appdata_mod_dir().join("accessibility.json")
}

pub fn steam_defaults() -> Vec<PathBuf> {
    if cfg!(target_os = "windows") {
        vec![PathBuf::from("C:\\Program Files (x86)\\Steam")]
    } else if cfg!(target_os = "macos") {
        let home = dirs::home_dir().unwrap_or_else(|| PathBuf::from("/"));
        vec![home.join("Library").join("Application Support").join("Steam")]
    } else {
        let home = dirs::home_dir().unwrap_or_else(|| PathBuf::from("/"));
        vec![
            home.join(".steam").join("steam"),
            home.join(".local").join("share").join("Steam"),
        ]
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn mod_files_contains_expected_entries() {
        assert!(MOD_FILES.contains(&"mods/SayTheSpire2.json"));
        assert!(MOD_FILES.contains(&"mods/SayTheSpire2.dll"));
        assert!(MOD_FILES.contains(&"mods/SayTheSpire2.pck"));
        assert!(MOD_FILES.contains(&"mods/System.Speech.dll"));
        assert!(MOD_FILES.contains(&"mods/TolkDotNet.dll"));
        assert_eq!(MOD_FILES.len(), 5);
    }

    #[test]
    fn root_files_contains_expected_entries() {
        assert!(ROOT_FILES.contains(&"Tolk.dll"));
        assert!(ROOT_FILES.contains(&"nvdaControllerClient64.dll"));
        assert!(ROOT_FILES.contains(&"SAAPI64.dll"));
        assert_eq!(ROOT_FILES.len(), 3);
    }

    #[test]
    fn user_data_dir_is_not_empty() {
        let dir = user_data_dir();
        assert!(dir.to_string_lossy().contains("SlayTheSpire2"));
    }

    #[test]
    fn appdata_mod_dir_is_under_user_data() {
        let mod_dir = appdata_mod_dir();
        let data_dir = user_data_dir();
        assert!(mod_dir.starts_with(&data_dir));
        assert!(mod_dir.to_string_lossy().contains("SayTheSpire2"));
    }

    #[test]
    fn version_file_is_under_mod_dir() {
        let vf = version_file();
        let mod_dir = appdata_mod_dir();
        assert!(vf.starts_with(&mod_dir));
        assert!(vf.to_string_lossy().ends_with("version"));
    }

    #[test]
    fn steam_defaults_not_empty() {
        let defaults = steam_defaults();
        assert!(!defaults.is_empty());
    }
}
