using CommunityToolkit.Mvvm.Input;
using EModbus.Extensions;
using EModbus.Model;
using EModbus.Models;
using ShadUI;

namespace EModbus.ViewModels;

[Page("home")]
public partial class HomeViewModel(DialogManager dialogManager, ToastManager toastManager)
    : ViewModelBase,
        INavigable
{
  
}
