using Dopamine.Core.Utils;
using Dopamine.Services.Playback;
using System;
using Prism.Ioc;

namespace Dopamine.ViewModels.Common
{
    public class ProgressControlsWithTimeViewModel : ProgressControlsViewModel
    {
        private string currentTime;
        private string totalTime;
      
        public string CurrentTime
        {
            get { return this.currentTime; }
            set { SetProperty<string>(ref this.currentTime, value); }
        }

        public string TotalTime
        {
            get { return this.totalTime; }
            set { SetProperty<string>(ref this.totalTime, value); }
        }
  
        public ProgressControlsWithTimeViewModel() : base(ContainerLocator.Current.Resolve<IPlaybackService>())
        {
            this.CurrentTime = FormatUtils.FormatTime(new TimeSpan(0));
            this.TotalTime = FormatUtils.FormatTime(new TimeSpan(0));
        }
    
        protected override void GetPlayBackServiceProgress()
        {
            base.GetPlayBackServiceProgress();

            this.CurrentTime = FormatUtils.FormatTime(this.playBackService.GetCurrentTime);
            this.TotalTime = FormatUtils.FormatTime(this.playBackService.GetTotalTime);
        }
    }
}
