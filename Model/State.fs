namespace Reliant.Photo

open System.Diagnostics
open System.IO

open Avalonia.Media.Imaging
open Avalonia.Threading

open SixLabors.ImageSharp
open SixLabors.ImageSharp.Formats.Png

/// Model state.
type State =
    {
        /// Directory containing images to browse.
        Directory : DirectoryInfo

        /// Current or upcoming image file, if any. This is set
        /// before the image itself is loaded.
        FileOpt : Option<FileInfo>

        /// Current loaded image, if any. This will be the old
        /// image when starting to browse to a new one.
        ImageOpt : Option<Bitmap>

        /// User can browse to previous image?
        HasPreviousImage : bool

        /// User can browse to next image?
        HasNextImage : bool
    }

module State =

    /// Supported image file extentions.
    let supportedExtensions =
        set [
            ".bmp"
            ".gif"
            ".jpg"; ".jpeg"
            ".pbm"
            ".png"
            ".tif"; ".tiff"
            ".tga"
            ".qoi"
            ".webp"
        ]

    /// Browses to an image in the current directory, if
    /// possible.
    let browseImage incr state =

            // get all candidate files for browsing
        let files =
            state.Directory.GetFiles()
                |> Array.where (fun file ->
                    supportedExtensions.Contains(
                        file.Extension.ToLower()))

            // find index of file we're browsing to, if possible
        let browseIdxOpt =
            option {
                let! curFile = state.FileOpt
                let! curIdx =
                    files
                        |> Array.tryFindIndex (fun file ->
                            file.FullName = curFile.FullName)
                let browseIdx = curIdx + incr
                if browseIdx >= 0 && browseIdx < files.Length then
                    return browseIdx
            }

            // update state accordingly
        match browseIdxOpt with
            | Some browseIdx ->
                { state with
                    FileOpt = Some files[browseIdx]
                    HasPreviousImage = browseIdx > 0
                    HasNextImage = browseIdx < files.Length - 1 }
            | None ->
                { state with
                    FileOpt = None
                    HasPreviousImage = false
                    HasNextImage = false }

    /// Browses to the given file.
    let init (file : FileInfo) =
        browseImage 0 {
            Directory = file.Directory
            FileOpt = Some file
            ImageOpt = None
            HasPreviousImage = false
            HasNextImage = false
        }

    /// PNG encoder.
    let private pngEncoder =
        PngEncoder(
            CompressionLevel =
                PngCompressionLevel.NoCompression)

    /// Tries to load a bitmap from the given image file.
    let tryLoadBitmap path =
        async {
            try
                    // load image to PNG format
                use stream = new MemoryStream()
                do
                    use image = Image.Load(path : string)
                    image.SaveAsPng(stream, pngEncoder)
                    stream.Position <- 0

                    // create bitmap on UI thread
                let! bitmap =
                    Dispatcher.UIThread.InvokeAsync(fun () ->
                        new Bitmap(stream))
                        .GetTask()
                        |> Async.AwaitTask
                return Some bitmap

            with exn ->
                Trace.WriteLine($"{path}: {exn.Message}")
                return None
        }
