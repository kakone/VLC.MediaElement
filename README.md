# VLC.MediaElement (Windows Runtime Component)
MediaElement clone powered by VLC

## Usage
Add [the NuGet package](https://www.nuget.org/packages/VLC.MediaElement) to your project and use it as the classic [MediaElement](https://msdn.microsoft.com/library/windows/apps/mt187272.aspx) :

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

## Download
[![NuGet](https://img.shields.io/nuget/v/VLC.MediaElement.svg)](https://www.nuget.org/packages/VLC.MediaElement)
