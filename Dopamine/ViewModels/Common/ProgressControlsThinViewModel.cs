using Dopamine.Services.Playback;
using Dopamine.Services.Playback;
using Prism.Ioc;

namespace Dopamine.ViewModels.Common
{
    public class ProgressControlsThinViewModel : ProgressControlsViewModel
    {
        public ProgressControlsThinViewModel() : base(ContainerLocator.Current.Resolve<IPlaybackService>())
        {
        }
    }
}