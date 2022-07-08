﻿// Copyright (C) 2011 - 2012, Jacob Johnston 
//
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions: 
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software. 
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE. 

using Dopamine.Core.Audio;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Dopamine.Controls
{
    public enum SpectrumAnimationStyle
    {
        Nervous = 1,
        Gentle
    }

    public class SpectrumAnalyzer : Control
    {
        private const double minDBValue = -90;
        private const double maxDBValue = 0;
        private const double dbScale = (maxDBValue - minDBValue);
        private const int defaultRefreshInterval = 16;

        private Window topLevelWindow;
        private bool isWindowActive;
        private readonly DispatcherTimer animationTimer;
        private Canvas spectrumCanvas;
        private ISpectrumPlayer soundPlayer;
        private GeometryDrawing barDrawing;
        private GeometryGroup barGroup;
        private readonly List<RectangleGeometry> barShapes = new List<RectangleGeometry>();
        private double[] barHeights;
        private float[] channelData = new float[1024];
        private float[] channelPeakData;
        private double bandWidth = 1.0;
        private int maximumFrequency = 20000;
        private int maximumFrequencyIndex = 2047;
        private int minimumFrequency = 20;
        private int minimumFrequencyIndex;
        private int[] barIndexMax;
        private int[] barLogScaleIndexMax;
        private int peakFallDelay = 10;

        public static readonly DependencyProperty AnimationStyleProperty = DependencyProperty.Register("AnimationStyle", typeof(SpectrumAnimationStyle), typeof(SpectrumAnalyzer), new PropertyMetadata(SpectrumAnimationStyle.Nervous, null));

        public SpectrumAnimationStyle AnimationStyle
        {
            get
            {
                return (SpectrumAnimationStyle)GetValue(AnimationStyleProperty);
            }
            set
            {
                SetValue(AnimationStyleProperty, value);
            }
        }

        public static readonly DependencyProperty BarBackgroundProperty = DependencyProperty.Register("BarBackground", typeof(Brush), typeof(SpectrumAnalyzer), new PropertyMetadata(Brushes.White, OnBarBackgroundChanged));

        private static void OnBarBackgroundChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer spectrumAnalyzer = o as SpectrumAnalyzer;

            if (spectrumAnalyzer.barShapes != null && spectrumAnalyzer.barShapes.Count > 0)
            {
                spectrumAnalyzer.barDrawing.Brush = spectrumAnalyzer.BarBackground;
            }
        }

        public Brush BarBackground
        {
            get
            {
                return (Brush)GetValue(BarBackgroundProperty);
            }
            set
            {
                SetValue(BarBackgroundProperty, value);
            }
        }

        public static readonly DependencyProperty BarWidthProperty = DependencyProperty.Register("BarWidth", typeof(double), typeof(SpectrumAnalyzer), new UIPropertyMetadata(1.0, OnBarWidthChanged, OnCoerceBarWidth));

        private static object OnCoerceBarWidth(DependencyObject o, object value)
        {
            return value;
        }

        private static void OnBarWidthChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer spectrumAnalyzer = o as SpectrumAnalyzer;

            if (spectrumAnalyzer.barShapes != null && spectrumAnalyzer.barShapes.Count > 0)
            {
                foreach (var bar in spectrumAnalyzer.barShapes)
                {
                    var origRect = bar.Rect;
                    bar.Rect = new Rect(origRect.X, origRect.Y, spectrumAnalyzer.BarWidth, origRect.Height);
                }
            }
        }

        public double BarWidth
        {
            get
            {
                return (double)GetValue(BarWidthProperty);
            }
            set
            {
                SetValue(BarWidthProperty, value);
            }
        }

        public static readonly DependencyProperty BarCountProperty = DependencyProperty.Register("BarCount", typeof(int), typeof(SpectrumAnalyzer), new UIPropertyMetadata(32, OnBarCountChanged, OnCoerceBarCount));

        private static object OnCoerceBarCount(DependencyObject o, object value)
        {
            SpectrumAnalyzer spectrumAnalyzer = o as SpectrumAnalyzer;
            if (spectrumAnalyzer != null) return spectrumAnalyzer.OnCoerceBarCount((int)value);
            return value;
        }

        private static void OnBarCountChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer spectrumAnalyzer = o as SpectrumAnalyzer;
            if (spectrumAnalyzer != null) spectrumAnalyzer.OnBarCountChanged((int)e.OldValue, (int)e.NewValue);
        }

        protected virtual int OnCoerceBarCount(int value)
        {
            value = Math.Max(value, 1);
            return value;
        }

        protected virtual void OnBarCountChanged(int oldValue, int newValue)
        {
            this.UpdateBarLayout();
        }

        public int BarCount
        {
            get
            {
                return (int)GetValue(BarCountProperty);
            }
            set
            {
                SetValue(BarCountProperty, value);
            }
        }

        public static readonly DependencyProperty BarSpacingProperty = DependencyProperty.Register("BarSpacing", typeof(double), typeof(SpectrumAnalyzer), new UIPropertyMetadata(5.0d, OnBarSpacingChanged, OnCoerceBarSpacing));

        private static object OnCoerceBarSpacing(DependencyObject o, object value)
        {
            SpectrumAnalyzer spectrumAnalyzer = o as SpectrumAnalyzer;

            if (spectrumAnalyzer != null) return spectrumAnalyzer.OnCoerceBarSpacing((double)value);
            return value;
        }

        private static void OnBarSpacingChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer spectrumAnalyzer = o as SpectrumAnalyzer;
            if (spectrumAnalyzer != null) spectrumAnalyzer.OnBarSpacingChanged((double)e.OldValue, (double)e.NewValue);
        }

        protected virtual double OnCoerceBarSpacing(double value)
        {
            value = Math.Max(value, 0);
            return value;
        }

        protected virtual void OnBarSpacingChanged(double oldValue, double newValue)
        {
            this.UpdateBarLayout();
        }

        public double BarSpacing
        {
            get
            {
                return (double)GetValue(BarSpacingProperty);
            }
            set
            {
                SetValue(BarSpacingProperty, value);
            }
        }

        public static readonly DependencyProperty RefreshIntervalProperty = DependencyProperty.Register("RefreshInterval", typeof(int), typeof(SpectrumAnalyzer), new UIPropertyMetadata(defaultRefreshInterval, OnRefreshIntervalChanged, OnCoerceRefreshInterval));

        private static object OnCoerceRefreshInterval(DependencyObject o, object value)
        {
            SpectrumAnalyzer spectrumAnalyzer = o as SpectrumAnalyzer;
            if (spectrumAnalyzer != null) return spectrumAnalyzer.OnCoerceRefreshInterval((int)value);
            return value;
        }

        private static void OnRefreshIntervalChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer spectrumAnalyzer = o as SpectrumAnalyzer;
            if (spectrumAnalyzer != null) spectrumAnalyzer.OnRefreshIntervalChanged((int)e.OldValue, (int)e.NewValue);
        }

        protected virtual int OnCoerceRefreshInterval(int value)
        {
            value = Math.Min(1000, Math.Max(10, value));
            return value;
        }

        protected virtual void OnRefreshIntervalChanged(int oldValue, int newValue)
        {
            animationTimer.Interval = TimeSpan.FromMilliseconds(newValue);
        }

        public int RefreshInterval
        {
            get
            {
                return (int)GetValue(RefreshIntervalProperty);
            }
            set
            {
                SetValue(RefreshIntervalProperty, value);
            }
        }

        static SpectrumAnalyzer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SpectrumAnalyzer), new FrameworkPropertyMetadata(typeof(SpectrumAnalyzer)));
        }

        public SpectrumAnalyzer()
        {
            this.animationTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(defaultRefreshInterval)
            };

            this.animationTimer.Tick += animationTimer_Tick;

            this.Loaded += SpectrumAnalyzer_Loaded;
            this.Unloaded += SpectrumAnalyzer_Unloaded;
        }

        public override void OnApplyTemplate()
        {
            this.spectrumCanvas = (Canvas)GetTemplateChild("PART_SpectrumCanvas");
            this.spectrumCanvas.SizeChanged += spectrumCanvas_SizeChanged;
            this.UpdateBarLayout();
        }

        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            base.OnTemplateChanged(oldTemplate, newTemplate);
            if (this.spectrumCanvas != null) this.spectrumCanvas.SizeChanged -= spectrumCanvas_SizeChanged;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            this.UpdateBarLayout();
            this.UpdateSpectrum();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            this.UpdateBarLayout();
            this.UpdateSpectrum();
        }

        public void RegisterSoundPlayer(ISpectrumPlayer soundPlayer)
        {
            this.UnregisterSoundPlayer();

            this.soundPlayer = soundPlayer;
            this.soundPlayer.PropertyChanged += soundPlayer_PropertyChanged;
            this.UpdateBarLayout();

            if (isWindowActive)
                this.animationTimer.Start();
        }

        public void UnregisterSoundPlayer()
        {
            this.animationTimer.Stop();

            if (soundPlayer != null)
            {
                this.soundPlayer.PropertyChanged -= soundPlayer_PropertyChanged;
                this.soundPlayer = null;
            }
        }

        private void UpdateSpectrum()
        {
            if (this.soundPlayer == null || this.spectrumCanvas == null || this.spectrumCanvas.RenderSize.Width < 1 || this.spectrumCanvas.RenderSize.Height < 1)
            {
                return;
            }

            if (this.soundPlayer.IsPlaying && !this.soundPlayer.GetFFTData(ref this.channelData))
            {
                return;
            }

            this.UpdateSpectrumShapes();
        }

        private void UpdateSpectrumShapes()
        {
            try
            {
                bool allZero = true;
                double fftBucketHeight = 0f;
                double barHeight = 0f;
                double lastPeakHeight = 0f;
                double peakYPos = 0f;
                double height = spectrumCanvas.RenderSize.Height;
                int barIndex = 0;
                double barWidth = this.BarWidth;

                for (int i = this.minimumFrequencyIndex; i <= this.maximumFrequencyIndex; i++)
                {
                    // If we're paused, keep drawing, but set the current height to 0 so the peaks fall.
                    if (!this.soundPlayer.IsPlaying)
                    {
                        barHeight = 0f;
                    }
                    else // Draw the maximum value for the bar's band
                    {
                        switch (this.AnimationStyle)
                        {
                            case SpectrumAnimationStyle.Nervous:
                                // Do nothing
                                break;
                            case SpectrumAnimationStyle.Gentle:
                                this.channelData[i] -= 0.003f;
                                break;
                            default:
                                break;
                        }

                        double dbValue = 20 * Math.Log10((double)this.channelData[i]);

                        fftBucketHeight = ((dbValue - minDBValue) / dbScale) * height;

                        if (barHeight < fftBucketHeight)
                        {
                            barHeight = fftBucketHeight;
                        }

                        if (barHeight < 0f)
                        {
                            barHeight = 0f;
                        }
                    }

                    // If this is the last FFT bucket in the bar's group, draw the bar.
                    if (i == this.barIndexMax[barIndex])
                    {
                        // Peaks can't surpass the height of the control.
                        if (barHeight > height)
                        {
                            barHeight = height;
                        }

                        peakYPos = barHeight;

                        if (this.channelPeakData[barIndex] < peakYPos)
                        {
                            this.channelPeakData[barIndex] = (float)peakYPos;
                        }
                        else
                        {
                            this.channelPeakData[barIndex] = (float)(peakYPos + (this.peakFallDelay * this.channelPeakData[barIndex])) / ((float)(this.peakFallDelay + 1));
                        }

                        double xCoord = this.BarSpacing + (barWidth * barIndex) + (this.BarSpacing * barIndex) + 1;

                        switch (this.AnimationStyle)
                        {
                            case SpectrumAnimationStyle.Nervous:
                                this.barShapes[barIndex].Rect = new Rect(xCoord, (height - 1) - barHeight, barWidth, barHeight);
                                break;
                            case SpectrumAnimationStyle.Gentle:
                                this.barShapes[barIndex].Rect = new Rect(xCoord, (height - 1) - this.channelPeakData[barIndex], barWidth, this.channelPeakData[barIndex]);
                                break;
                            default:
                                break;
                        }

                        if (this.channelPeakData[barIndex] > 0.05)
                        {
                            allZero = false;
                        }

                        lastPeakHeight = barHeight;
                        barHeight = 0f;
                        barIndex++;
                    }
                }

                if (!allZero || this.soundPlayer.IsPlaying)
                {
                    return;
                }

                this.animationTimer.Stop();
            }
            catch (IndexOutOfRangeException)
            {
                // Intended suppression.
            }
        }

        private void UpdateBarLayout()
        {
            if (this.soundPlayer == null || this.spectrumCanvas == null) return;

            this.maximumFrequencyIndex = Math.Min(this.soundPlayer.GetFFTFrequencyIndex(this.maximumFrequency) + 1, 2047);
            this.minimumFrequencyIndex = Math.Min(this.soundPlayer.GetFFTFrequencyIndex(this.minimumFrequency), 2047);
            this.bandWidth = Math.Max(((double)(this.maximumFrequencyIndex - this.minimumFrequencyIndex)) / this.spectrumCanvas.RenderSize.Width, 1.0);

            int actualBarCount;

            if (this.BarWidth >= 1.0d)
            {
                actualBarCount = this.BarCount;
            }
            else
            {
                actualBarCount = Math.Max((int)((this.spectrumCanvas.RenderSize.Width - this.BarSpacing) / (this.BarWidth + this.BarSpacing)), 1);
            }

            this.channelPeakData = new float[actualBarCount];

            int indexCount = this.maximumFrequencyIndex - this.minimumFrequencyIndex;
            int linearIndexBucketSize = (int)Math.Round((double)indexCount / (double)actualBarCount, 0);
            var maxIndexList = new List<int>();
            var maxLogScaleIndexList = new List<int>();
            double maxLog = Math.Log(actualBarCount, actualBarCount);

            for (int i = 1; i < actualBarCount; i++)
            {
                maxIndexList.Add(this.minimumFrequencyIndex + (i * linearIndexBucketSize));
                int logIndex = (int)((maxLog - Math.Log((actualBarCount + 1) - i, (actualBarCount + 1))) * indexCount) + this.minimumFrequencyIndex;
                maxLogScaleIndexList.Add(logIndex);
            }

            maxIndexList.Add(this.maximumFrequencyIndex);
            maxLogScaleIndexList.Add(this.maximumFrequencyIndex);
            this.barIndexMax = maxIndexList.ToArray();
            this.barLogScaleIndexMax = maxLogScaleIndexList.ToArray();

            this.barHeights = new double[actualBarCount];

            this.spectrumCanvas.Children.Clear();
            this.barShapes.Clear();

            double height = this.spectrumCanvas.RenderSize.Height;

            barGroup = new GeometryGroup();
            barDrawing = new GeometryDrawing(this.BarBackground, null, barGroup);

            for (int i = 0; i < actualBarCount; i++)
            {
                double xCoord = this.BarSpacing + (this.BarWidth * i) + (this.BarSpacing * i) + 1;
                var barRectangle = new RectangleGeometry(new Rect(xCoord, height, this.BarWidth, 0));

                barGroup.Children.Add(barRectangle);
                this.barShapes.Add(barRectangle);
            }

            this.spectrumCanvas.Children.Add(new Image() {
                Source = new DrawingImage(barDrawing),
                Stretch = Stretch.None,
                Width = this.spectrumCanvas.RenderSize.Width,
                Height = this.spectrumCanvas.RenderSize.Height,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Left
            });
        }

        private void soundPlayer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsPlaying":
                    if (this.soundPlayer.IsPlaying && !this.animationTimer.IsEnabled && isWindowActive)
                    {
                        this.animationTimer.Start();
                    }
                    break;
            }
        }

        private void animationTimer_Tick(object sender, EventArgs e)
        {
            this.UpdateSpectrum();
        }

        private void spectrumCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdateBarLayout();
        }

        private void SpectrumAnalyzer_Loaded(object sender, RoutedEventArgs e)
        {
            if (topLevelWindow != null)
            {
                topLevelWindow.StateChanged -= TopLevelWindow_StateChanged;
                isWindowActive = false;
            }

            topLevelWindow = Window.GetWindow(this.VisualParent);

            if (topLevelWindow != null)
            {
                topLevelWindow.StateChanged += TopLevelWindow_StateChanged;
                isWindowActive = topLevelWindow.WindowState != WindowState.Minimized;
            }

            if (isWindowActive && (this.soundPlayer?.IsPlaying ?? false))
            {
                this.animationTimer.Start();
            }
        }

        private void SpectrumAnalyzer_Unloaded(object sender, RoutedEventArgs e)
        {
            this.animationTimer.Stop();

            if (topLevelWindow != null)
            {
                topLevelWindow.StateChanged -= TopLevelWindow_StateChanged;
                isWindowActive = false;
            }
            topLevelWindow = null;
        }

        private void TopLevelWindow_StateChanged(object sender, EventArgs e)
        {
            isWindowActive = topLevelWindow?.WindowState != WindowState.Minimized;

            if (!isWindowActive)
            {
                this.animationTimer.Stop();
            }
            else if (this.soundPlayer?.IsPlaying ?? false)
            {
                this.animationTimer.Start();
            }
        }
    }
}