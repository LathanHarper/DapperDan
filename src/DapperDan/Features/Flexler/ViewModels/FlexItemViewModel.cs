using Flexler.Models;
using Microsoft.Maui.Layouts;
using Prism.Mvvm;

namespace Flexler.ViewModels;

public sealed class FlexItemViewModel : BindableBase
{
    private FlexAlignSelf _alignSelf = FlexAlignSelf.Auto;
    private FlexBasisOption _basisOption;
    private float _grow;
    private bool _isSelected;
    private int _order;
    private float _shrink = 1;
    private string _text;

    public FlexItemViewModel(int id, string text, FlexBasisOption basisOption)
    {
        Id = id;
        _text = text;
        _basisOption = basisOption;
    }

    public int Id { get; }

    public string AutomationId => $"Flexler_Item_{Id}";

    public FlexAlignSelf AlignSelf
    {
        get => _alignSelf;
        set => SetProperty(ref _alignSelf, value);
    }

    public FlexBasis Basis => BasisOption.Value;

    public FlexBasisOption BasisOption
    {
        get => _basisOption;
        set
        {
            if (SetProperty(ref _basisOption, value))
            {
                RaisePropertyChanged(nameof(Basis));
            }
        }
    }

    public float Grow
    {
        get => _grow;
        set => SetProperty(ref _grow, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public int Order
    {
        get => _order;
        set => SetProperty(ref _order, value);
    }

    public float Shrink
    {
        get => _shrink;
        set => SetProperty(ref _shrink, value);
    }

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }
}
