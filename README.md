# VLC.MediaElement 
MediaElement clone powered by VLC.

It's just a little control (a x86 and x64 Windows Runtime Component - the ARM version will be added soon), all the hard work is done by [VLC](https://code.videolan.org/videolan/vlc-winrt). So, thanks to the VLC team !

## Usage
Requires Windows 10 Anniversary Edition and matching Windows SDK.

Add [the NuGet package](https://www.nuget.org/packages/VLC.MediaElement) to your project and use it as the classic [MediaElement](https://msdn.microsoft.com/library/windows/apps/mt187272.aspx) (don't forget the Internet (Client) capability for this sample code) :

```
xmlns:vlc="using:VLC"
```
```
<vlc:MediaElement AreTransportControlsEnabled="True" HardwareAcceleration="True"
                  Source="http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_surround-fix.avi">
    <vlc:MediaElement.TransportControls>
        <vlc:MediaTransportControls ControlPanelOpacity="0.8" />
    </vlc:MediaElement.TransportControls>
</vlc:MediaElement>
```

![VLC.MediaElement screenshot](http://freemiupnp.fr/tv/VLC.MediaElement.png)

## Added properties
There is some added properties compared to the classic MediaElement :
- HardwareAcceleration : a value indicating whether the hardware acceleration must be used or not.
- DeinterlaceMode : the deinterlace mode (Bob, Mean, Linear, X, Yadif, Yadif2x, ...) - only works if HardwareAcceleration is set to false.

On VLC.MediaTransportControls :
- Content : you can add some content over the video.
- AvailableDeinterlaceModes : the deinterlace modes to show in the deinterlace menu.
- IsDeinterlaceModeButtonVisible : a value that indicates whether the deinterlace mode button must be shown or not.
- IsDeinterlaceModeButtonEnabled : a value that indicates whether the user can choose a deinterlace mode.
- ControlPanelOpacity : it's more beautiful when the control panel is not opaque :)

## Download
[![NuGet](https://img.shields.io/nuget/v/VLC.MediaElement.svg)](https://www.nuget.org/packages/VLC.MediaElement)


