<div align="center">
  <h1>
    <img src="https://mangafire.to/assets/mangafire/logo-sm.png" alt="MangaFire Logo" width="46" valign="middle"/> Yukari.Plugin.MangaFire
  </h1>
</div>

<div align="center">
  
  ![GitHub Repo stars](https://img.shields.io/github/stars/Yukari-App/Plugin.MangaFire?style=for-the-badge&color=3A83BA)
  ![GitHub last commit](https://img.shields.io/github/last-commit/Yukari-App/Plugin.MangaFire?style=for-the-badge&color=E6E6E6)
  ![GitHub repo size](https://img.shields.io/github/repo-size/Yukari-App/Plugin.MangaFire?style=for-the-badge&color=3A83BA)
</div>

<div align="center">
    <h2>📖 Overview</h2>

**Yukari.Plugin.MangaFire** is a community plugin implementing the `IComicSource` interface from **[Yukari.Core](https://github.com/Yukari-App/Core)**.

It connects to the **[MangaFire](https://mangafire.to/)** official API with filter support (content rating, type, status, demographic, genres...).

Features multiple languages (English, Français, Español, Português-BR, 日本語, etc.).

Built for the **[Yukari](https://github.com/Yukari-App/Yukari)** Windows reader app.

</div>

<div align="center">
    <h2>📚 Comic Source Installation</h2>

**Yukari** doesn't come with pre-installed **Comic Sources** for legal reasons. You add the sources you want through **community plugins**.

</div>

- Go to **Releases** and download the `.dll` file from the latest version;
  - If your installed **Yukari** is not up to date, download a **compatible** plugin.
- Inside **Yukari**, go to **Settings** and look for **Sources**;
- Click on **Add New Source** and select the downloaded `.dll` Plugin;
- Done. Now you can go to **Discover** and search for **Comics** in that source.

<div align="center">
    <h2>⚙️ Settings</h2>
</div>

Available in **Yukari** (**Core 2.6.0+**) via **Settings → Comic Sources → ⋯ menu → Settings**:

- **Use WebView for Challenges** (Default: *Enabled*): Uses an embedded browser to resolve Cloudflare Turnstile anti-bot challenges when required. Can be turned off if MangaFire challenges are not currently active or to bypass browser challenge resolution.

<div align="center">
    <h2>🗒️ Notes</h2>
</div>

- No login required.
- Uses the official MangaFire API — image requests require a valid Referer header, which Yukari's plugin architecture (Core 2.4.0+) handles automatically.
- **Filters**: Supports content rating, type, demographic, status and genres.
- **Languages**: English (en), Français (fr), Español (es), Español LA (es-la), Português (pt-br), Português (EU) (pt), 日本語 (ja).
- **Page count per chapter**: The total number of pages isn't available until a chapter is opened. A `?` is shown in the chapter list until then.
- **Cloudflare Protection** (Core 2.5.0+): Uses an embedded browser to bypass challenges. May request manual verification if auto-resolution fails. Can be toggled on/off in the source settings.
- **Errors**: Returns empty results instead of crashing on API issues (e.g., invalid ID).

<div align="center">
    <h2>🤝 Contributing</h2>
  
Contributions are welcome! You can help improve **Yukari.Plugin.MangaFire** in several ways:
</div>

- 🐛 **Report issues**: Found a bug or unexpected behavior? Open an [issue](../../issues) describing the problem.
- ✨ **Suggest features**: Have an idea to make **Yukari.Plugin.MangaFire** better? Share it in the issues tab.
- 🔧 **Submit pull requests**: Fix bugs, improve code quality, or add new features.

<div align="center">
  <h2>📜 License</h2>

This project is licensed under the **GPL-3.0**. See the [LICENSE](LICENSE) file for details.

</div>
