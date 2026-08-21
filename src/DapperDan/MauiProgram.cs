using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using CodeCrafty.DapperDan.Controls;
using CodeCrafty.DapperDan.Data;
using CodeCrafty.DapperDan.PanelBossKit;
using CodeCrafty.DapperDan.ViewModels;
using CodeCrafty.DapperDan.Views.DapperDan;
using CodeCrafty.DapperDan.Views.Diagnostics;

namespace CodeCrafty.DapperDan;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
#if !IOS
        SQLitePCL.Batteries_V2.Init();
#endif

        var builder = MauiApp.CreateBuilder();
#if IOS
        builder
            .UseMauiApp<App>()
            .UsePrism(prism => prism
                .RegisterTypes(RegisterIosDiagnosticTypes)
                .CreateWindow($"NavigationPage/{nameof(IosPrismRootPage)}"));
#else
        builder
            .UseMauiApp<App>()
            .UsePrism(prism => prism
                .RegisterTypes(RegisterPrismTypes)
                .CreateWindow("NavigationPage/DapperDanPage"))
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<IDatabasePathProvider, MauiDatabasePathProvider>();
        builder.Services.AddDbContextFactory<DapperDanDbContext>((services, options) =>
        {
            var databasePath = services
                .GetRequiredService<IDatabasePathProvider>()
                .GetDatabasePath();

            options.UseSqlite(
                $"Data Source={databasePath};Mode=ReadWriteCreate;Cache=Private");
        });
        builder.Services.AddSingleton<DatabaseInitializer>();
        builder.Services.AddSingleton<IKeikiRepository, KeikiRepository>();
        builder.Services.AddTransient<PanelBoss>();
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void RegisterPrismTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation<NavigationPage>();
        containerRegistry.RegisterForNavigation<DapperDanPage, DapperDanViewModel>();
    }

#if IOS
    private static void RegisterIosDiagnosticTypes(IContainerRegistry containerRegistry)
    {
        // Leave NavigationPage unregistered so Prism installs its
        // PrismNavigationPage implementation during initialization.
        containerRegistry.RegisterForNavigation<IosPrismRootPage>();
        containerRegistry.RegisterForNavigation<IosPrismBootPage>();
        containerRegistry.RegisterForNavigation<IosPrismSecondPage>();
    }
#endif
}
