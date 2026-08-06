# AGENTS.md

## Project Overview
- **Type:** WPF (.NET 8) / Node.js ES Modules
- **Architecture:** MVVM with CommunityToolkit.Mvvm / Express API

## Build & Test Commands
- **Run Tests:** `dotnet test` or `npm test`
- **Build Solution:** `dotnet build` or `npm run build`

## Key Boundaries
- Do not edit third-party wrappers in `src/Vendor/`.
- Always put WPF XAML Styles in `App.xaml` or `Themes/Generic.xaml`.