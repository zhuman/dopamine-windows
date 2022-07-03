using Dopamine.Services.Metadata;
using Dopamine.Services.Playback;
using Dopamine.Services.Scrobbling;
using Prism.Ioc;

namespace Dopamine.ViewModels.Common
{
    public class CoverPlaybackInfoControlViewModel : PlaybackInfoControlViewModel
    {
        public CoverPlaybackInfoControlViewModel() : base(
            ContainerLocator.Current.Resolve<IPlaybackService>(),
            ContainerLocator.Current.Resolve<IMetadataService>(),
            ContainerLocator.Current.Resolve<IScrobblingService>())
        {
        }
    }
}