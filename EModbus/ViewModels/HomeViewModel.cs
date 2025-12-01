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
    private SlaveViewModel slaveViewModel;

    [ObservableProperty]
    private MasterViewModel masterViewModel;
}
