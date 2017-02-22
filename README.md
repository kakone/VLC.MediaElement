# VLC.MediaElement for UWP
MediaElement clone powered by VLC.

It's just a little control (a Windows Runtime Component), all the hard work is done by [VLC](https://code.videolan.org/videolan/vlc-winrt). So, thanks to the VLC team !

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

## How To
- How to play a file using a FileOpenPicker ?

```
private const string FILE_TOKEN = "{1BBC4B94-BE33-4D79-A0CB-E5C6CDB9D107}"; // GUID in registry format
```
```
var fileOpenPicker = new FileOpenPicker();
fileOpenPicker.FileTypeFilter.Add("*");
var file = await fileOpenPicker.PickSingleFileAsync();
if (file != null)
{
    StorageApplicationPermissions.FutureAccessList.AddOrReplace(FILE_TOKEN, file);
    mediaElement.Source = $"winrt://{FILE_TOKEN}";
}
```

## Added properties
There are some added properties compared to the classic MediaElement :
- DeinterlaceMode : the deinterlace mode (Bob, Mean, Linear, X, Yadif, Yadif2x, ...) - only works if HardwareAcceleration is set to false.
- HardwareAcceleration : a value indicating whether the hardware acceleration must be used or not.
- Options : a dictionary to add some options to the media (see [VLC command-line help](https://wiki.videolan.org/VLC_command-line_help/)). This property must be defined before setting the source.

On VLC.MediaTransportControls :
- AutoHide : indicates whether the media transport controls must be hidden automatically or not.
- AvailableDeinterlaceModes : the deinterlace modes to show in the deinterlace menu.
- Content : you can add some content over the video.
- ControlPanelOpacity : it's more beautiful when the control panel is not opaque :)
- CursorAutoHide : indicates whether the mouse cursor must be hidden automatically or not.
- IsDeinterlaceModeButtonEnabled : indicates whether the user can choose a deinterlace mode.
- IsDeinterlaceModeButtonVisible : indicates whether the deinterlace mode button must be shown or not.
 
## Download
[![NuGet](https://img.shields.io/nuget/v/VLC.MediaElement.svg)](https://www.nuget.org/packages/VLC.MediaElement)


