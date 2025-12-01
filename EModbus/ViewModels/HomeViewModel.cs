using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EModbus.Model;

namespace EModbus.ViewModels;

[Page("home")]
public partial class HomeViewModel : ViewModelBase, INavigable
{
    public HomeViewModel(SlaveViewModel slaveViewModel, MasterViewModel masterViewModel)
    {
        SlaveViewModel = slaveViewModel;
        MasterViewModel = masterViewModel;
    }

    [ObservableProperty]
    public partial SlaveViewModel SlaveViewModel { get; set; }

    [ObservableProperty]
    public partial MasterViewModel MasterViewModel { get; set; }

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
