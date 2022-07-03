﻿using Dopamine.Services.Playback;
using Prism.Commands;
using Prism.Events;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Prism.Ioc;

namespace Dopamine.Views.Common.Base
{
    public abstract class CommonViewBase : UserControl
    {
        protected IEventAggregator eventAggregator;
        protected IPlaybackService playbackService;
    
        public DelegateCommand ViewInExplorerCommand { get; set; }
        public DelegateCommand JumpToPlayingTrackCommand { get; set; }
       
        public CommonViewBase()
        {
            // We need a parameterless constructor to be able to use this UserControl in other UserControls without dependency injection.
            // So for now there is no better solution than to find the EventAggregator by using the ServiceLocator.
            this.eventAggregator = ContainerLocator.Current.Resolve<IEventAggregator>();
            this.playbackService = ContainerLocator.Current.Resolve<IPlaybackService>();
        }
     
        protected abstract Task KeyUpHandlerAsync(object sender, KeyEventArgs e);
        protected abstract Task ActionHandler(Object sender, DependencyObject source, bool enqueue);
        protected abstract Task ScrollToPlayingTrackAsync(Object sender);
        protected abstract void ViewInExplorer(Object sender);
    }
}
