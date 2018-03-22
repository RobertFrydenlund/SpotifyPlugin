# SpotifyPlugin

Spotify plugin for [Rainmeter](http://rainmeter.net/). Forum discussion can be found [here](http://rainmeter.net/forum/viewtopic.php?f=18&t=17077/).

## Example
```ini
[MeasureCover]
Measure=Plugin
Plugin=SpotifyPlugin
Type=AlbumArt
Res=300
DefaultPath=#@#Default.png
CoverPath=#@#Cover.png

[MeasureProgress]
Measure=Plugin
Plugin=SpotifyPlugin
Type=Progress

 [MeterCover]
 Meter=Image
 ImageName=[MeasureCover]
 LeftMouseUpAction=[!CommandMeasure "MeasureProgress" "PlayPause"]
 X=0
 Y=0
 W=300
 H=300
 DynamicVariables=1
```


## Offline API
|Measure	|Description						| Alias
|-----------|-----------------------------------|------|
|TrackName	|Returns track name					| Track
|AlbumName	|Returns album name					| Album
|ArtistName	|Returns artist name				| Artist
|TrackURI	|Returns spotify URI for the track
|AlbumURI	|Returns spotify URI for the album
|ArtistURI	|Returns spotify URI for the artist
|AlbumArt	|Path to album image				| Cover
|volume|Current volume
|repeat|1 if enabled
|shuffle|1 if enabled
|position|Current position
|playing|1 if playing
|length|Song length| duration
|progress|Song progress (0.0-1.0)
---

## Online API
|Command	| Description 					|Argument|
|-----------|------------------------		|--------------------
|playpause	|Pauses or resumes playback		|
|play		|Starts playback				|
|pause		|Pauses playback				|
|next		|Next song						|
|previous	|Previous song					|
|volume		|Changes active device volume	|```0``` to ```100```
|seek		|Seek to positon (ms)			|```0``` to *```length```*
|seekpercent *or* setposition|Seek to position (percent)		|```0``` to ```100```
|shuffle *or* setshuffle|Change shuffle state					|```0``` or ```false```, ```1``` or ```true``` , ```-1``` to toggle.
|repeat	*or* setrepeat	|Change repeat					|```0``` or ```off```, ```1``` or ```track```, ```2``` or ```context```, ```-1``` to toggle.
