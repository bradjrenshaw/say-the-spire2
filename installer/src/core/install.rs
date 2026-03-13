use std::fs;
use std::io::{Cursor, Read};
use std::path::Path;

use super::paths::{accessibility_file, appdata_mod_dir, version_file};

pub fn get_installed_version() -> Option<String> {
    let vf = version_file();
    fs::read_to_string(vf).ok().map(|s| s.trim().to_string())
}

pub fn save_installed_version(version: &str) -> Result<(), String> {
    let dir = appdata_mod_dir();
    fs::create_dir_all(&dir).map_err(|e| format!("Failed to create directory: {}", e))?;
    fs::write(version_file(), version).map_err(|e| format!("Failed to write version: {}", e))
}

pub fn enable_accessibility() -> Result<(), String> {
    let dir = appdata_mod_dir();
    fs::create_dir_all(&dir).map_err(|e| format!("Failed to create directory: {}", e))?;
    fs::write(accessibility_file(), "{\"enabled\": true}")
        .map_err(|e| format!("Failed to write accessibility.json: {}", e))
}

pub fn download_and_extract(
    url: &str,
    game_path: &Path,
    progress: impl Fn(u32),
) -> Result<(), String> {
    let client = reqwest::blocking::Client::builder()
        .user_agent("SayTheSpire2Installer")
        .timeout(std::time::Duration::from_secs(120))
        .build()
        .map_err(|e| format!("Failed to create HTTP client: {}", e))?;

    let resp = client
        .get(url)
        .send()
        .map_err(|e| format!("Download failed: {}", e))?;

    if !resp.status().is_success() {
        return Err(format!("Download returned status {}", resp.status()));
    }

    let total = resp.content_length().unwrap_or(0);
    let mut reader = resp;
    let mut buffer = Vec::new();
    let mut downloaded: u64 = 0;
    let mut buf = [0u8; 8192];

    loop {
        let n = reader
            .read(&mut buf)
            .map_err(|e| format!("Read error: {}", e))?;
        if n == 0 {
            break;
        }
        buffer.extend_from_slice(&buf[..n]);
        downloaded += n as u64;
        if total > 0 {
            progress((downloaded * 100 / total) as u32);
        }
    }

    extract_zip(&buffer, game_path)
}

pub fn install_from_file(zip_path: &Path, game_path: &Path) -> Result<(), String> {
    let data = fs::read(zip_path).map_err(|e| format!("Failed to read zip: {}", e))?;
    extract_zip(&data, game_path)
}

pub fn extract_zip(data: &[u8], dest: &Path) -> Result<(), String> {
    let cursor = Cursor::new(data);
    let mut archive =
        zip::ZipArchive::new(cursor).map_err(|e| format!("Failed to open zip: {}", e))?;

    for i in 0..archive.len() {
        let mut file = archive
            .by_index(i)
            .map_err(|e| format!("Failed to read zip entry: {}", e))?;

        let name = file.name().to_string();

        // Skip directory entries
        if name.ends_with('/') {
            let dir_path = dest.join(&name);
            fs::create_dir_all(&dir_path)
                .map_err(|e| format!("Failed to create dir {}: {}", name, e))?;
            continue;
        }

        let out_path = dest.join(&name);
        if let Some(parent) = out_path.parent() {
            fs::create_dir_all(parent)
                .map_err(|e| format!("Failed to create parent dir: {}", e))?;
        }

        let mut out_file = fs::File::create(&out_path)
            .map_err(|e| format!("Failed to create file {}: {}", name, e))?;
        std::io::copy(&mut file, &mut out_file)
            .map_err(|e| format!("Failed to write file {}: {}", name, e))?;
    }

    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Write;

    #[test]
    fn version_roundtrip() {
        let dir = tempfile::tempdir().unwrap();
        let vf = dir.path().join("version");

        // Write
        fs::write(&vf, "v0.1.3").unwrap();
        let content = fs::read_to_string(&vf).unwrap().trim().to_string();
        assert_eq!(content, "v0.1.3");
    }

    #[test]
    fn extract_zip_creates_files() {
        let dir = tempfile::tempdir().unwrap();

        // Create a zip in memory
        let mut zip_buf = Vec::new();
        {
            let cursor = Cursor::new(&mut zip_buf);
            let mut writer = zip::ZipWriter::new(cursor);

            let options = zip::write::SimpleFileOptions::default();
            writer.start_file("mods/test.dll", options).unwrap();
            writer.write_all(b"test content").unwrap();

            writer.start_file("root.txt", options).unwrap();
            writer.write_all(b"root file").unwrap();

            writer.finish().unwrap();
        }

        extract_zip(&zip_buf, dir.path()).unwrap();

        assert!(dir.path().join("mods").join("test.dll").exists());
        assert!(dir.path().join("root.txt").exists());

        let content = fs::read_to_string(dir.path().join("root.txt")).unwrap();
        assert_eq!(content, "root file");
    }

    #[test]
    fn extract_zip_creates_nested_dirs() {
        let dir = tempfile::tempdir().unwrap();

        let mut zip_buf = Vec::new();
        {
            let cursor = Cursor::new(&mut zip_buf);
            let mut writer = zip::ZipWriter::new(cursor);
            let options = zip::write::SimpleFileOptions::default();
            writer
                .start_file("a/b/c/deep.txt", options)
                .unwrap();
            writer.write_all(b"deep").unwrap();
            writer.finish().unwrap();
        }

        extract_zip(&zip_buf, dir.path()).unwrap();
        assert!(dir.path().join("a").join("b").join("c").join("deep.txt").exists());
    }

    #[test]
    fn extract_zip_empty_archive() {
        let dir = tempfile::tempdir().unwrap();

        let mut zip_buf = Vec::new();
        {
            let cursor = Cursor::new(&mut zip_buf);
            let writer = zip::ZipWriter::new(cursor);
            writer.finish().unwrap();
        }

        extract_zip(&zip_buf, dir.path()).unwrap();
    }
}
