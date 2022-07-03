﻿using Digimezzo.Foundation.Core.Logging;
using Dopamine.Utils;
using Dopamine.Core.Prism;
using Dopamine.Services.Playback;
using Prism.Ioc;
using Prism.Events;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Dopamine.Services.Utils;

namespace Dopamine.Views.Common
{
    public partial class LyricsControl : UserControl
    {
        private IPlaybackService playbackService;
        private IEventAggregator eventAggregator;
        private ListBox lyricsListBox;
        private TextBox lyricsTextBox;
       
        public LyricsControl()
        {
            InitializeComponent();

            this.playbackService = ContainerLocator.Current.Resolve<IPlaybackService>();
            this.eventAggregator = ContainerLocator.Current.Resolve<IEventAggregator>();
            this.eventAggregator.GetEvent<ScrollToHighlightedLyricsLine>().Subscribe((_) => this.ScrollToHighlightedLyricsLineAsync());
        }
    
        private void LyricsListBox_Loaded(object sender, RoutedEventArgs e)
        {
            // This is a workaround to be able to access the LyricsListBox which is in the DataTemplate.
            try
            {
                this.lyricsListBox = sender as ListBox;
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not get lyricsListBox from the DataTemplate. Exception: {0}", ex.Message);
            }
        }

        private void LyricsTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            // This is a workaround to be able to access the LyricsTextBox which is in the DataTemplate.
            try
            {
                this.lyricsTextBox = sender as TextBox;
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not get lyricsTextBox from the DataTemplate. Exception: {0}", ex.Message);
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.T & Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (!this.playbackService.IsStopped) this.AddTimeStampToSelectedLyricsLine();
            }
        }

        private async void ScrollToHighlightedLyricsLineAsync()
        {
            if (this.lyricsListBox == null) return;

            try
            {
                // When shutting down, Application.Current is null
                if (Application.Current != null)
                {
                    await Application.Current.Dispatcher.Invoke(async () =>
                    {
                        await ScrollUtils.ScrollToHighlightedLyricsLineAsync(this.lyricsListBox);
                    });
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not scroll to the highlighted lyrics line. Exception: {0}", ex.Message);
            }
        }

        private void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.lyricsTextBox != null)
            {
                try
                {
                    // Using the Dispatcher seems to be the only way to ever make the TextBox focus.
                    // See: http://stackoverflow.com/questions/13955340/keyboard-focus-does-not-work-on-text-box-in-wpf
                    Dispatcher.BeginInvoke(DispatcherPriority.Input,
                          new Action(delegate ()
                          {
                              this.lyricsTextBox.Focus(); // Set Logical Focus
                              Keyboard.Focus(this.lyricsTextBox); // Set Keyboard Focus (this is probably not needed)
                          }));
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not set focus on lyricsTextBox. Exception: {0}", ex.Message);
                }

            }
        }

        private void AddTimeStampToSelectedLyricsLine()
        {
            if (this.lyricsTextBox != null)
            {
                try
                {
                    int lineIndex = this.lyricsTextBox.GetLineIndexFromCharacterIndex(this.lyricsTextBox.CaretIndex);
                    int lineStartIndex = this.lyricsTextBox.GetCharacterIndexFromLineIndex(lineIndex);
                    string line = this.lyricsTextBox.GetLineText(lineIndex);

                    if (line.Trim().Length == 0)
                    {
                        this.lyricsTextBox.CaretIndex += 1; // Jump to the next line
                        return; // Don't try to add a timeStamp to an empty line (Trim removes newline characters)
                    }

                    string strippedLine = string.Empty;

                    if (line.Length > 0 && line.StartsWith("["))
                    {
                        int index = line.IndexOf(']');

                        if (index > 0)
                        {
                            strippedLine = line.Substring(index + 1);
                        }
                    }
                    else
                    {
                        strippedLine = line;
                    }

                    TimeSpan currentPlaybackTime = this.playbackService.GetCurrentTime;

                    int minutes = currentPlaybackTime.Minutes + currentPlaybackTime.Hours * 60;
                    int seconds = currentPlaybackTime.Seconds;
                    int milliseconds = currentPlaybackTime.Milliseconds;

                    string format = string.Format("[{0:00}:{1:00}.{2:000}]", minutes, seconds, milliseconds);
                    string newLine = string.Format("{0}{1}", new DateTime(currentPlaybackTime.Ticks).ToString(format), strippedLine);

                    this.lyricsTextBox.Text = this.lyricsTextBox.Text.Remove(lineStartIndex, line.Length);
                    this.lyricsTextBox.Text = this.lyricsTextBox.Text.Insert(lineStartIndex, newLine);
                    this.lyricsTextBox.CaretIndex = lineStartIndex + newLine.Length;

                    // Jump over empty lines
                    line = this.lyricsTextBox.GetLineText(this.lyricsTextBox.GetLineIndexFromCharacterIndex(this.lyricsTextBox.CaretIndex));

                    while (line.Trim().Length == 0)
                    {
                        // Make sure to get out of the loop if an empty line was found at the end of the lyrics
                        if (this.lyricsTextBox.CaretIndex == this.lyricsTextBox.Text.Length) break;
                        this.lyricsTextBox.CaretIndex += 1;
                        line = this.lyricsTextBox.GetLineText(this.lyricsTextBox.GetLineIndexFromCharacterIndex(this.lyricsTextBox.CaretIndex));
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not add timeStamp to selected lyrics line. Exception: {0}", ex.Message);
                }
            }
        }
    }
}