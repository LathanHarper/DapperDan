using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using CodeCrafty.DapperDan.Controls;
using CodeCrafty.DapperDan.Data;
using CodeCrafty.DapperDan.PanelBossKit;
using CodeCrafty.DapperDan.ViewModels;
using CodeCrafty.DapperDan.Views.DapperDan;

namespace CodeCrafty.DapperDan;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        SQLitePCL.Batteries_V2.Init();

        var builder = MauiApp.CreateBuilder();
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

#if IOS
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<RichButton, NativePrimaryTapViewHandler>();
        });
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void RegisterPrismTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation<DapperDanPage, DapperDanViewModel>();
    }
}
