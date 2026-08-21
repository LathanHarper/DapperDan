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
}
