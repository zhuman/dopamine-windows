using Dopamine.ViewModels;
using Dopamine.Services.Playback;
using Prism.Ioc;

namespace Dopamine.ViewModels.Common
{
    public class HorizontalVolumeControlsViewModel : VolumeControlsViewModel
    {
        public HorizontalVolumeControlsViewModel() : base(ContainerLocator.Current.Resolve<IPlaybackService>())
        {
        }
    }
}