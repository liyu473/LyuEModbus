using Jab;
using ShadUI;

namespace EModbus.Services;

[ServiceProviderModule]
[Singleton<DialogManager>]
[Singleton<ToastManager>]
[Singleton<NavigationService>]
public interface IUtilitiesModule
{
    
}