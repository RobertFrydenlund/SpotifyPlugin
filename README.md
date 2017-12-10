# SpotifyPlugin

Spotify plugin for [Rainmeter](http://rainmeter.net/). Forum discussion can be found [here](http://rainmeter.net/forum/viewtopic.php?f=18&t=17077/).



|Measure	|Description						| Alias
|-----------|-----------------------------------|------|
|TrackName	|Returns track name					| Track
|AlbumName	|Returns album name					| Album
|ArtistName	|Returns artist name				| Artist
|TrackURI	|Returns spotify URI for the track
|AlbumURI	|Returns spotify URI for the album
|ArtistURI	|Returns spotify URI for the artist
|AlbumArt	|Path to album image				| Cover
---

|Command	| Description 					|Argument|
|-----------|------------------------		|--------
|playpause	|Pauses or resumes playback		|
|play		|Starts playback				|
|pause		|Pauses playback				|
|next		|Next song						|
|previous	|Previous song					|
|volume		|Changes active device volume	|0-100
|seek		|Seek to positon (ms)			|0-*length*
|seekpercent|Seek to position (percent)		|0-100
|shuffle	|Toggle shuffle					|*true*, *false*
|repeat		|Toggle repeat					|*track*, *context*, *off*