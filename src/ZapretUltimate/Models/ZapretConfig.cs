using CommunityToolkit.Mvvm.ComponentModel;

namespace ZapretUltimate.Models;

public partial class ZapretConfig : ObservableObject
{
    public string Name { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public ConfigCategory Category { get; init; }

    [ObservableProperty]
    private bool _isSelected;

    public string DisplayName => string.IsNullOrEmpty(Name) ? FileName : Name;

    public override string ToString() => DisplayName;
}
