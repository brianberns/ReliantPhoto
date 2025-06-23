namespace Reliant.Photo

open System.IO

open Avalonia.Media.Imaging

open SixLabors.ImageSharp
open SixLabors.ImageSharp.Formats.Png

/// Model state.
type State =
    {
        /// Current or upcoming image file. This is set before
        /// the image itself is loaded.
        File : FileInfo

        /// Current loaded image, if any. This will be the old
        /// image when starting to browse to a new one.
        ImageResult : Result<Bitmap, string>

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
            state.File.Directory.GetFiles()
                |> Array.where (fun file ->
                    supportedExtensions.Contains(
                        file.Extension.ToLower()))

            // find index of file we're browsing to, if possible
        let toIdxOpt =
            option {
                let! fromIdx =
                    files
                        |> Array.tryFindIndex (fun file ->
                            file.FullName = state.File.FullName)
                let toIdx = fromIdx + incr
                if toIdx >= 0 && toIdx < files.Length then
                    return toIdx
            }

            // update state accordingly
        match toIdxOpt with
            | Some toIdx ->
                { state with
                    File = files[toIdx]
                    HasPreviousImage = toIdx > 0
                    HasNextImage = toIdx < files.Length - 1 }
            | None -> state

    /// Browses to the given file.
    let init (file : FileInfo) =
        browseImage 0 {
            File = file
            ImageResult = Error ""
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
        try
                // load image to PNG format
            use stream = new MemoryStream()
            do
                use image = Image.Load(path : string)
                image.SaveAsPng(stream, pngEncoder)
                stream.Position <- 0

                // create Avalonia bitmap
            Ok (new Bitmap(stream))

        with exn ->
            Error exn.Message
