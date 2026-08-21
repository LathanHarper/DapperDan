using Prism.Navigation;

namespace CodeCrafty.DapperDan.Views.Diagnostics;

public sealed class IosPrismBootPage : ContentPage
{
    public IosPrismBootPage(INavigationService navigationService)
    {
        Console.WriteLine("DAPPER_BOOT 03 Prism created the first diagnostic page");

        var status = new Label
        {
            AutomationId = "DapperDan_PrismBoot_Status",
            FontSize = 24,
            HorizontalTextAlignment = TextAlignment.Center,
            Text = "Prism created this code-only page after loading 2 resource dictionaries.",
            TextColor = Colors.Black
        };
        var continueButton = new Button
        {
            AutomationId = "DapperDan_PrismBoot_Continue",
            Text = "Continue through Prism"
        };

        continueButton.Clicked += async (_, _) =>
        {
            Console.WriteLine("DAPPER_BOOT 04 Prism navigation requested");
            var result = await navigationService.NavigateAsync(nameof(IosPrismSecondPage));
            if (!result.Success)
            {
                status.Text = $"Prism navigation failed: {result.Exception?.GetType().Name ?? "unknown"}";
                Console.WriteLine("DAPPER_BOOT 05 Prism navigation failed");
            }
        };

        AutomationId = "DapperDan_PrismBoot_Page1";
        BackgroundColor = Colors.White;
        Content = new VerticalStackLayout
        {
            Padding = new Thickness(32),
            Spacing = 24,
            VerticalOptions = LayoutOptions.Center,
            Children = { status, continueButton }
        };
    }
}

public sealed class IosPrismSecondPage : ContentPage
{
    public IosPrismSecondPage()
    {
        Console.WriteLine("DAPPER_BOOT 06 Prism created the second diagnostic page");

        AutomationId = "DapperDan_PrismBoot_Page2";
        BackgroundColor = Colors.White;
        Content = new Label
        {
            FontSize = 24,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Text = "Prism navigation reached page two.",
            TextColor = Colors.Black
        };
    }
}
