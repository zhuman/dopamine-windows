using Dopamine.ViewModels;
using Dopamine.Services.Playback;
using Prism.Events;
using Prism.Ioc;

namespace Dopamine.ViewModels.Common
{
    public class CompactPlaybackControlsViewModel : PlaybackControlsViewModel
    {
        public CompactPlaybackControlsViewModel() : base(ContainerLocator.Current.Resolve<IPlaybackService>(), ContainerLocator.Current.Resolve<IEventAggregator>())
        {
        }
    }
}
