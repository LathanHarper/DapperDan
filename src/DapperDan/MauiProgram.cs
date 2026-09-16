using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using CodeCrafty.DapperDan.Controls;
using CodeCrafty.DapperDan.Data;
using CodeCrafty.DapperDan.Data.CompiledModels;
using CodeCrafty.DapperDan.Diagnostics;
using CodeCrafty.DapperDan.PanelBossKit;
using CodeCrafty.DapperDan.Speech;
using CodeCrafty.DapperDan.ViewModels;
using CodeCrafty.DapperDan.Views.BillboardCanary;
using CodeCrafty.DapperDan.Views.DapperDan;
using CodeCrafty.DapperDan.Views.RotationCanary;

namespace CodeCrafty.DapperDan;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        CrashJournal.Checkpoint(CrashPoint.MauiProgramEnter);

        try
        {
            CrashJournal.Checkpoint(CrashPoint.SqliteNativeEnter);
            SQLitePCL.Batteries_V2.Init();
            CrashJournal.Checkpoint(CrashPoint.SqliteNativeReady);

            CrashJournal.Checkpoint(CrashPoint.MauiBuilderEnter);
            var builder = MauiApp.CreateBuilder();
            CrashJournal.Checkpoint(CrashPoint.MauiBuilderReady);
            builder
                .UseMauiApp<App>()
                .UsePrism(prism => prism
                    .RegisterTypes(RegisterPrismTypes)
#if FLEXLER_SHOWCASE_PROOF
                    .CreateWindow("NavigationPage/FlexlerShowcasePage", ReportStartupFailure))
#else
                    .CreateWindow("NavigationPage/DapperDanPage", ReportStartupFailure))
#endif
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("InstrumentSans-Regular.ttf", "InstrumentSans");
                    fonts.AddFont("InstrumentSans-SemiBold.ttf", "InstrumentSansSemibold");
                    fonts.AddFont("GeistMono-Medium.ttf", "GeistMono");
                });

            builder.Services.AddSingleton<IDatabasePathProvider, MauiDatabasePathProvider>();
            builder.Services.AddSingleton<IPackagedDatabaseSource, MauiPackagedDatabaseSource>();
            builder.Services.AddSingleton<PackagedDatabaseInstaller>();
            builder.Services.AddDbContextFactory<DapperDanDbContext>((services, options) =>
            {
                var databasePath = services
                    .GetRequiredService<IDatabasePathProvider>()
                    .GetDatabasePath();

                CrashJournal.Checkpoint(CrashPoint.CompiledModelEnter);
                var compiledModel = DapperDanDbContextModel.Instance;
                CrashJournal.Checkpoint(CrashPoint.CompiledModelReady);

                options
                    .UseSqlite($"Data Source={databasePath};Mode=ReadWrite;Cache=Private;Foreign Keys=True")
                    .UseModel(compiledModel);
                CrashJournal.Checkpoint(CrashPoint.DatabaseOptionsReady);
            });
            builder.Services.AddSingleton<DatabaseInitializer>();
            builder.Services.AddSingleton<IKeikiRepository, KeikiRepository>();
#if IOS
            builder.Services.AddSingleton<IVoiceCanaryService, IosVoiceCanaryService>();
#else
            builder.Services.AddSingleton<IVoiceCanaryService, UnsupportedVoiceCanaryService>();
#endif
            builder.Services.AddTransient<PanelBoss>();

            builder.ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<PrimoMaterial.CrossFade_RadoDots, PrimoMaterial.CrossFadeRadoDotsHandler>();
#if IOS || WINDOWS
                handlers.AddHandler<Flexler.Controls.RichButton, Flexler.Controls.NativePrimaryTapViewHandler>();
#endif
            });

            Flexler.Controls.RichButtonDiagnostics.CommandExceptionReporter = context =>
            {
                CrashJournal.Capture(CrashSource.RichButtonCommand,
                    CrashPoint.RichButtonCommandException, context.Exception, terminating: false);
                return Task.CompletedTask;
            };

            RichButtonDiagnostics.CommandExceptionReporter = context =>
            {
                CrashJournal.Capture(
                    CrashSource.RichButtonCommand,
                    CrashPoint.RichButtonCommandException,
                    context.Exception,
                    terminating: false);
                return Task.CompletedTask;
            };

            CrashJournal.Checkpoint(CrashPoint.MauiServicesReady);

#if IOS
            builder.ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<RichButton, NativePrimaryTapViewHandler>();
            });
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif

            CrashJournal.Checkpoint(CrashPoint.MauiBuildEnter);
            var app = builder.Build();
            CrashJournal.Checkpoint(CrashPoint.MauiBuildReady);
            return app;
        }
        catch (Exception exception)
        {
            CrashJournal.Capture(
                CrashSource.GuardedSeam,
                CrashPoint.GuardedFailure,
                exception,
                terminating: true);
            throw;
        }
    }

    private static void ReportStartupFailure(Exception exception)
    {
        CrashJournal.Capture(CrashSource.HandledStartupFailure,
            CrashPoint.GuardedFailure, exception, terminating: false);
#if DEBUG
        Console.Error.WriteLine($"Dapper startup navigation failed: {exception}");
#endif
    }

    private static void RegisterPrismTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<Flexler.PanelBossKit.PanelBoss>();
        containerRegistry.RegisterInstance<IClipboard>(Clipboard.Default);
        containerRegistry.RegisterSingleton<Flexler.Services.IFlexRecipeExporter, Flexler.Services.FlexRecipeExporter>();
        containerRegistry.RegisterInstance<Flexler.Services.IFavoriteRecipeStore>(
            new Flexler.Services.FileFavoriteRecipeStore(Path.Combine(FileSystem.AppDataDirectory, "FlexlerShowcase")));
        containerRegistry.RegisterForNavigation<Flexler.Views.MainPage, Flexler.ViewModels.MainPageViewModel>("FlexlerShowcasePage");
        containerRegistry.RegisterForNavigation<BillboardCanaryPage, BillboardCanaryViewModel>();
        containerRegistry.RegisterForNavigation<DapperDanPage, DapperDanViewModel>();
        containerRegistry.RegisterForNavigation<RotationCanaryPage, RotationCanaryViewModel>();
    }
}
