# Contributing to MN-Barcode

First off — thank you for taking the time to contribute! 🎉
This project aims to be a simple, dependable, offline POS for small businesses, and
community help keeps it that way.

*Katkıda bulunmak isteyen herkese teşekkürler! İngilizce ya da Türkçe issue/PR açabilirsin.*

## Ways to contribute

- 🐛 **Report a bug** — open an issue with clear steps to reproduce
- 💡 **Suggest a feature** — describe the problem it solves for a shopkeeper
- 🌍 **Improve docs / translations** — README, comments, UI text
- 🔧 **Send a pull request** — bug fixes, features, refactors

## Development setup

```bash
git clone https://github.com/EminAkplt/MN-Barcode.git
cd MN-Barcode
dotnet build
dotnet run --project MN-Barcode.WinForms
```

- **Requirements:** Windows 10/11, [.NET 9 SDK](https://dotnet.microsoft.com/download), SQL Server LocalDB.
- The database is created automatically on first run; default login is `admin` / `admin123`.
- See the [README](README.md) for architecture and configuration.

## Pull request guidelines

1. **Branch** from `master` (e.g. `fix/negative-stock`, `feat/receipt-printer`).
2. **Keep changes focused** — one logical change per PR is easiest to review.
3. **Build before pushing:** `dotnet build` must succeed with **0 errors**.
4. **Match the existing style** — the code follows the surrounding conventions
   (Turkish domain terms, `Theme`-based UI colors, layered structure). Read a nearby
   file before adding a new one.
5. **Describe the "why"** in the PR, not just the "what".

## Project structure

| Project | Responsibility |
|---|---|
| `MN-Barcode.Entities` | Plain data classes |
| `MN-Barcode.DataAccess` | EF Core `BarcodeContext` + migrations |
| `MN-Barcode.Business` | Business-logic services |
| `MN-Barcode.WinForms` | Desktop UI |

Business logic lives in the `Business` layer — please don't put data access or
business rules directly in Forms.

## Code of conduct

Be respectful and constructive. We want this to be a welcoming project for
newcomers and shopkeepers-turned-tinkerers alike.

## License

By contributing, you agree that your contributions will be licensed under the
[MIT License](LICENSE).
