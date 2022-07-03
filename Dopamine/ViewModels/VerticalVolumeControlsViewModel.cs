using Dopamine.Services.Playback;
using Prism.Ioc;

namespace Dopamine.ViewModels
{
    public class VerticalVolumeControlsViewModel : VolumeControlsViewModel
    {
        // Workaround to have inheritance with dependency injection
        public VerticalVolumeControlsViewModel() : base(ContainerLocator.Current.Resolve<IPlaybackService>())
        {
        }
    }
}