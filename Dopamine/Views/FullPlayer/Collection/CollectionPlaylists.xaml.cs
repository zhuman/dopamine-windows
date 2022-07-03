﻿using Prism.Ioc;
using Digimezzo.Foundation.Core.IO;
using Digimezzo.Foundation.Core.Logging;
using Dopamine.Core.Prism;
using Dopamine.Services.Entities;
using Dopamine.Services.Playlist;
using Dopamine.Views.Common.Base;
using Prism.Commands;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dopamine.Views.FullPlayer.Collection
{
    public partial class CollectionPlaylists : TracksViewBase
    {
        private IPlaylistService playlistService;

        public DelegateCommand ViewPlaylistInExplorerCommand { get; set; }

        public CollectionPlaylists()
        {
            InitializeComponent();

            // We need a parameterless constructor to be able to use this UserControl in other UserControls without dependency injection.
            // So for now there is no better solution than to find the EventAggregator by using the ServiceLocator.
            this.playlistService = ContainerLocator.Current.Resolve<IPlaylistService>();

            // Commands
            this.ViewPlaylistInExplorerCommand = new DelegateCommand(() => this.ViewPlaylistInExplorer(this.ListBoxPlaylists));
            this.ViewInExplorerCommand = new DelegateCommand(() => this.ViewInExplorer(this.ListBoxTracks));
            this.JumpToPlayingTrackCommand = new DelegateCommand(() => this.ScrollToPlayingTrackAsync(this.ListBoxTracks));

            // PubSub Events
            this.eventAggregator.GetEvent<ScrollToPlayingTrack>().Subscribe(async (_) => await this.ScrollToPlayingTrackAsync(this.ListBoxTracks));
        }

        private async void ListBoxPlaylists_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            await this.ActionHandler(sender, e.OriginalSource as DependencyObject, true);
        }

        private async void ListBoxPlaylists_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await this.ActionHandler(sender, e.OriginalSource as DependencyObject, true);
            }
        }

        private async void ListBoxTracks_KeyUp(object sender, KeyEventArgs e)
        {
            await this.KeyUpHandlerAsync(sender, e);
        }

        private async void ListBoxTracks_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            await this.ActionHandler(sender, e.OriginalSource as DependencyObject, true);
        }

        private async void ListBoxTracks_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await this.ActionHandler(sender, e.OriginalSource as DependencyObject, true);
            }
        }

        private void PlaylistsButton_Click(object sender, RoutedEventArgs e)
        {
            this.ListBoxPlaylists.SelectedItem = null;
        }

        private void ListBoxPlaylists_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (this.ListBoxPlaylists.SelectedItem != null)
                {
                    this.ViewPlaylistInExplorer(this.ListBoxPlaylists);
                }
            }
        }

        private void ViewPlaylistInExplorer(Object sender)
        {
            try
            {
                // Cast sender to ListBox
                ListBox lb = (ListBox)sender;

                if (lb.SelectedItem != null)
                {
                    PlaylistViewModel playlist = lb.SelectedItem as PlaylistViewModel;

                    Actions.TryViewInExplorer(playlist.Path);
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not view playlist in Windows Explorer. Exception: {0}", ex.Message);
            }
        }
    }
}
