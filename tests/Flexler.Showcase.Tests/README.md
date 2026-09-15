# Flexler showcase functional tests

This project links the production Flexler feature source under `src/DapperDan/Features/Flexler` and uses the repository's centrally managed MAUI, Prism and test packages. It runs on the plain .NET 10 target without building the mobile host.

```powershell
dotnet test tests/Flexler.Showcase.Tests/Flexler.Showcase.Tests.csproj -c Release --nologo
```

The suite exercises recipe export through MAUI's real XAML loader, view-model editing and command state, favorites persistence and recovery, PanelBoss transitions, FlexLayout measure invalidation and button activation. It uses synthetic data, a fake clipboard and disposable, uniquely named temporary favorites folders. It does not read user favorites or call external services.

These checks do not prove native rendering, operating-system clipboard access, navigation integration or a signed iOS launch. Native UI and release verification remain separate. See the repository's Prism licensing guidance before restoring or using its dependencies.
