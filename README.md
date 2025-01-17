# Tellah - HTML Photo/Video Album Generator

![Tellah](https://raw.githubusercontent.com/gordonglas/tellah-photo/main/tellah.webp)

Simple tool for creating smaller versions of photos and videos and use them to generate an HTML album, that can be used for self-hosting.

TellahPhoto has only been tested on Windows, but shouldn't take too much work to get working on other platforms. The generated html albums themselves can be hosted anywhere, on any platform.

[Demo album](https://gglas.ninja/photos/public/album-index.html)

* To run the command line app, `tellah`, you'll need to:
    * Install the `.NET 8 runtime`.
    * Download [exiftool.exe](https://exiftool.org), [ffmpeg.exe](https://www.ffmpeg.org) and [ffprobe.exe](https://www.ffmpeg.org), and put them in your environment path.
        * `ffprobe` comes with `ffmpeg`.
    * You can download pre-built `tellah` binaries in [Releases](https://github.com/gordonglas/tellah-photo/releases) or [build from source](#build-from-source).
    * See [How does it work?](#how-does-it-work) and [Command-line Reference](#command-line-reference) on how to use it.

* Uses Imagick.NET lib for image resizing.
* Calls out to `exiftool.exe` to get file info and set EXIF metadata.
* Calls out to `ffprobe.exe` to get file info (for things that don't work well with exiftool).
* Calls out to `ffmpeg.exe` to transcode videos and generate preview images from videos.
* Yes, it's named after the Great Sage of Mysidia from Final Fantasy IV. Yes, I'm a huge dork.

## How does it work?
* You put your raw photos/videos for each album in it's own folder, under a parent folder somewhere. For this example, I'll have multiple album folders under a parent "public" folder, that will end up as my public photo album. So for example, I might organize my raw photos like so:
```
c:\photos\raw\public\
  |-- art\
    |-- some-image.jpg
    |-- a-video.mp4
  |-- gaming\
    |-- final-fantasy-iv.png
    |-- mario.gif
  |-- vacation\
    |-- trip-to-korea.mov
```
* You run tellah to convert those raw photos/videos to a smaller web-friendly format, which saves them to a different folder (your raw photos/videos won't be changed). This also generates a `.tellah.json` file that contains all the album metadata. You run this command for each album, so commands for the above example might look like this:
```
tellah -i "c:\photos\raw\public\art" -o "c:\photos\generated-albums\public\art" -album-name "Art"
tellah -i "c:\photos\raw\public\gaming" -o "c:\photos\generated-albums\public\gaming" -album-name "Gaming"
tellah -i "c:\photos\raw\public\vacation" -o "c:\photos\generated-albums\public\vacation" -album-name "Vacation"
```
* You can then add/modify/delete some of the raw photos/videos, then run the same command again to have it sync those changes with the output folder and the `.tellah.json` file. Because of this, your `raw` folder should really be a "copy" of your original photos that you keep backed up somewhere else.
* You can then run another tellah command to build the album html, which uses the metadata in the `.tellah.json` file. This command looks something like this.
```
tellah -build-html "c:\photos\generated-albums\public" -album-index-url-path "/photos/public/"
```
* At this point, you'll have a fully functional html album. You can view it in your browser by opening `c:\photos\generated-albums\public\album-index.html`
* You can also run another tellah command to set one of the generated thumbnail images as an album's cover photo, which shows on the album's index page. You'll then need to rebuild the html again for that change to be seen in the html album index page. These command's might look like so:
```
tellah -album-cover "c:\photos\generated-albums\public\art\a-video_tm.jpg"
tellah -album-cover "c:\photos\generated-albums\public\gaming\mario_tm.jpg"

tellah -build-html "c:\photos\generated-albums\public" -album-index-url-path "/photos/public/"
```

## Command-line reference
* Run `tellah` without any command line switches to display usage info.

## What does it NOT do?
* Modify your raw (input) files.
* Support recursive folders.
* Support a search-pattern.
* Support every image/video format. I have an iPhone, so I'm biased with the formats I use/dev/test with. That said, Imagick.NET, ffmpeg, exiftool, etc have pretty wide support for a large variety of formats, so other formats might "just work" without any code changes needed. If you find that something doesn't work, submit an issue or pull request.
* Always copy all EXIF, IPTC, XMP metadata to the files it generates. It mostly does this, but cannot when EXIF data is corrupted to begin with. For these files that have corrupted EXIF data, sometimes you won't see the "Date Taken" in Windows Explorer even though exiftool can extract it (but exiftool cannot reliably overwrite it). See: https://exiftool.org/forum/index.php?topic=4936.0 The good news is, in the cases where exiftool can read the "date taken", it will be in the `.tellah.json` file.
* Slice, dice, and make julienne fries.

## Build from source

If you just need a pre-built binary, you can find it under [Releases](https://github.com/gordonglas/tellah-photo/releases).
If you want to build it, just clone the repo, open the .sln file in Visual Studio 2022+ and build it. You'll need the `.NET Desktop Development` Visual Studio workload installed.

## Do you accept pull requests?

Sure! Just keep in mind, I have a day-job and several other personal projects I'm currently working on, so it might take some time for me to respond/include your changes. I created this project on my own time, mainly because I want to de-Google myself that much more, and don't want to pay Google to host my photos/videos.

## TODO (maybe):
* A slideshow button.
* Test/fix problems on platforms other than Windows. (double-quotes, slashes, etc)
* Use AV1 (patent/royalty-free) video format when it has more browser support: https://caniuse.com/av1
* A lightweight cross-platform UI at some point.
