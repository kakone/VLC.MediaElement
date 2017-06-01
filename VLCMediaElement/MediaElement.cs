using libVLCX;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation.Diagnostics;
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
    public
#if !CLASS_LIBRARY
    sealed
#endif
    class MediaElement : Control
    {
        private static SemaphoreSlim logSemaphoreSlim = new SemaphoreSlim(1, 1);

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
        /// Occurs when a login dialog box must be shown.
        /// </summary>
        public event EventHandler<LoginDialogEventArgs> ShowLoginDialog;

        /// <summary>
        /// Occurs when a dialog box must be shown.
        /// </summary>
        public event EventHandler<DialogEventArgs> ShowDialog;

        /// <summary>
        /// Occurs when the current dialog box must be cancelled.
        /// </summary>
        public event EventHandler<DeferrableEventArgs> CancelCurrentDialog;

        /// <summary>
        /// Instantiates a new instance of the MediaElement class.
        /// </summary>
        public MediaElement()
        {
            DefaultStyleKey = typeof(MediaElement);
            Unloaded += (sender, e) => { Source = null; Logger.RemoveLoggingChannel(LoggingChannel); };
        }

        private SwapChainPanel SwapChainPanel { get; set; }
        private Instance Instance { get; set; }
        private Media Media { get; set; }
        private AudioDeviceHandler AudioDevice { get; set; }
        private LoggingChannel LoggingChannel { get; set; }
        private AsyncLock SourceChangedMutex { get; } = new AsyncLock();
        private bool UpdatingPosition { get; set; }

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
        /// Gets the underlying media player
        /// </summary>
        public MediaPlayer MediaPlayer { get; private set; }

        /// <summary>
        /// Gets or sets the keystore filename
        /// </summary>
        public string KeyStoreFilename { get; set; } = "VLC_MediaElement_KeyStore";

        /// <summary>
        /// Gets or sets the log filename
        /// </summary>
        public string LogFilename { get; set; } = "VLC_MediaElement_Log.etl";

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
        public static DependencyProperty IsMutedProperty { get; } = DependencyProperty.Register("IsMuted", typeof(bool), typeof(MediaElement),
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
        /// Identifies the <see cref="Position"/> dependency property.
        /// </summary>
        public static DependencyProperty PositionProperty { get; } = DependencyProperty.Register("Position", typeof(TimeSpan), typeof(MediaElement),
            new PropertyMetadata(TimeSpan.Zero, (d, e) => ((MediaElement)d).OnPositionChanged()));
        /// <summary>
        /// Gets or sets the current position of progress through the media's playback time.
        /// </summary>
        public TimeSpan Position
        {
            get { return (TimeSpan)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Volume"/> dependency property.
        /// </summary>
        public static DependencyProperty VolumeProperty { get; } = DependencyProperty.Register("Volume", typeof(int), typeof(MediaElement),
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
        public static DependencyProperty CurrentStateProperty { get; } = DependencyProperty.Register("CurrentState", typeof(MediaElementState), typeof(MediaElement),
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
        public static DependencyProperty TransportControlsProperty { get; } = DependencyProperty.Register("TransportControls", typeof(MediaTransportControls), typeof(MediaElement),
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
        public static DependencyProperty AreTransportControlsEnabledProperty { get; } = DependencyProperty.Register("AreTransportControlsEnabled", typeof(bool), typeof(MediaElement),
            new PropertyMetadata(false, (d, e) => UpdateMediaTransportControls(d)));
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
        public static DependencyProperty ZoomProperty { get; } = DependencyProperty.Register("Zoom", typeof(bool), typeof(MediaElement),
            new PropertyMetadata(false, async (d, e) => await ((MediaElement)d).OnZoomChanged()));
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
        public static DependencyProperty DeinterlaceModeProperty { get; } = DependencyProperty.Register("DeinterlaceMode", typeof(DeinterlaceMode), typeof(MediaElement),
            new PropertyMetadata(DeinterlaceMode.Disabled, (d, e) => ((MediaElement)d).OnDeinterlaceModeChanged()));
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
        public static DependencyProperty HardwareAccelerationProperty { get; } = DependencyProperty.Register("HardwareAcceleration", typeof(bool), typeof(MediaElement),
            new PropertyMetadata(false));
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
        public static DependencyProperty SourceProperty { get; } = DependencyProperty.Register("Source", typeof(string), typeof(MediaElement),
            new PropertyMetadata(null, (d, e) => ((MediaElement)d).OnSourceChanged()));
        /// <summary>
        /// Gets or sets a media source on the MediaElement.
        /// </summary>
        public string Source
        {
            get { return GetValue(SourceProperty) as string; }
            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AutoPlay"/> dependency property.
        /// </summary>
        public static DependencyProperty AutoPlayProperty { get; } = DependencyProperty.Register("AutoPlay", typeof(bool), typeof(MediaElement),
            new PropertyMetadata(true));
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
        public static DependencyProperty PosterSourceProperty { get; } = DependencyProperty.Register("PosterSource", typeof(ImageSource), typeof(MediaElement),
            new PropertyMetadata(null));
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
        public static DependencyProperty StretchProperty { get; } = DependencyProperty.Register("Stretch", typeof(Stretch), typeof(MediaElement),
            new PropertyMetadata(Stretch.Uniform));
        /// <summary>
        /// Gets or sets a value that describes how an MediaElement should be stretched to fill the destination rectangle.
        /// </summary>
        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Options"/> dependency property.
        /// </summary>
        public static DependencyProperty OptionsProperty { get; } = DependencyProperty.Register("Options", typeof(IDictionary<string, object>), typeof(MediaElement),
            new PropertyMetadata(new Dictionary<string, object>()));
        /// <summary>
        /// Gets or sets the options for the media
        /// </summary>
        public IDictionary<string, object> Options
        {
            get { return (IDictionary<string, object>)GetValue(OptionsProperty); }
            set { SetValue(OptionsProperty, value); }
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
            swapChainPanel.CompositionScaleChanged += async (sender, e) => await UpdateScale();
            swapChainPanel.SizeChanged += async (sender, e) => await UpdateSize();

            Task.Run(() => DispatcherRunAsync(async () => await Init(swapChainPanel)));
        }

        private static void OnTransportControlsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is MediaTransportControls transportControls)
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

        private void OnPositionChanged()
        {
            if (!UpdatingPosition)
            {
                var mediaPlayer = MediaPlayer;
                if (mediaPlayer != null)
                {
                    var length = mediaPlayer.length();
                    if (length > 0)
                    {
                        mediaPlayer.setPosition((float)(Position.TotalMilliseconds / length));
                    }
                }
            }
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
            LoggingChannel = Logger.AddLoggingChannel(Name);
            var instance = new Instance(new List<string>
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
                    $"--keystore-file={Path.Combine(ApplicationData.Current.LocalFolder.Path, KeyStoreFilename)}"
                }, swapChainPanel);
            Instance = instance;
            instance.setDialogHandlers(OnError, OnShowLoginDialog, OnShowDialog,
                (dialog, title, text, intermediate, position, cancel) => { },
                OnCancelCurrentDialog,
                (dialog, position, text) => { });
            instance.logSet((param0, param1) =>
                {
                    var logLevel = (LogLevel)param0;
                    Debug.WriteLine($"[VLC {logLevel}] {param1}");
                    LoggingChannel.LogMessage(param1, logLevel.ToLoggingLevel());
                });

            await UpdateScale();
            UpdateDeinterlaceMode();

            AudioDeviceId = MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default);
            MediaDevice.DefaultAudioRenderDeviceChanged += (sender, e) => { if (e.Role == AudioDeviceRole.Default) { AudioDeviceId = e.Id; } };

            OnSourceChanged();
        }

        private async void OnError(string title, string text)
        {
            await DispatcherRunAsync(() => TransportControls?.SetError($"{title}{Environment.NewLine}{text}"));
            await logSemaphoreSlim.WaitAsync();
            try
            {
                await Logger.SaveToFileAsync(LogFilename);
            }
            catch (Exception)
            {
            }
            finally
            {
                logSemaphoreSlim.Release();
            }
        }

        private async void OnShowLoginDialog(Dialog dialog, string title, string text, string defaultUserName, bool askToStore)
        {
            LoginDialogResult dialogResult;
            var showLoginDialog = ShowLoginDialog;
            if (showLoginDialog == null)
            {
                dialogResult = null;
            }
            else
            {
                var loginEventArgs = new LoginDialogEventArgs(title, text, defaultUserName, askToStore);
                showLoginDialog(this, loginEventArgs);
                await loginEventArgs.WaitForDeferralsAsync();
                dialogResult = loginEventArgs.DialogResult;
            }

            if (dialogResult == null)
            {
                dialog.dismiss();
            }
            else
            {
                dialog.postLogin(dialogResult.Username, dialogResult.Password, dialogResult.StoreCredentials);
            }
        }

        private async void OnShowDialog(Dialog dialog, string title, string text, Question qType, string cancel, string action1, string action2)
        {
            int? selectedActionIndex;
            var showDialog = ShowDialog;
            if (showDialog == null)
            {
                selectedActionIndex = null;
            }
            else
            {
                var dialogEventArgs = new DialogEventArgs(title, text, qType, cancel, action1, action2);
                showDialog(this, dialogEventArgs);
                await dialogEventArgs.WaitForDeferralsAsync();
                selectedActionIndex = dialogEventArgs.SelectedActionIndex;
            }

            if (selectedActionIndex == null)
            {
                dialog.dismiss();
            }
            else
            {
                dialog.postAction((int)selectedActionIndex);
            }
        }

        private async void OnCancelCurrentDialog(Dialog dialog)
        {
            var cancelCurrentDialog = CancelCurrentDialog;
            if (cancelCurrentDialog != null)
            {
                var eventArgs = new DeferrableEventArgs();
                cancelCurrentDialog(this, eventArgs);
                await eventArgs.WaitForDeferralsAsync();
            }
            dialog.dismiss();
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
            await UpdateState(MediaElementState.Playing);
            await DispatcherRunAsync(() => TransportControls?.OnPositionChanged(position));
        }

        private async void EventManager_OnTimeChanged(long time)
        {
            await SetPosition(time);
            await DispatcherRunAsync(() => TransportControls?.OnTimeChanged(time));
        }

        private async Task UpdateState(MediaElementState state)
        {
            await DispatcherRunAsync(() =>
            {
                var previousState = CurrentState;
                if (previousState != state)
                {
                    CurrentState = state;
                    TransportControls?.UpdateState(previousState, state);
                }
            });
        }

        private async Task UpdateZoom()
        {
            if ((Stretch == Stretch.None || Stretch == Stretch.Uniform) && !Zoom ||
                (Stretch == Stretch.Fill || Stretch == Stretch.UniformToFill && Zoom))
            {
                var renderTransform = SwapChainPanel.RenderTransform;
                if (renderTransform != null && (!(renderTransform is MatrixTransform matrix) || matrix.Matrix != Matrix.Identity))
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

                var scaleTransform = new ScaleTransform()
                {
                    CenterX = screenWidth / 2,
                    CenterY = screenHeight / 2
                };
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
            Instance?.UpdateScale(scp.CompositionScaleX, scp.CompositionScaleY);
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

        private async Task ClearMedia()
        {
            await SetPosition(0);
            Media = null;
            MediaPlayer = null;
        }

        private async void OnSourceChanged()
        {
            if (Instance == null || DesignMode.DesignModeEnabled)
            {
                return;
            }

            using (await SourceChangedMutex.LockAsync())
            {
                Stop();
                TransportControls?.Clear();

                var source = Source;
                if (source == null)
                {
                    await ClearMedia();
                    return;
                }

                FromType type;
                if (!Uri.TryCreate(source, UriKind.RelativeOrAbsolute, out Uri location) || location.IsAbsoluteUri && !location.IsFile)
                {
                    type = FromType.FromLocation;
                }
                else
                {
                    if (!location.IsAbsoluteUri)
                    {
                        source = Path.Combine(Package.Current.InstalledLocation.Path, source);
                    }
                    type = FromType.FromPath;
                }
                var media = new Media(Instance, source, type);
                media.addOption($":avcodec-hw={(HardwareAcceleration ? "d3d11va" : "none")}");
                media.addOption($":avcodec-threads={Convert.ToInt32(HardwareAcceleration)}");
                var options = Options;
                if (options != null)
                {
                    foreach (var option in options)
                    {
                        media.addOption($":{option.Key}={option.Value}");
                    }
                }
                Media = media;

                var mediaPlayer = new MediaPlayer(media);
                var eventManager = mediaPlayer.eventManager();
                eventManager.OnBuffering += async p => await UpdateState(MediaElementState.Buffering);
                eventManager.OnOpening += async () => await UpdateState(MediaElementState.Opening);
                eventManager.OnPlaying += async () => await UpdateState(MediaElementState.Playing);
                eventManager.OnPaused += async () => await UpdateState(MediaElementState.Paused);
                eventManager.OnStopped += async () => await UpdateState(MediaElementState.Stopped);
                eventManager.OnEndReached += async () => { await ClearMedia(); await UpdateState(MediaElementState.Closed); };
                eventManager.OnPositionChanged += EventManager_OnPositionChanged;
                eventManager.OnVoutCountChanged += async p => await DispatcherRunAsync(async () => { await UpdateZoom(); });
                eventManager.OnTrackAdded += EventManager_OnTrackAdded;
                eventManager.OnTrackDeleted += async (trackType, trackId) => await DispatcherRunAsync(() => TransportControls?.OnTrackDeleted(trackType, trackId));
                eventManager.OnLengthChanged += async length => await DispatcherRunAsync(() => TransportControls?.OnLengthChanged(length));
                eventManager.OnTimeChanged += EventManager_OnTimeChanged;
                eventManager.OnSeekableChanged += async seekable => await DispatcherRunAsync(() => TransportControls?.OnSeekableChanged(seekable));
                MediaPlayer = mediaPlayer;

                SetAudioDevice();
                SetDeinterlaceMode();

                if (AutoPlay) { Play(); }
            }
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

        private async Task SetPosition(long time)
        {
            await DispatcherRunAsync(() =>
            {
                UpdatingPosition = true;
                try
                {
                    Position = TimeSpan.FromMilliseconds(time);
                }
                finally
                {
                    UpdatingPosition = false;
                }
            });
        }
    }
}