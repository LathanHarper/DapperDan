using Prism.Navigation;

namespace CodeCrafty.DapperDan.Views.Diagnostics;

public sealed class IosPrismRootPage : ContentPage
{
    public IosPrismRootPage()
    {
        Console.WriteLine("DAPPER_BOOT 03 Prism resolved the parameterless root page");

        var status = new Label
        {
            AutomationId = "DapperDan_PrismRoot_Status",
            FontSize = 24,
            HorizontalTextAlignment = TextAlignment.Center,
            Text = "Prism resolved this parameterless root page without a NavigationPage segment.",
            TextColor = Colors.Black
        };
        var confirmButton = new Button
        {
            AutomationId = "DapperDan_PrismRoot_Confirm",
            Text = "Prism navigate to parameterless page"
        };

        confirmButton.Clicked += async (_, _) =>
        {
            Console.WriteLine("DAPPER_BOOT 04 Attached Prism navigation requested from root page");

            var navigationService = Prism.Navigation.Xaml.Navigation.GetNavigationService(this);
            if (navigationService is null)
            {
                status.Text = "Prism did not attach a navigation service to the root page.";
                Console.WriteLine("DAPPER_BOOT 05 Root page has no attached Prism navigation service");
                return;
            }

            var result = await navigationService.NavigateAsync(nameof(IosPrismSecondPage));
            if (!result.Success)
            {
                status.Text = $"Prism navigation failed: {result.Exception?.GetType().Name ?? "unknown"}";
                Console.WriteLine("DAPPER_BOOT 05 Root page Prism navigation failed");
            }
        };

        AutomationId = "DapperDan_PrismRoot_Page";
        BackgroundColor = Colors.White;
        Content = new VerticalStackLayout
        {
            Padding = new Thickness(32),
            Spacing = 24,
            VerticalOptions = LayoutOptions.Center,
            Children = { status, confirmButton }
        };
    }
}

public sealed class IosPrismHostPage : ContentPage
{
    public IosPrismHostPage()
    {
        Console.WriteLine("DAPPER_BOOT 05 Plain MAUI page created while Prism stays loaded");

        var continueButton = new Button
        {
            AutomationId = "DapperDan_PrismHost_Continue",
            Text = "Continue with MAUI navigation"
        };

        continueButton.Clicked += async (_, _) =>
        {
            Console.WriteLine("DAPPER_BOOT 06 Plain MAUI navigation requested");
            await Navigation.PushAsync(new IosPrismHostSecondPage());
        };

        AutomationId = "DapperDan_PrismHost_Page1";
        BackgroundColor = Colors.White;
        Content = new VerticalStackLayout
        {
            Padding = new Thickness(32),
            Spacing = 24,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    FontSize = 24,
                    HorizontalTextAlignment = TextAlignment.Center,
                    Text = "Prism host initialized. This window bypassed Prism routing and page resolution.",
                    TextColor = Colors.Black
                },
                continueButton
            }
        };
    }
}

public sealed class IosPrismHostSecondPage : ContentPage
{
    public IosPrismHostSecondPage()
    {
        Console.WriteLine("DAPPER_BOOT 07 Plain MAUI navigation reached page two");

        AutomationId = "DapperDan_PrismHost_Page2";
        BackgroundColor = Colors.White;
        Content = new Label
        {
            FontSize = 24,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Text = "MAUI navigation succeeded while Prism stayed loaded.",
            TextColor = Colors.Black
        };
    }
}

public sealed class IosPrismBootPage : ContentPage
{
    public IosPrismBootPage(INavigationService navigationService)
    {
        Console.WriteLine("DAPPER_BOOT 09 Prism injected INavigationService into the diagnostic page");

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
            Console.WriteLine("DAPPER_BOOT 10 Injected Prism navigation requested");
            var result = await navigationService.NavigateAsync(nameof(IosPrismSecondPage));
            if (!result.Success)
            {
                status.Text = $"Prism navigation failed: {result.Exception?.GetType().Name ?? "unknown"}";
                Console.WriteLine("DAPPER_BOOT 11 Injected Prism navigation failed");
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
        Console.WriteLine("DAPPER_BOOT 06 Prism resolved the parameterless second page");

        var status = new Label
        {
            AutomationId = "DapperDan_PrismSecond_Status",
            FontSize = 24,
            HorizontalTextAlignment = TextAlignment.Center,
            Text = "Prism navigation resolved this parameterless second page.",
            TextColor = Colors.Black
        };
        var continueButton = new Button
        {
            AutomationId = "DapperDan_PrismSecond_Continue",
            Text = "Resolve injected-navigation page"
        };

        continueButton.Clicked += async (_, _) =>
        {
            Console.WriteLine("DAPPER_BOOT 07 Prism navigation requested to injected page");

            var navigationService = Prism.Navigation.Xaml.Navigation.GetNavigationService(this);
            if (navigationService is null)
            {
                status.Text = "Prism did not attach a navigation service to page two.";
                Console.WriteLine("DAPPER_BOOT 08 Page two has no attached Prism navigation service");
                return;
            }

            var result = await navigationService.NavigateAsync(nameof(IosPrismBootPage));
            if (!result.Success)
            {
                status.Text = $"Injected-page resolution failed: {result.Exception?.GetType().Name ?? "unknown"}";
                Console.WriteLine("DAPPER_BOOT 08 Injected-page resolution failed");
            }
        };

        AutomationId = "DapperDan_PrismBoot_Page2";
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
