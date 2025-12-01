using EModbus.Model;
using ShadUI;

namespace EModbus.ViewModels;

[Page("home")]
public partial class HomeViewModel(DialogManager dialogManager, ToastManager toastManager)
    : ViewModelBase,
        INavigable
{

}
