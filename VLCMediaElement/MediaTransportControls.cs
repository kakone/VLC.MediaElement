using libVLCX;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace VLC
{
    /// <summary>
    /// Represents the playback controls for a media player element.
    /// </summary>
    [ContentProperty(Name = "Content")]
    public sealed class MediaTransportControls : Control
    {
        /// <summary>
        /// Initializes a new instance of MediaTransportControls class.
        /// </summary>
        public MediaTransportControls()
        {
            DefaultStyleKey = typeof(MediaTransportControls);
        }

        private bool HasError { get; set; }
        private bool Seekable { get; set; }
        private TimeSpan Length { get; set; }
        private Point? PreviousPointerPosition { get; set; }
        private CoreCursor Cursor { get; set; }
        private DispatcherTimer Timer { get; set; } = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1.5) };
        private IDictionary<TrackType, TracksMenu> TracksMenus { get; } = new Dictionary<TrackType, TracksMenu>();

        private FrameworkElement LeftSeparator { get; set; }
        private FrameworkElement RightSeparator { get; set; }
        private FrameworkElement ControlPanelGrid { get; set; }
        private TextBlock ErrorTextBlock { get; set; }
        private Slider ProgressSlider { get; set; }
        private FrameworkElement TimeTextGrid { get; set; }
        private Slider VolumeSlider { get; set; }
        private FrameworkElement DeinterlaceModeButton { get; set; }
        private FrameworkElement PlayPauseButton { get; set; }
        private FrameworkElement PlayPauseButtonOnLeft { get; set; }
        private FrameworkElement ZoomButton { get; set; }
        private FrameworkElement FullWindowButton { get; set; }
        private FrameworkElement StopButton { get; set; }
        private TextBlock TimeElapsedTextBlock { get; set; }
        private TextBlock TimeRemainingTextBlock { get; set; }
        private MenuFlyout DeinterlaceModeMenu { get; set; }

        private ResourceLoader ResourceLoader
        {
            get
            {
                try
                {
                    return ResourceLoader.GetForCurrentView("VLC/Resources");
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        private double Position
        {
            get { return (ProgressSlider?.Value) ?? 0; }
            set
            {
                if (ProgressSlider != null && ProgressSlider.Value != value)
                {
                    ProgressSlider.ValueChanged -= ProgressSlider_ValueChanged;
                    try
                    {
                        ProgressSlider.Value = value;
                    }
                    finally
                    {
                        ProgressSlider.ValueChanged += ProgressSlider_ValueChanged;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the media element.
        /// </summary>
        internal MediaElement MediaElement { get; set; }

        /// <summary>
        /// Gets the command bar.
        /// </summary>
        public CommandBar CommandBar { get; private set; }

        /// <summary>
        /// Gets the style of the bar buttons.
        /// </summary>
        public Style AppBarButtonStyle { get; private set; }

        private IEnumerable<DeinterlaceMode> _availableDeinterlaceModes;
        /// <summary>
        /// Gets or sets the deinterlace modes to show in the menu.
        /// </summary>
        public IEnumerable<DeinterlaceMode> AvailableDeinterlaceModes
        {
            get { return _availableDeinterlaceModes ?? (_availableDeinterlaceModes = Enum.GetValues(typeof(DeinterlaceMode)).OfType<DeinterlaceMode>()); }
            set { _availableDeinterlaceModes = value; }
        }

        /// <summary>
        /// Identifies the <see cref="AutoHide"/> dependency property.
        /// </summary>
        public static DependencyProperty AutoHideProperty { get; } = DependencyProperty.Register("AutoHide", typeof(bool), typeof(MediaTransportControls),
            new PropertyMetadata(true, (d, e) => ((MediaTransportControls)d).OnAutoHidePropertyChanged()));
        /// <summary>
        /// Gets or sets a value that indicates whether the media transport controls must be hidden automatically or not.
        /// </summary>
        public bool AutoHide
        {
            get { return (bool)GetValue(AutoHideProperty); }
            set { SetValue(AutoHideProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CursorAutoHide"/> dependency property.
        /// </summary>
        public static DependencyProperty CursorAutoHideProperty { get; } = DependencyProperty.Register("CursorAutoHide", typeof(bool), typeof(MediaTransportControls),
            new PropertyMetadata(false));
        /// <summary>
        /// Gets or sets a value that indicates whether the mouse cursor must be hidden automatically or not.
        /// </summary>
        public bool CursorAutoHide
        {
            get { return (bool)GetValue(CursorAutoHideProperty); }
            set { SetValue(CursorAutoHideProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsDeinterlaceModeButtonVisible"/> dependency property.
        /// </summary>
        public static DependencyProperty IsDeinterlaceModeButtonVisibleProperty { get; } = DependencyProperty.Register("IsDeinterlaceModeButtonVisible", typeof(bool), typeof(MediaTransportControls),
            new PropertyMetadata(false, (d, e) => ((MediaTransportControls)d).UpdateDeinterlaceModeButton()));
        /// <summary>
        /// Gets or sets a value that indicates whether the deinterlace mode button is shown.
        /// </summary>
        public bool IsDeinterlaceModeButtonVisible
        {
            get { return (bool)GetValue(IsDeinterlaceModeButtonVisibleProperty); }
            set { SetValue(IsDeinterlaceModeButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsDeinterlaceModeButtonEnabled"/> dependency property
        /// </summary>
        public static DependencyProperty IsDeinterlaceModeButtonEnabledProperty { get; } = DependencyProperty.Register("IsDeinterlaceModeButtonEnabled", typeof(bool), typeof(MediaTransportControls),
            new PropertyMetadata(true, (d, e) => ((MediaTransportControls)d).UpdateDeinterlaceModeButton()));
        /// <summary>
        /// Gets or sets a value that indicates whether a user can choose a deinterlace mode.
        /// </summary>
        public bool IsDeinterlaceModeButtonEnabled
        {
            get { return (bool)GetValue(IsDeinterlaceModeButtonEnabledProperty); }
            set { SetValue(IsDeinterlaceModeButtonEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsPlayPauseButtonVisible"/> dependency property.
        /// </summary>
        public static DependencyProperty IsPlayPauseButtonVisibleProperty { get; } = DependencyProperty.Register("IsPlayPauseButtonVisible", typeof(bool), typeof(MediaTransportControls),
            new PropertyMetadata(true, (d, e) => ((MediaTransportControls)d).UpdatePlayPauseButton()));
        /// <summary>
        /// Gets or sets a value that indicates whether the play/pause button is shown.
        /// </summary>
        public bool IsPlayPauseButtonVisible
        {
            get { return (bool)GetValue(IsPlayPauseButtonVisibleProperty); }
            set { SetValue(IsPlayPauseButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsPlayPauseEnabled"/> dependency property
        /// </summary>
        public static DependencyProperty IsPlayPauseEnabledProperty { get; } = DependencyProperty.Register("IsPlayPauseEnabled", typeof(bool), typeof(MediaTransportControls),
            new PropertyMetadata(true, (d, e) => ((MediaTransportControls)d).UpdatePlayPauseButton()));
        /// <summary>
        /// Gets or sets a value that indicates whether a user can play/pause the media.
        /// </summary>
        public bool IsPlayPauseEnabled
        {
            get { return (bool)GetValue(IsPlayPauseEnabledProperty); }
            set { SetValue(IsPlayPauseEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsZoomButtonVisible"/> dependency property.
        /// </summary>
        public static DependencyProperty IsZoomButtonVisibleProperty { get; } = DependencyProperty.Register("IsZoomButtonVisible", typeof(bool), typeof(MediaTransportControls),
            new PropertyMetadata(true, (d, e) => ((MediaTransportControls)d).UpdateZoomButton()));
        /// <summary>
        /// Gets or sets a value that indicates whether the zoom button is shown.
        /// </summary>
        public bool IsZoomButtonVisible
        {
            get { return (bool)GetValue(IsZoomButtonVisibleProperty); }
            set { SetValue(IsZoomButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsZoomEnabled"/> dependency property.
        /// </summary>
        public static DependencyProperty IsZoomEnabledProperty { get; } = DependencyProperty.Register("IsZoomEnabled", typeof(bool), typeof(MediaTransportControls),
            new PropertyMetadata(true, (d, e) => ((MediaTransportControls)d).UpdateZoomButton()));
        /// <summary>
        /// Gets or sets a value that indicates whether a user can zoom the media.
        /// </summary>
        public bool IsZoomEnabled
        {
            get { return (bool)GetValue(IsZoomEnabledProperty); }
            set { SetValue(IsZoomEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsFullWindowButtonVisible"/> dependency property.
        /// </summary>
        public static DependencyProperty IsFullWindowButtonVisibleProperty { get; } = DependencyProperty.Register("IsFullWindowButtonVisible", typeof(bool), typeof(MediaTransportControls),
            new PropertyMetadata(true, (d, e) => ((MediaTransportControls)d).UpdateZoomButton()));
        /// <summary>
        /// Gets or sets a value that indicates whether the full screen button is shown.
        /// </summary>
        public bool IsFullWindowButtonVisible
        {
            get { return (bool)GetValue(IsFullWindowButtonVisibleProperty); }
            set { SetValue(IsFullWindowButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsFullWindowEnabled"/> dependency property.
        /// </summary>
        public static DependencyProperty IsFullWindowEnabledProperty { get; } = DependencyProperty.Register("IsFullWindowEnabled", typeof(bool), typeof(MediaTransportControls),
            new PropertyMetadata(true, (d, e) => ((MediaTransportControls)d).UpdateZoomButton()));
        /// <summary>
        /// Gets or sets a value that indicates whether a user can play the media in full-screen mode.
        /// </summary>
        public bool IsFullWindowEnabled
        {
            get { return (bool)GetValue(IsFullWindowEnabledProperty); }
            set { SetValue(IsFullWindowEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsStopButtonVisible"/> dependency property.
        /// </summary>
        public static DependencyProperty IsStopButtonVisibleProperty { get; } = DependencyProperty.Register("IsStopButtonVisible", typeof(bool), typeof(MediaTransportControls),
            new PropertyMetadata(false, (d, e) => ((MediaTransportControls)d).UpdateStopButton()));
        /// <summary>
        /// Gets or sets a value that indicates whether the stop button is shown.
        /// </summary>
        public bool IsStopButtonVisible
        {
            get { return (bool)GetValue(IsStopButtonVisibleProperty); }
            set { SetValue(IsStopButtonVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsStopEnabled"/> dependency property.
        /// </summary>
        public static DependencyProperty IsStopEnabledProperty { get; } = DependencyProperty.Register("IsStopEnabled", typeof(bool), typeof(MediaTransportControls),
            new PropertyMetadata(true, (d, e) => ((MediaTransportControls)d).UpdateStopButton()));
        /// <summary>
        /// Gets or sets a value that indicates whether a user can stop the media playback.
        /// </summary>
        public bool IsStopEnabled
        {
            get { return (bool)GetValue(IsStopEnabledProperty); }
            set { SetValue(IsStopEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Content"/> dependency property.
        /// </summary>
        public static DependencyProperty ContentProperty { get; } = DependencyProperty.Register("Content", typeof(object), typeof(MediaTransportControls),
            new PropertyMetadata(null));
        /// <summary>
        /// Gets or sets the content to show over the media element.
        /// </summary>
        public object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ControlPanelOpacity"/> dependency property.
        /// </summary>
        public static DependencyProperty ControlPanelOpacityProperty { get; } = DependencyProperty.Register("ControlPanelOpacity", typeof(double), typeof(MediaTransportControls),
           new PropertyMetadata(1));
        /// <summary>
        /// Gets or sets the control panel opacity.
        /// </summary>
        public double ControlPanelOpacity
        {
            get { return (double)GetValue(ControlPanelOpacityProperty); }
            set { SetValue(ControlPanelOpacityProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsCompact"/> dependency property.
        /// </summary>
        public static DependencyProperty IsCompactProperty { get; } = DependencyProperty.Register("IsCompact", typeof(bool), typeof(MediaTransportControls),
            new PropertyMetadata(false, (d, e) => ((MediaTransportControls)d).UpdateMediaTransportControlMode()));
        /// <summary>
        /// Gets or sets a value that indicates whether transport controls are shown on one row instead of two.
        /// </summary>
        public bool IsCompact
        {
            get { return (bool)GetValue(IsCompactProperty); }
            set { SetValue(IsCompactProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsSeekBarVisible"/> dependency property.
        /// </summary>
        public static DependencyProperty IsSeekBarVisibleProperty { get; } = DependencyProperty.Register("IsSeekBarVisible", typeof(bool), typeof(MediaTransportControls),
            new PropertyMetadata(true, (d, e) => ((MediaTransportControls)d).UpdateSeekBarVisibility()));
        /// <summary>
        /// Gets or sets a value that indicates whether the seek bar is shown.
        /// </summary>
        public bool IsSeekBarVisible
        {
            get { return (bool)GetValue(IsSeekBarVisibleProperty); }
            set { SetValue(IsSeekBarVisibleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsSeekBarEnabled"/> dependency property.
        /// </summary>
        public static DependencyProperty IsSeekBarEnabledProperty { get; } = DependencyProperty.Register("IsSeekBarEnabled", typeof(bool), typeof(MediaTransportControls),
            new PropertyMetadata(true, (d, e) => ((MediaTransportControls)d).UpdatePlayPauseButton()));
        /// <summary>
        /// Gets or sets a value that indicates whether a user can use the seek bar to find a location in the media.
        /// </summary>
        public bool IsSeekBarEnabled
        {
            get { return (bool)GetValue(IsSeekBarEnabledProperty); }
            set { SetValue(IsSeekBarEnabledProperty, value); }
        }

        /// <summary>
        /// Invoked whenever application code or internal processes (such as a rebuilding layout pass) call ApplyTemplate. 
        /// In simplest terms, this means the method is called just before a UI element displays in your app.
        /// Override this method to influence the default post-template logic of a class.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            LeftSeparator = GetTemplateChild("LeftSeparator") as FrameworkElement;
            RightSeparator = GetTemplateChild("RightSeparator") as FrameworkElement;

            var rootGrid = GetTemplateChild("RootGrid") as FrameworkElement;
            if (rootGrid != null)
            {
                rootGrid.DoubleTapped += Grid_DoubleTapped;
                rootGrid.PointerEntered += Grid_PointerMoved;
                rootGrid.PointerMoved += Grid_PointerMoved;
                rootGrid.Tapped += OnPointerMoved;
                rootGrid.PointerExited += Grid_PointerExited;

                AppBarButtonStyle = rootGrid.Resources["AppBarButtonStyle"] as Style;
            }

            var commandBar = GetTemplateChild("MediaControlsCommandBar") as CommandBar;
            CommandBar = commandBar;
            if (commandBar != null)
            {
                commandBar.LayoutUpdated += CommandBar_LayoutUpdated;
            }

            ControlPanelGrid = GetTemplateChild("ControlPanelGrid") as FrameworkElement;

            ErrorTextBlock = GetTemplateChild("ErrorTextBlock") as TextBlock;

            ProgressSlider = GetTemplateChild("ProgressSlider") as Slider;
            if (ProgressSlider != null)
            {
                ProgressSlider.Minimum = 0;
                ProgressSlider.Maximum = 1000;
                ProgressSlider.ValueChanged += ProgressSlider_ValueChanged;
            }
            TimeTextGrid = GetTemplateChild("TimeTextGrid") as FrameworkElement;
            VolumeSlider = GetTemplateChild("VolumeSlider") as Slider;
            if (VolumeSlider != null)
            {
                VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
                UpdateVolume();
            }
            PlayPauseButton = GetTemplateChild("PlayPauseButton") as FrameworkElement;
            PlayPauseButtonOnLeft = GetTemplateChild("PlayPauseButtonOnLeft") as FrameworkElement;
            ZoomButton = GetTemplateChild("ZoomButton") as FrameworkElement;
            FullWindowButton = GetTemplateChild("FullWindowButton") as FrameworkElement;
            StopButton = GetTemplateChild("StopButton") as FrameworkElement;
            DeinterlaceModeButton = GetTemplateChild("DeinterlaceModeButton") as FrameworkElement;

            TimeElapsedTextBlock = GetTemplateChild("TimeElapsedElement") as TextBlock;
            TimeRemainingTextBlock = GetTemplateChild("TimeRemainingElement") as TextBlock;

            var deinterlaceModeMenu = GetTemplateChild("DeinterlaceModeMenu") as MenuFlyout;
            DeinterlaceModeMenu = deinterlaceModeMenu;
            if (deinterlaceModeMenu != null)
            {
                var deinterlaceModeType = typeof(DeinterlaceMode);
                ToggleMenuFlyoutItem menuItem;
                string deinterlaceModeName;

                foreach (var deinterlaceMode in AvailableDeinterlaceModes)
                {
                    deinterlaceModeName = Enum.GetName(deinterlaceModeType, deinterlaceMode);
                    menuItem = new ToggleMenuFlyoutItem()
                    {
                        Text = ResourceLoader?.GetString($"{deinterlaceModeType.Name}_{deinterlaceModeName}") ?? deinterlaceModeName,
                        Tag = deinterlaceMode
                    };
                    menuItem.Click += DeinterlaceModeMenuItem_Click;
                    deinterlaceModeMenu.Items.Add(menuItem);
                }
            }

            AddTracksMenu("AudioTracksSelectionMenu", TrackType.Audio, "AudioSelectionAvailable", "AudioSelectionUnavailable");
            AddTracksMenu("CCSelectionSelectionMenu", TrackType.Subtitle, "CCSelectionAvailable", "CCSelectionUnavailable", true);

            SetButtonClick(PlayPauseButtonOnLeft, PlayPauseButton_Click);
            SetButtonClick(PlayPauseButton, PlayPauseButton_Click);
            SetButtonClick(ZoomButton, ZoomButton_Click);
            SetButtonClick(FullWindowButton, FullWindowButton_Click);
            SetButtonClick(StopButton, StopButton_Click);
            var audioMuteButton = GetTemplateChild("AudioMuteButton");
            SetButtonClick(audioMuteButton, AudioMuteButton_Click);

            SetToolTip(DeinterlaceModeButton, "DeinterlaceFilter");
            SetToolTip(ZoomButton, "AspectRatio");
            SetToolTip("VolumeMuteButton", "Volume");
            SetToolTip(audioMuteButton, "Mute");
            SetToolTip("CCSelectionButton", "ShowClosedCaptionMenu");
            SetToolTip("AudioTracksSelectionButton", "ShowAudioSelectionMenu");
            SetToolTip(StopButton, "Stop");
            SetToolTip(PlayPauseButton, "Play");
            SetToolTip(PlayPauseButtonOnLeft, "Play");

            UpdateMediaTransportControlMode();
            UpdateSeekBarVisibility();
            UpdatePlayPauseButton();
            UpdateZoomButton();
            UpdateFullWindowButton();
            UpdateStopButton();
            UpdateMuteState();
            UpdateDeinterlaceModeButton();
            UpdateDeinterlaceMode();

            ApplicationView.GetForCurrentView().VisibleBoundsChanged += (sender, args) => UpdateFullWindowState();
            UpdateFullWindowState();

            Timer.Tick += Timer_Tick;
        }

        private void SetToolTip(DependencyObject element, string resource)
        {
            if (element != null)
            {
                ToolTipService.SetToolTip(element, ResourceLoader?.GetString(resource));
            }
        }

        private void SetToolTip(string elementName, string resource)
        {
            SetToolTip(GetTemplateChild(elementName), resource);
        }

        private void CommandBar_LayoutUpdated(object sender, object e)
        {
            var leftSeparator = LeftSeparator;
            var rightSeparator = RightSeparator;
            if (leftSeparator == null || rightSeparator == null)
            {
                return;
            }

            var commandBar = CommandBar;
            var width = commandBar.PrimaryCommands.Where(el => !(el is AppBarSeparator) && ((FrameworkElement)el).Visibility == Visibility.Visible).Sum(el => ((FrameworkElement)el).Width);
            width = (commandBar.ActualWidth - width) / 2;
            if (width >= 0 && leftSeparator.Width != width)
            {
                leftSeparator.Width = width;
                rightSeparator.Width = width;
            }
        }

        private void AddNoneItem(TracksMenu tracksMenu)
        {
            AddTrack(tracksMenu, null, ResourceLoader?.GetString("None"));
        }

        private void AddTracksMenu(string trackMenuName, TrackType trackType, string availableStateName, string unavailableStateName, bool addNoneItem = false)
        {
            var menu = GetTemplateChild(trackMenuName) as MenuFlyout;
            if (menu != null)
            {
                var tracksMenu = new TracksMenu()
                {
                    MenuFlyout = menu,
                    AvailableStateName = availableStateName,
                    UnavailableStateName = unavailableStateName,
                    HasNoneItem = addNoneItem
                };
                TracksMenus.Add(trackType, tracksMenu);
                if (addNoneItem)
                {
                    AddNoneItem(tracksMenu);
                }
            }
        }

        private void SetButtonClick(DependencyObject dependencyObject, RoutedEventHandler eventHandler)
        {
            var button = dependencyObject as Button;
            if (button != null)
            {
                button.Click += eventHandler;
            }
        }

        private void OnAutoHidePropertyChanged()
        {
            if (AutoHide)
            {
                StartTimer();
            }
            else
            {
                Show();
            }
        }

        private void StartTimer()
        {
            if (AutoHide && MediaElement?.State == MediaState.Playing)
            {
                Timer.Start();
            }
        }

        /// <summary>
        /// Show controls with a fade in animation
        /// </summary>
        public void Show()
        {
            Timer.Stop();
            VisualStateManager.GoToState(this, "ControlPanelFadeIn", true);
            if (CursorAutoHide)
            {
                var cursor = Cursor;
                if (cursor != null && Window.Current.CoreWindow.PointerCursor != cursor)
                {
                    Window.Current.CoreWindow.PointerCursor = cursor;
                    Cursor = null;
                }
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            Timer.Stop();
            VisualStateManager.GoToState(this, "ControlPanelFadeOut", true);
            if (CursorAutoHide)
            {
                if (Cursor == null)
                {
                    Cursor = Window.Current.CoreWindow.PointerCursor;
                }
                Window.Current.CoreWindow.PointerCursor = null;
            }
        }

        private void Grid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var position = e.GetCurrentPoint((UIElement)sender).Position;
            if (PreviousPointerPosition != position)
            {
                PreviousPointerPosition = position;
                OnPointerMoved(sender, e);
            }
        }

        private void OnPointerMoved(object sender, RoutedEventArgs e)
        {
            Show();
            if (e.OriginalSource == sender || e.OriginalSource == ControlPanelGrid)
            {
                StartTimer();
            }
            else
            {
                var controlPanelGrid = ControlPanelGrid;
                if (controlPanelGrid != null && e.OriginalSource is Panel)
                {
                    var renderSize = controlPanelGrid.RenderSize;
                    var currentWindow = Window.Current;
                    var bounds = currentWindow.Bounds;
                    var pointerPosition = currentWindow.CoreWindow.PointerPosition;
                    pointerPosition = new Point(pointerPosition.X - bounds.X, pointerPosition.Y - bounds.Y);
                    var controlPanelGridPosition = controlPanelGrid.TransformToVisual(currentWindow.Content).TransformPoint(new Point(0, 0));
                    if (pointerPosition.X >= controlPanelGridPosition.X && pointerPosition.X < controlPanelGridPosition.X + renderSize.Width &&
                    pointerPosition.Y >= controlPanelGridPosition.Y && pointerPosition.Y < controlPanelGridPosition.Y + renderSize.Height)
                    {
                        StartTimer();
                    }
                }
            }
        }

        private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Show();

            var element = (UIElement)sender;
            var renderSize = element.RenderSize;
            var currentWindow = Window.Current;
            var bounds = currentWindow.Bounds;
            var pointerPosition = currentWindow.CoreWindow.PointerPosition;
            pointerPosition = new Point(pointerPosition.X - bounds.X, pointerPosition.Y - bounds.Y);
            var currentPosition = element.TransformToVisual(currentWindow.Content).TransformPoint(new Point(0, 0));
            if (pointerPosition.X < currentPosition.X || pointerPosition.X >= currentPosition.X + renderSize.Width ||
                pointerPosition.Y < currentPosition.Y || pointerPosition.Y >= currentPosition.Y + renderSize.Height)
            {
                StartTimer();
            }
        }

        private void Grid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                ToggleFullscreen();
            }
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            var state = MediaElement.State;
            switch (state)
            {
                case MediaState.Ended:
                case MediaState.Paused:
                case MediaState.Stopped:
                case MediaState.Error:
                case MediaState.NothingSpecial:
                    MediaElement.Play();
                    break;
                default:
                    MediaElement.Pause();
                    break;
            }
        }

        private void UpdateSeekBarVisibility()
        {
            UpdateControl(ProgressSlider, IsSeekBarVisible, IsSeekBarEnabled);
            UpdateControl(TimeTextGrid, IsSeekBarVisible);
        }

        private void UpdatePlayPauseButton()
        {
            UpdateControl(PlayPauseButton, IsPlayPauseButtonVisible, IsPlayPauseEnabled);
            UpdateControl(PlayPauseButtonOnLeft, IsPlayPauseButtonVisible, IsPlayPauseEnabled);
        }

        private void UpdateDeinterlaceModeButton()
        {
            UpdateControl(DeinterlaceModeButton, IsDeinterlaceModeButtonVisible, IsDeinterlaceModeButtonEnabled);
        }

        /// <summary>
        /// Updates zoom button properties.
        /// </summary>
        internal void UpdateZoomButton()
        {
            UpdateControl(ZoomButton, IsZoomButtonVisible, IsZoomEnabled);
        }

        private void UpdateFullWindowButton()
        {
            UpdateControl(FullWindowButton, IsFullWindowButtonVisible, IsFullWindowEnabled);
        }

        private void UpdateStopButton()
        {
            UpdateControl(StopButton, IsStopButtonVisible, IsStopEnabled);
        }

        private void UpdateControl(FrameworkElement control, bool visible, bool enabled = true)
        {
            if (control != null)
            {
                control.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                if (control is Control)
                {
                    ((Control)control).IsEnabled = enabled;
                }
            }
        }

        private void AudioMuteButton_Click(object sender, RoutedEventArgs e)
        {
            MediaElement.IsMuted = !MediaElement.IsMuted;
        }

        private void ZoomButton_Click(object sender, RoutedEventArgs e)
        {
            MediaElement.Zoom = !MediaElement.Zoom;
        }

        private void DeinterlaceModeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MediaElement.DeinterlaceMode = (DeinterlaceMode)((MenuFlyoutItemBase)sender).Tag;
        }

        /// <summary>
        /// Resets the control.
        /// </summary>
        internal void Clear()
        {
            Length = TimeSpan.Zero;
            UpdateTime();
            Position = 0;
            ProgressSlider.IsEnabled = false;
            Seekable = true;
            UpdateProgressSliderIsEnabled(MediaState.Stopped);

            foreach (var tracksMenukeyValuePair in TracksMenus)
            {
                var tracksMenu = tracksMenukeyValuePair.Value;
                tracksMenu.MenuFlyout.Items.Clear();
                if (tracksMenu.HasNoneItem)
                {
                    AddNoneItem(tracksMenu);
                }
                VisualStateManager.GoToState(this, tracksMenu.UnavailableStateName, true);
            }
        }

        /// <summary>
        /// Checks the correct deinterlace mode.
        /// </summary>
        internal void UpdateDeinterlaceMode()
        {
            var deinterlaceMenu = DeinterlaceModeMenu;
            if (deinterlaceMenu == null)
            {
                return;
            }

            var deinterlaceMode = MediaElement?.DeinterlaceMode;
            foreach (var item in deinterlaceMenu.Items)
            {
                if (item is ToggleMenuFlyoutItem)
                {
                    ((ToggleMenuFlyoutItem)item).IsChecked = ((DeinterlaceMode)(item.Tag) == deinterlaceMode);
                }
            }
        }

        /// <summary>
        /// Updates the mute state.
        /// </summary>
        internal void UpdateMuteState()
        {
            VisualStateManager.GoToState(this, MediaElement?.IsMuted == true ? "MuteState" : "VolumeState", true);
        }

        private TracksMenu GetTracksMenu(TrackType trackType)
        {
            TracksMenu tracksMenu;
            if (TracksMenus.TryGetValue(trackType, out tracksMenu))
            {
                return tracksMenu;
            }
            return null;
        }

        /// <summary>
        /// Called when a track (audio or subtitle) is added.
        /// </summary>
        /// <param name="trackType">track type</param>
        /// <param name="trackId">track identifier</param>
        /// <param name="trackName">track name</param>
        internal void OnTrackAdded(TrackType trackType, int trackId, string trackName)
        {
            AddTrack(GetTracksMenu(trackType), trackId, trackName, trackType == TrackType.Subtitle);
        }

        private void AddTrack(TracksMenu tracksMenu, int? trackId, string trackName, bool subTitle = false)
        {
            if (tracksMenu == null)
            {
                return;
            }

            var menuItem = new ToggleMenuFlyoutItem()
            {
                Text = trackName,
                Tag = trackId
            };
            menuItem.Click += TrackMenuItem_Click;
            var menuItems = tracksMenu.MenuFlyout.Items;
            menuItems.Add(menuItem);

            switch (menuItems.Count)
            {
                case 2:
                    var firstMenuItem = (menuItems.FirstOrDefault() as ToggleMenuFlyoutItem);
                    if (subTitle || firstMenuItem?.Tag != null)
                    {
                        firstMenuItem.IsChecked = true;
                    }
                    else
                    {
                        menuItem.IsChecked = true;
                    }
                    VisualStateManager.GoToState(this, tracksMenu.AvailableStateName, true);
                    break;
            }
        }

        private void TrackMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menu = TracksMenus.FirstOrDefault(kvp => kvp.Value.MenuFlyout.Items.Contains(sender));
            if (!menu.Equals(default(KeyValuePair<TrackType, TracksMenu>)))
            {
                foreach (var item in menu.Value.MenuFlyout.Items)
                {
                    if (item is ToggleMenuFlyoutItem)
                    {
                        ((ToggleMenuFlyoutItem)item).IsChecked = (item == sender);
                    }
                }

                MediaElement.SetTrack(menu.Key, (int?)((MenuFlyoutItemBase)sender).Tag);
            }
        }

        /// <summary>
        /// Called when a track (audio or subtitle) is deleted.
        /// </summary>
        /// <param name="trackType">track type</param>
        /// <param name="trackId">track identifier</param>
        internal void OnTrackDeleted(TrackType trackType, int trackId)
        {
            var tracksMenu = GetTracksMenu(trackType);
            if (tracksMenu == null)
            {
                return;
            }

            var menuItems = tracksMenu.MenuFlyout.Items;
            var menuItem = menuItems.FirstOrDefault(mi => trackId.Equals(mi.Tag));
            if (menuItem != null)
            {
                var isChecked = (menuItem is ToggleMenuFlyoutItem && ((ToggleMenuFlyoutItem)menuItem).IsChecked);
                menuItems.Remove(menuItem);
                if (isChecked)
                {
                    var firstMenuItem = menuItems.FirstOrDefault();
                    if (firstMenuItem is ToggleMenuFlyoutItem)
                    {
                        ((ToggleMenuFlyoutItem)firstMenuItem).IsChecked = true;
                    }
                }
                if (menuItems.Count < 2)
                {
                    VisualStateManager.GoToState(this, tracksMenu.UnavailableStateName, true);
                }
            }
        }

        /// <summary>
        /// Called when the media length has changed.
        /// </summary>
        /// <param name="length">media length</param>
        internal void OnLengthChanged(long length)
        {
            Length = TimeSpan.FromMilliseconds(length);
            UpdateTime();
        }

        /// <summary>
        /// Called when the current position of progress through the media's playback time has changed.
        /// </summary>
        /// <param name="time">current position of progress</param>
        internal void OnTimeChanged(long time)
        {
            UpdateTime();
        }

        private void UpdateTime()
        {
            var time = MediaElement.Position;
            var timeElapsed = time.ToShortString();
            var timeRemaining = (Length - time).ToShortString();
            if (TimeElapsedTextBlock?.Text != timeElapsed)
            {
                TimeElapsedTextBlock.Text = timeElapsed;
            }
            if (TimeRemainingTextBlock?.Text != timeRemaining)
            {
                TimeRemainingTextBlock.Text = timeRemaining;
            }
        }

        private void ProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            MediaElement.SetPosition((float)(e.NewValue / ProgressSlider.Maximum));
        }

        /// <summary>
        /// Called when the seekable property of the media has changed.
        /// </summary>
        /// <param name="seekable">true if the media is seekable, false otherwise.</param>
        internal void OnSeekableChanged(bool seekable)
        {
            Seekable = seekable;
            UpdateProgressSliderIsEnabled();
        }

        private void UpdateProgressSliderIsEnabled(MediaState? state = null)
        {
            if (!Seekable)
            {
                ProgressSlider.IsEnabled = false;
            }

            var currentState = state ?? MediaElement.State;
            ProgressSlider.IsEnabled = currentState != MediaState.Ended && currentState != MediaState.Stopped && currentState != MediaState.Error;
        }

        private void UpdateMediaTransportControlMode()
        {
            VisualStateManager.GoToState(this, IsCompact ? "CompactMode" : "NormalMode", true);
        }

        private void UpdateFullWindowState()
        {
            var fullScreen = ApplicationView.GetForCurrentView().IsFullScreenMode;
            SetToolTip(FullWindowButton, fullScreen ? "ExitFullScreen" : "FullScreen");
            VisualStateManager.GoToState(this, fullScreen ? "FullWindowState" : "NonFullWindowState", true);
        }

        private void FullWindowButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullscreen();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            MediaElement?.Stop();
        }

        private void ToggleFullscreen()
        {
            MediaElement?.ToggleFullscreen();
            Show();
            StartTimer();
        }

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            MediaElement.Volume = (int)e.NewValue;
        }

        /// <summary>
        /// Updates the volume slider
        /// </summary>
        internal void UpdateVolume()
        {
            VolumeSlider.Value = MediaElement.Volume;
        }

        /// <summary>
        /// Updates the state of the media
        /// </summary>
        /// <param name="previousState">previous state of the media element</param>
        /// <param name="state">state of the media element</param>
        internal void UpdateState(MediaElementState previousState, MediaElementState state)
        {
            string statusStateName, playPauseStateName, playPauseToolTip;
            switch (state)
            {
                case MediaElementState.Closed:
                case MediaElementState.Stopped:
                    statusStateName = (HasError ? null : "Disabled");
                    playPauseStateName = "PlayState";
                    playPauseToolTip = "Play";
                    Clear();
                    break;
                case MediaElementState.Paused:
                    statusStateName = "Disabled";
                    playPauseStateName = "PlayState";
                    playPauseToolTip = "Play";
                    break;
                case MediaElementState.Buffering:
                    statusStateName = (previousState == MediaElementState.Playing || previousState == MediaElementState.Opening ? "Buffering" : null);
                    playPauseStateName = null;
                    playPauseToolTip = null;
                    break;
                default:
                    statusStateName = "Normal";
                    playPauseStateName = "PauseState";
                    playPauseToolTip = "Pause";
                    break;
            }
            if (statusStateName != null)
            {
                HasError = false;
                VisualStateManager.GoToState(this, statusStateName, true);
            }
            if (playPauseStateName != null)
            {
                SetToolTip(PlayPauseButton, playPauseToolTip);
                SetToolTip(PlayPauseButtonOnLeft, playPauseToolTip);
                VisualStateManager.GoToState(this, playPauseStateName, true);
            }

            Show();
            StartTimer();
        }

        /// <summary>
        /// Sets the error message
        /// </summary>
        /// <param name="error">error message</param>
        internal void SetError(string error)
        {
            if (ErrorTextBlock != null)
            {
                ErrorTextBlock.Text = error;
                if (!String.IsNullOrWhiteSpace(error))
                {
                    VisualStateManager.GoToState(this, "Error", true);
                    HasError = true;
                }
            }
        }

        /// <summary>
        /// Called when the current position of progress has changed.
        /// </summary>
        /// <param name="position">current position of progress</param>
        internal void OnPositionChanged(float position)
        {
            if (ProgressSlider != null)
            {
                Position = position * ProgressSlider.Maximum;
            }
        }
    }
}
