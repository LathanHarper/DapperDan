namespace CodeCrafty.DapperDan;

public partial class App : Application
{
    public App()
    {
#if IOS
        Console.WriteLine("DAPPER_BOOT 01 Loading App.xaml resources");
        InitializeComponent();
        Console.WriteLine($"DAPPER_BOOT 02 App.xaml loaded with {Resources.MergedDictionaries.Count} merged dictionaries");
#else
        InitializeComponent();
#endif
    }

#if IOS
    protected override Window CreateWindow(IActivationState? activationState)
    {
        Console.WriteLine("DAPPER_BOOT 03 CreateWindow entered");

        var status = new Label
        {
            AutomationId = "DapperDan_BareBoot_Status",
            FontSize = 24,
            HorizontalTextAlignment = TextAlignment.Center,
            Text = $"Dapper Dan loaded App.xaml resources ({Resources.MergedDictionaries.Count} dictionaries).",
            TextColor = Colors.Black
        };
        var continueButton = new Button
        {
            AutomationId = "DapperDan_BareBoot_Continue",
            Text = "Continue bare-board test"
        };
        var root = new ContentPage
        {
            AutomationId = "DapperDan_BareBoot_Page1",
            BackgroundColor = Colors.White,
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(32),
                Spacing = 24,
                VerticalOptions = LayoutOptions.Center,
                Children = { status, continueButton }
            }
        };
        var navigation = new NavigationPage(root);

        continueButton.Clicked += async (_, _) =>
        {
            Console.WriteLine("DAPPER_BOOT 04 Click reached");
            await navigation.PushAsync(new ContentPage
            {
                AutomationId = "DapperDan_BareBoot_Page2",
                BackgroundColor = Colors.White,
                Content = new Label
                {
                    FontSize = 24,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Text = "Second code-only page rendered.",
                    TextColor = Colors.Black
                }
            });
            Console.WriteLine("DAPPER_BOOT 05 Second page pushed");
        };

        Console.WriteLine("DAPPER_BOOT 06 Window returned");
        return new Window(navigation);
    }
#endif
}
