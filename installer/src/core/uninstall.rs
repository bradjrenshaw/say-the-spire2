use std::fs;
use std::path::Path;

use super::paths::{appdata_mod_dir, LEGACY_FILES, MOD_FILES, ROOT_FILES};

pub fn uninstall_mod(game_path: &Path) -> Vec<String> {
    let mut removed = Vec::new();

    for f in MOD_FILES
        .iter()
        .chain(ROOT_FILES.iter())
        .chain(LEGACY_FILES.iter())
    {
        let fp = game_path.join(f);
        if fp.exists() {
            if fs::remove_file(&fp).is_ok() {
                removed.push(f.to_string());
            }
        }
    }

    removed
}

pub fn remove_mod_settings() -> Result<(), String> {
    let dir = appdata_mod_dir();
    if dir.exists() {
        fs::remove_dir_all(&dir).map_err(|e| format!("Failed to remove settings: {}", e))?;
    }
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::fs;

    #[test]
    fn uninstall_removes_existing_files() {
        let dir = tempfile::tempdir().unwrap();
        let mods_dir = dir.path().join("mods");
        fs::create_dir(&mods_dir).unwrap();

        // Create some mod files
        fs::write(mods_dir.join("SayTheSpire2.dll"), "").unwrap();
        fs::write(mods_dir.join("SayTheSpire2.pck"), "").unwrap();
        fs::write(dir.path().join("Tolk.dll"), "").unwrap();

        let removed = uninstall_mod(dir.path());

        assert!(removed.contains(&"mods/SayTheSpire2.dll".to_string()));
        assert!(removed.contains(&"mods/SayTheSpire2.pck".to_string()));
        assert!(removed.contains(&"Tolk.dll".to_string()));
        assert_eq!(removed.len(), 3);

        // Verify files are gone
        assert!(!mods_dir.join("SayTheSpire2.dll").exists());
        assert!(!mods_dir.join("SayTheSpire2.pck").exists());
        assert!(!dir.path().join("Tolk.dll").exists());
    }

    #[test]
    fn uninstall_skips_missing_files() {
        let dir = tempfile::tempdir().unwrap();
        let removed = uninstall_mod(dir.path());
        assert!(removed.is_empty());
    }

    #[test]
    fn uninstall_partial_files() {
        let dir = tempfile::tempdir().unwrap();
        // Only create one file
        fs::write(dir.path().join("Tolk.dll"), "").unwrap();

        let removed = uninstall_mod(dir.path());
        assert_eq!(removed.len(), 1);
        assert!(removed.contains(&"Tolk.dll".to_string()));
    }
}
