using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EModbus.Model;

namespace EModbus.ViewModels;

[Page("home")]
public partial class HomeViewModel(SlaveViewModel slaveViewModel, MasterViewModel masterViewModel) : ViewModelBase, INavigable
{
    [ObservableProperty]
    public partial SlaveViewModel SlaveViewModel { get; set; } = slaveViewModel;

    [ObservableProperty]
    public partial MasterViewModel MasterViewModel { get; set; } = masterViewModel;

    [ObservableProperty]
    public partial int TabSelectedIndex { get; set; } = 0;

    [RelayCommand]
    private void BackToSlave()
    {
        TabSelectedIndex = 0;
    }

    [RelayCommand]
    private void NextToMaster()
    {
        TabSelectedIndex = 1;
    }
}
