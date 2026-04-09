"""Generate an HTML table of GitHub release download stats."""

import json
import urllib.request
import sys

REPO = "bradjrenshaw/say-the-spire2"
API_URL = f"https://api.github.com/repos/{REPO}/releases"


def fetch_releases():
    req = urllib.request.Request(API_URL, headers={"Accept": "application/vnd.github+json"})
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read())


def build_html(releases):
    rows = []
    grand_total = 0

    for release in releases:
        tag = release["tag_name"]
        name = release.get("name") or tag
        published = release["published_at"][:10]
        assets = release.get("assets", [])

        asset_parts = []
        release_total = 0
        for asset in assets:
            dl = asset["download_count"]
            release_total += dl
            asset_parts.append(f'{asset["name"]}: {dl:,}')

        grand_total += release_total
        assets_str = "<br>".join(asset_parts) if asset_parts else "No assets"

        rows.append(
            f"<tr>"
            f"<td>{name}</td>"
            f"<td>{tag}</td>"
            f"<td>{published}</td>"
            f"<td>{assets_str}</td>"
            f"<td><strong>{release_total:,}</strong></td>"
            f"</tr>"
        )

    rows_html = "\n".join(rows)

    return f"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>SayTheSpire2 Download Stats</title>
<style>
  body {{ font-family: system-ui, sans-serif; margin: 2rem; }}
  table {{ border-collapse: collapse; width: 100%; }}
  th, td {{ border: 1px solid #ccc; padding: 8px 12px; text-align: left; }}
  th {{ background: #f0f0f0; }}
  tr:nth-child(even) {{ background: #fafafa; }}
  .total {{ font-size: 1.2em; margin-top: 1rem; }}
</style>
</head>
<body>
<h1>SayTheSpire2 Download Stats</h1>
<p class="total">Total downloads across all releases: <strong>{grand_total:,}</strong></p>
<table>
<thead>
<tr><th>Release</th><th>Tag</th><th>Published</th><th>Assets</th><th>Total</th></tr>
</thead>
<tbody>
{rows_html}
</tbody>
</table>
</body>
</html>"""


def main():
    output = sys.argv[1] if len(sys.argv) > 1 else "download_stats.html"
    releases = fetch_releases()
    html = build_html(releases)
    with open(output, "w", encoding="utf-8") as f:
        f.write(html)
    print(f"Written to {output}")

    total = sum(
        asset["download_count"]
        for r in releases
        for asset in r.get("assets", [])
    )
    print(f"Total downloads: {total:,}")


if __name__ == "__main__":
    main()
