using Prism.Mvvm;

namespace CodeCrafty.DapperDan.Models;

public sealed class DapperDanPageAction : BindableBase
{
    private Color _backgroundColor;
    private bool _isBusy;
    private bool _isSelected;
    private Color _textColor;

    public DapperDanPageAction(
        string title,
        string glyph,
        string panelName,
        string automationId)
    {
        Title = title;
        Glyph = glyph;
        PanelName = panelName;
        AutomationId = automationId;
        _backgroundColor = Color.FromArgb("#F4F7FA");
        _textColor = Color.FromArgb("#304A5B");
    }

    public string AutomationId { get; }

    public Color BackgroundColor
    {
        get => _backgroundColor;
        private set => SetProperty(ref _backgroundColor, value);
    }

    public string Glyph { get; }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        private set => SetProperty(ref _isSelected, value);
    }

    public string PanelName { get; }

    public Color TextColor
    {
        get => _textColor;
        private set => SetProperty(ref _textColor, value);
    }

    public string Title { get; }

    public void SetSelected(bool isSelected)
    {
        IsSelected = isSelected;
        BackgroundColor = isSelected
            ? Color.FromArgb("#075985")
            : Color.FromArgb("#F4F7FA");
        TextColor = isSelected
            ? Colors.White
            : Color.FromArgb("#304A5B");
    }
}
