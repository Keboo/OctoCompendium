using System.Diagnostics.CodeAnalysis;
using Uno.Resizetizer;

namespace OctoCompendium;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    [SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Uno.Extensions APIs are used in a way that is safe for trimming in this template context.")]
    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .UseToolkitNavigation()
            .Configure(host => host
#if DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)
                        .CoreLogLevel(LogLevel.Warning);
                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                .UseLocalization()
                .ConfigureServices((context, services) =>
                {
                    // HTTP client for model downloads
                    services.AddSingleton<HttpClient>();

                    // Matching services
                    services.AddSingleton<IEmbeddingStore, EmbeddingStore>();
                    services.AddSingleton<IModelDownloadService, ModelDownloadService>();
                    services.AddSingleton<IStickerMatcher, StickerMatcher>();

                    // Collection service
                    services.AddSingleton<ICollectionService, CollectionService>();
                })
                .UseNavigation(RegisterRoutes)
            );
        MainWindow = builder.Window;

#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<Shell>();

        // Initialize services after host is built
        var matcher = Host.Services.GetRequiredService<IStickerMatcher>();
        await matcher.InitializeAsync();

        var collection = Host.Services.GetRequiredService<ICollectionService>();
        await collection.InitializeAsync();
    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellViewModel)),
            new ViewMap<MainPage, CollectionViewModel>(),
            new ViewMap<CapturePage, CaptureViewModel>(),
            new DataViewMap<MatchResultPage, MatchResultViewModel, MatchResultData>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    new ("Collection", View: views.FindByViewModel<CollectionViewModel>(), IsDefault: true),
                    new ("Capture", View: views.FindByViewModel<CaptureViewModel>()),
                    new ("MatchResult", View: views.FindByViewModel<MatchResultViewModel>()),
                ]
            )
        );
    }
}
