using CommunityToolkit.Mvvm.ComponentModel;
using EModbus.Extensions;
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
}
