using Dopamine.ViewModels;
using Dopamine.Services.Playback;
using Prism.Ioc;

namespace Dopamine.ViewModels.Common
{
    public class PopupVolumeControlsViewModel : VolumeControlsViewModel
    {
        public PopupVolumeControlsViewModel() : base(ContainerLocator.Current.Resolve<IPlaybackService>())
        {
        }
    }
}
