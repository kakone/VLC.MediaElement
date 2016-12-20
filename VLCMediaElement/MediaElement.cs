using libVLCX;
using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Media.Devices;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace VLC
{
    /// <summary>
    /// Represents a control that contains audio and/or video.
    /// </summary>
    public sealed class MediaElement : Control
    {
        /// <summary>
        /// Occurs when zoom has changed.
        /// </summary>
        public event RoutedEventHandler ZoomChanged;

        /// <summary>
        /// Occurs when deinterlace mode has changed.
        /// </summary>
        public event RoutedEventHandler DeinterlaceModeChanged;

        /// <summary>
        /// Occurs when the current state has changed.
        /// </summary>
        public event RoutedEventHandler CurrentStateChanged;

        /// <summary>
        /// Instantiates a new instance of the MediaElement class.
        /// </summary>
        public MediaElement()
        {
            DefaultStyleKey = typeof(MediaElement);
        }

        private SwapChainPanel SwapChainPanel { get; set; }
        private Instance Instance { get; set; }
        private MediaPlayer MediaPlayer { get; set; }
        private Media Media { get; set; }
        private AudioDeviceHandler AudioDevice { get; set; }

        private string _audioDeviceId;
        private string AudioDeviceId
        {
            get { return _audioDeviceId; }
            set
            {
                if (_audioDeviceId != value)
                {
                    _audioDeviceId = value;
                    SetAudioDevice();
                }
            }
        }

        /// <summary>
        /// Gets the current state.
        /// </summary>
        internal MediaState State
        {
            get { return MediaPlayer?.state() ?? MediaState.NothingSpecial; }
        }

        /// <summary>
        /// Identifies the <see cref="IsMuted"/> dependency property.
        /// </summary>
        internal static readonly DependencyProperty IsMutedProperty = DependencyProperty.Register("IsMuted", typeof(bool), typeof(MediaElement),
           new PropertyMetadata(false, (d, e) => ((MediaElement)d).OnIsMutedChanged()));
        /// <summary>
        /// Gets or sets a value indicating whether the audio is muted.
        /// </summary>
        public bool IsMuted
        {
            get { return (bool)GetValue(IsMutedProperty); }
            set { SetValue(IsMutedProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Volume"/> dependency property.
        /// </summary>
        internal static readonly DependencyProperty VolumeProperty = DependencyProperty.Register("Volume", typeof(int), typeof(MediaElement),
           new PropertyMetadata(100, (d, e) => ((MediaElement)d).OnVolumeChanged()));
        /// <summary>
        /// Gets or sets the media's volume.
        /// </summary>
        public int Volume
        {
            get { return (int)GetValue(VolumeProperty); }
            set { SetValue(VolumeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CurrentState"/> dependency property.
        /// </summary>
        internal static readonly DependencyProperty CurrentStateProperty = DependencyProperty.Register("CurrentState", typeof(MediaElementState), typeof(MediaElement),
           new PropertyMetadata(MediaElementState.Closed, (d, e) => ((MediaElement)d).OnCurrentStateChanged()));
        /// <summary>
        /// Gets the current state.
        /// </summary>
        public MediaElementState CurrentState
        {
            get { return (MediaElementState)GetValue(CurrentStateProperty); }
            private set { SetValue(CurrentStateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TransportControls"/> dependency property.
        /// </summary>
        internal static readonly DependencyProperty TransportControlsProperty = DependencyProperty.Register("TransportControls", typeof(MediaTransportControls), typeof(MediaElement),
            new PropertyMetadata(null, OnTransportControlsPropertyChanged));
        /// <summary>
        /// Gets or sets the transport controls for the media.
        /// </summary>
        public MediaTransportControls TransportControls
        {
            get { return (MediaTransportControls)GetValue(TransportControlsProperty); }
            set { SetValue(TransportControlsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AreTransportControlsEnabled"/> dependency property.
        /// </summary>
        internal static readonly DependencyProperty AreTransportControlsEnabledProperty = DependencyProperty.Register("AreTransportControlsEnabled", typeof(bool), typeof(MediaElement), new PropertyMetadata(false, (d, e) => UpdateMediaTransportControls(d)));
        /// <summary>
        /// Gets or sets a value that determines whether the standard transport controls are enabled.
        /// </summary>
        public bool AreTransportControlsEnabled
        {
            get { return (bool)GetValue(AreTransportControlsEnabledProperty); }
            set { SetValue(AreTransportControlsEnabledProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Zoom"/> dependency property.
        /// </summary>
        internal static readonly DependencyProperty ZoomProperty = DependencyProperty.Register("Zoom", typeof(bool), typeof(MediaElement), new PropertyMetadata(false, async (d, e) => await ((MediaElement)d).OnZoomChanged()));
        /// <summary>
        /// Gets or sets a value indicating whether the video is zoomed.
        /// </summary>
        public bool Zoom
        {
            get { return (bool)GetValue(ZoomProperty); }
            set { SetValue(ZoomProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="DeinterlaceMode"/> dependency property.
        /// </summary>
        internal static readonly DependencyProperty DeinterlaceModeProperty = DependencyProperty.Register("DeinterlaceMode", typeof(DeinterlaceMode), typeof(MediaElement), new PropertyMetadata(DeinterlaceMode.Disabled, (d, e) => ((MediaElement)d).OnDeinterlaceModeChanged()));
        /// <summary>
        /// Gets or sets the deinterlace mode.
        /// </summary>
        public DeinterlaceMode DeinterlaceMode
        {
            get { return (DeinterlaceMode)GetValue(DeinterlaceModeProperty); }
            set { SetValue(DeinterlaceModeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="HardwareAcceleration"/> dependency property.
        /// </summary>
        internal static readonly DependencyProperty HardwareAccelerationProperty = DependencyProperty.Register("HardwareAcceleration", typeof(bool), typeof(MediaElement), new PropertyMetadata(false));
        /// <summary>
        /// Gets or sets a value indicating whether the hardware acceleration must be used.
        /// </summary>
        public bool HardwareAcceleration
        {
            get { return (bool)GetValue(HardwareAccelerationProperty); }
            set { SetValue(HardwareAccelerationProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Source"/> dependency property.
        /// </summary>
        internal static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(Uri), typeof(MediaElement), new PropertyMetadata(null, (d, e) => ((MediaElement)d).OnSourceChanged()));
        /// <summary>
        /// Gets or sets a media source on the MediaElement.
        /// </summary>
        public Uri Source
        {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AutoPlay"/> dependency property.
        /// </summary>
        internal static readonly DependencyProperty AutoPlayProperty = DependencyProperty.Register("AutoPlay", typeof(bool), typeof(MediaElement), new PropertyMetadata(true));
        /// <summary>
        /// Gets or sets a value that indicates whether media will begin playback automatically when the <see cref="Source"/> property is set.
        /// </summary>
        public bool AutoPlay
        {
            get { return (bool)GetValue(AutoPlayProperty); }
            set { SetValue(AutoPlayProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PosterSource"/> dependency property.
        /// </summary>
        internal static readonly DependencyProperty PosterSourceProperty = DependencyProperty.Register("PosterSource", typeof(ImageSource), typeof(MediaElement), new PropertyMetadata(null));
        /// <summary>
        /// Gets or sets the image source that is used for a placeholder image during MediaElement loading transition states.
        /// </summary>
        public ImageSource PosterSource
        {
            get { return (ImageSource)GetValue(PosterSourceProperty); }
            set { SetValue(PosterSourceProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Stretch"/> dependency property.
        /// </summary>
        internal static readonly DependencyProperty StretchProperty = DependencyProperty.Register("Stretch", typeof(Stretch), typeof(MediaElement), new PropertyMetadata(Stretch.Uniform));
        /// <summary>
        /// Gets or sets a value that describes how an MediaElement should be stretched to fill the destination rectangle.
        /// </summary>
        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        /// <summary>
        /// Invoked whenever application code or internal processes (such as a rebuildinglayout pass) call ApplyTemplate. 
        /// In simplest terms, this means the method is called just before a UI element displays in your app.
        /// Override this method to influence the default post-template logic of a class.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var swapChainPanel = (SwapChainPanel)GetTemplateChild("SwapChainPanel");
            SwapChainPanel = swapChainPanel;
            swapChainPanel.Loaded += async (sender, e) => await Init(swapChainPanel);
            swapChainPanel.CompositionScaleChanged += async (sender, e) => await UpdateScale();
            swapChainPanel.SizeChanged += async (sender, e) => await UpdateSize();
        }

        private static void OnTransportControlsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var transportControls = e.NewValue as MediaTransportControls;
            if (transportControls != null)
            {
                transportControls.MediaElement = ((MediaElement)d);
            }
        }

        private static void UpdateMediaTransportControls(DependencyObject d)
        {
            var mediaElement = (MediaElement)d;
            if (!mediaElement.AreTransportControlsEnabled)
            {
                return;
            }

            var mediaTransportControls = mediaElement.TransportControls;
            if (mediaTransportControls == null)
            {
                mediaElement.TransportControls = new MediaTransportControls();
            }
        }

        private void OnIsMutedChanged()
        {
            MediaPlayer?.setVolume(IsMuted ? 0 : Volume);
            TransportControls?.UpdateMuteState();
        }

        private void OnVolumeChanged()
        {
            if (!IsMuted)
            {
                MediaPlayer?.setVolume(Volume);
            }
            TransportControls?.UpdateVolume();
        }

        private void OnCurrentStateChanged()
        {
            CurrentStateChanged?.Invoke(this, new RoutedEventArgs());
        }

        private async Task OnZoomChanged()
        {
            ZoomChanged?.Invoke(this, new RoutedEventArgs());
            await UpdateZoom();
            TransportControls?.UpdateZoomButton();
        }

        private void OnDeinterlaceModeChanged()
        {
            DeinterlaceModeChanged?.Invoke(this, new RoutedEventArgs());
            UpdateDeinterlaceMode();
        }

        private void SetDeinterlaceMode()
        {
            MediaPlayer?.setDeinterlace(Enum.GetName(typeof(DeinterlaceMode), DeinterlaceMode).ToLowerInvariant());
        }

        private void UpdateDeinterlaceMode()
        {
            SetDeinterlaceMode();
            TransportControls?.UpdateDeinterlaceMode();
        }

        private async Task DispatcherRunAsync(DispatchedHandler agileCallback)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, agileCallback);
        }

        private async Task Init(SwapChainPanel swapChainPanel)
        {
            Instance = new Instance(new List<string>
                {
                    "-I",
                    "dummy",
                    "--no-osd",
                    "--verbose=3",
                    "--no-stats",
                    "--avcodec-fast",
                    "--subsdec-encoding",
                    "",
                    "--aout=winstore",
                    string.Format("--keystore-file={0}\\keystore", ApplicationData.Current.LocalFolder.Path),
                }, swapChainPanel);
#if DEBUG
            Instance.logSet((param0, param1) => Debug.WriteLine($"[VLC {(LogLevel)param0}] {param1}"));
#endif
            await UpdateScale();
            UpdateDeinterlaceMode();

            AudioDeviceId = MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default);
            MediaDevice.DefaultAudioRenderDeviceChanged += (sender, e) => { if (e.Role == AudioDeviceRole.Default) { AudioDeviceId = e.Id; } };

            OnSourceChanged();
        }

        private async void EventManager_OnTrackAdded(TrackType trackType, int trackId)
        {
            IList<TrackDescription> source;
            switch (trackType)
            {
                case TrackType.Audio:
                    source = MediaPlayer.audioTrackDescription();
                    break;
                case TrackType.Subtitle:
                    source = MediaPlayer.spuDescription();
                    break;
                default:
                    return;
            }

            var trackName = source?.FirstOrDefault(td => td.id() == trackId)?.name();
            if (!String.IsNullOrWhiteSpace(trackName))
            {
                await DispatcherRunAsync(() => TransportControls?.OnTrackAdded(trackType, trackId, trackName));
            }
        }

        private async void EventManager_OnPositionChanged(float position)
        {
            await DispatcherRunAsync(() =>
            {
                TransportControls?.OnPositionChanged(position);
            });
        }

        private async Task UpdateState(MediaElementState state)
        {
            await DispatcherRunAsync(() =>
            {
                if (CurrentState != state)
                {
                    CurrentState = state;
                    TransportControls?.UpdateState(state);
                }
            });
        }

        private async Task UpdateZoom()
        {
            if ((Stretch == Stretch.None || Stretch == Stretch.Uniform) && !Zoom ||
                (Stretch == Stretch.Fill || Stretch == Stretch.UniformToFill && Zoom))
            {
                if (SwapChainPanel.RenderTransform != null)
                {
                    SwapChainPanel.RenderTransform = null;
                }
            }
            else
            {
                MediaTrack videoTrack;
                try
                {
                    videoTrack = Media?.tracks()?.FirstOrDefault(x => x.type() == TrackType.Video);
                }
                catch (Exception)
                {
                    videoTrack = null;
                }
                if (videoTrack == null)
                    return;

                var videoWidth = videoTrack.width();
                var videoHeight = videoTrack.height();
                if (videoWidth == 0 || videoHeight == 0)
                {
                    await Task.Delay(500);
                    await UpdateZoom();
                    return;
                }
                var swapChainPanel = SwapChainPanel;
                var screenWidth = swapChainPanel.ActualWidth;
                var screenHeight = swapChainPanel.ActualHeight;
                var sarDen = videoTrack.sarDen();
                var sarNum = videoTrack.sarNum();
                var var = (sarDen == sarNum ? (double)videoWidth / videoHeight : ((double)videoWidth * sarNum / sarDen) / videoHeight);
                var screenar = screenWidth / screenHeight;

                var scaleTransform = new ScaleTransform();
                scaleTransform.CenterX = screenWidth / 2;
                scaleTransform.CenterY = screenHeight / 2;
                var scale = (var > screenar ? var / screenar : screenar / var);
                scaleTransform.ScaleX = scale;
                scaleTransform.ScaleY = scale;
                var renderTransform = swapChainPanel.RenderTransform as ScaleTransform;
                if (renderTransform == null || renderTransform.CenterX != scaleTransform.CenterX || renderTransform.CenterY != scaleTransform.CenterY ||
                    renderTransform.ScaleX != renderTransform.ScaleX || renderTransform.ScaleY != renderTransform.ScaleY)
                {
                    swapChainPanel.RenderTransform = scaleTransform;
                }
            }
        }

        private async Task UpdateSize()
        {
            var scp = SwapChainPanel;
            Instance?.UpdateSize((float)(scp.ActualWidth * scp.CompositionScaleX), (float)(scp.ActualHeight * scp.CompositionScaleY));
            await UpdateZoom();
        }

        private async Task UpdateScale()
        {
            var scp = SwapChainPanel;
            Instance.UpdateScale(scp.CompositionScaleX, scp.CompositionScaleY);
            await UpdateSize();
        }

        private void SetAudioDevice()
        {
            var mediaPlayer = MediaPlayer;
            if (mediaPlayer != null)
            {
                AudioDevice = new AudioDeviceHandler(AudioDeviceId);
                mediaPlayer.outputDeviceSet(AudioDevice.audioClient());
            }
        }

        private void ClearMedia()
        {
            Media = null;
            MediaPlayer = null;
        }

        private void OnSourceChanged()
        {
            if (Instance == null || DesignMode.DesignModeEnabled)
            {
                return;
            }

            Stop();
            TransportControls?.Clear();

            var source = Source;
            if (source == null)
            {
                ClearMedia();
                return;
            }

            var media = new Media(Instance, source.IsAbsoluteUri ? source.AbsoluteUri : Path.Combine(Package.Current.InstalledLocation.Path, source.AbsoluteUri), source.IsFile ? FromType.FromPath : FromType.FromLocation);
            media.addOption($":avcodec-hw={(HardwareAcceleration ? "d3d11va" : "none")}");
            media.addOption($":avcodec-threads={Convert.ToInt32(HardwareAcceleration)}");
            Media = media;

            var mediaPlayer = new MediaPlayer(media);
            var eventManager = mediaPlayer.eventManager();
            eventManager.OnBuffering += async p => await UpdateState(MediaElementState.Buffering);
            eventManager.OnOpening += async () => await UpdateState(MediaElementState.Opening);
            eventManager.OnPlaying += async () => await UpdateState(MediaElementState.Playing);
            eventManager.OnPaused += async () => await UpdateState(MediaElementState.Paused);
            eventManager.OnStopped += async () => await UpdateState(MediaElementState.Stopped);
            eventManager.OnEndReached += async () => { ClearMedia(); await UpdateState(MediaElementState.Closed); };
            eventManager.OnPositionChanged += async m => await UpdateState(MediaElementState.Playing);
            eventManager.OnVoutCountChanged += async p => await DispatcherRunAsync(async () => { await UpdateZoom(); });
            eventManager.OnTrackAdded += EventManager_OnTrackAdded;
            eventManager.OnTrackDeleted += async (trackType, trackId) => await DispatcherRunAsync(() => TransportControls?.OnTrackDeleted(trackType, trackId));
            eventManager.OnLengthChanged += async length => await DispatcherRunAsync(() => TransportControls?.OnLengthChanged(length));
            eventManager.OnPositionChanged += EventManager_OnPositionChanged;
            eventManager.OnTimeChanged += async time => await DispatcherRunAsync(() => TransportControls?.OnTimeChanged(time));
            eventManager.OnSeekableChanged += async seekable => await DispatcherRunAsync(() => TransportControls?.OnSeekableChanged(seekable));
            MediaPlayer = mediaPlayer;

            SetAudioDevice();
            SetDeinterlaceMode();

            if (AutoPlay) { Play(); }
        }

        /// <summary>
        /// Switches the application between windowed and full-screen modes.
        /// </summary>
        public void ToggleFullscreen()
        {
            var v = ApplicationView.GetForCurrentView();
            if (v.IsFullScreenMode)
            {
                v.ExitFullScreenMode();
            }
            else
            {
                v.TryEnterFullScreenMode();
            }
        }

        /// <summary>
        /// Pauses media at the current position.
        /// </summary>
        public void Pause()
        {
            MediaPlayer?.pause();
        }

        /// <summary>
        /// Plays media from the current position.
        /// </summary>
        public void Play()
        {
            if (Media == null)
            {
                OnSourceChanged();
                if (!AutoPlay)
                {
                    MediaPlayer?.play();
                }
            }
            else
            {
                MediaPlayer?.play();
            }
        }

        /// <summary>
        /// Stops and resets media to be played from the beginning.
        /// </summary>
        public void Stop()
        {
            MediaPlayer?.stop();
        }

        /// <summary>
        /// Sets audio or subtitle track
        /// </summary>
        /// <param name="trackType">track type</param>
        /// <param name="trackId">track identifier</param>
        internal void SetTrack(TrackType trackType, int? trackId)
        {
            switch (trackType)
            {
                case TrackType.Audio:
                    MediaPlayer.setAudioTrack(trackId ?? -1);
                    break;
                case TrackType.Subtitle:
                    MediaPlayer.setSpu(trackId ?? -1);
                    break;
            }
        }

        /// <summary>
        /// Sets the current position of progress
        /// </summary>
        /// <param name="position">position</param>
        internal void SetPosition(float position)
        {
            MediaPlayer?.setPosition(position);
        }
    }
}