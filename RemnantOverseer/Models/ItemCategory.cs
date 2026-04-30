using CommunityToolkit.Mvvm.ComponentModel;
using RemnantOverseer.Models.Enums;
using RemnantOverseer.Services;
using System.Collections.Generic;

namespace RemnantOverseer.Models;
public class ItemCategory : ObservableObject
{
    public ItemTypes Type { get; set; }
    public List<Item> Items { get; set; } = [];

    public string Name
    {
        get
        {
            return LocalizationService.ItemTypePluralName(Type);
        }
    }

    public ItemCategory ShallowCopy()
    {
        return (ItemCategory)MemberwiseClone();
    }

    public void RefreshLocalizedProperties()
    {
        OnPropertyChanged(nameof(Name));
    }
}
