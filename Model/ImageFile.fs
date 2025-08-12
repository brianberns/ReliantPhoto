namespace Reliant.Photo

open System.IO
open System.Threading

open Avalonia.Media.Imaging

module FileInfo =

    /// Waits for the given file to be readable.
    let waitForFileRead
        (token : CancellationToken) (file : FileInfo) =

        let rec loop duration =
            async {
                try
                    use _ = file.OpenRead()
                    return ()
                with
                    | :? IOException as exn
                        when exn.HResult = 0x80070020 ->   // file in use by another process
                        if not token.IsCancellationRequested then
                            do! Async.Sleep(duration : int)
                            return! loop (2 * duration)
                    | :? IOException as exn
                        when exn.HResult = 0x80070002 ->   // file no longer exists
                        ()
            }

        loop 100

/// SixLabors supports TIFF, while Avalonia doesn't.
module private SixLabors =

    open SixLabors.ImageSharp
    open SixLabors.ImageSharp.Processing
    open SixLabors.ImageSharp.Formats
    open SixLabors.ImageSharp.Formats.Png
    open SixLabors.ImageSharp.Metadata.Profiles.Exif

    /// Gets the orientation of the given image.
    let getOrientation (imageInfo : ImageInfo) =
        option {
            let! profile =
                Option.ofObj imageInfo.Metadata.ExifProfile
            let flag, exifValue =
                profile.TryGetValue(ExifTag.Orientation)
            if flag then
                return exifValue.Value
        } |> Option.defaultValue ExifOrientationMode.TopLeft

    /// Gets the target size for the given target height,
    /// accounting for rotation, if necessary.
    let getTargetSize targetHeight imageInfo =
        match getOrientation imageInfo with
            | ExifOrientationMode.LeftTop
            | ExifOrientationMode.RightTop
            | ExifOrientationMode.RightBottom
            | ExifOrientationMode.LeftBottom ->
                Size(targetHeight, 0)   // rotate
            | _ ->
                Size(0, targetHeight)

    /// Creates decoder options for loading an image.
    let private getDecoderOptions heightOpt imageInfo =
        heightOpt
            |> Option.map (fun height ->
                DecoderOptions(
                    TargetSize =   // decode to the given height
                        getTargetSize height imageInfo))
            |> Option.defaultWith DecoderOptions

    /// PNG encoder.
    let private pngEncoder =
        PngEncoder(
            CompressionLevel =
                PngCompressionLevel.NoCompression)

    /// Loads an image from the given file.
    let loadImage heightOpt (file : FileInfo) =

            // load image to PNG format
        use stream = new MemoryStream()
        do
            let options =
                file.FullName
                    |> Image.Identify
                    |> getDecoderOptions heightOpt
            use image = Image.Load(options, file.FullName)
            image.Mutate(_.AutoOrient() >> ignore)
            image.SaveAsPng(stream, pngEncoder)
            stream.Position <- 0

            // create Avalonia bitmap
        new Bitmap(stream)

/// An image result for a specific file.
type FileImageResult = FileInfo * Result<Bitmap, string (*error message*)>

module ImageFile =

    /// Loads an image from the given file.
    let private loadImage heightOpt (file : FileInfo) =
        SixLabors.loadImage heightOpt file

    /// Tries to load an image from the given file.
    let tryLoadImage heightOpt file =
        async {
            try
                let image = loadImage heightOpt file
                return Ok image
            with exn ->
                return Error exn.Message
        }

    /// Tries to load the contents of the given directory.
    let tryLoadDirectory height (dir : DirectoryInfo) =
        dir.EnumerateFiles("*", EnumerationOptions())   // ignore hidden and system files
            |> Seq.map (fun file ->
                async {
                    let! result =
                        tryLoadImage
                            (Some height)
                            file
                    return ((file, result) : FileImageResult)
                })

    /// Gets the appropriate interpolation mode for the given
    /// image file.
    let getInterpolationMode (file : FileInfo) =
        match file.Extension.ToLower() with
            | ".gif" | ".bmp" | ".png" ->   // try to display crisp edges in lossless images
                BitmapInterpolationMode.None
            | _ -> BitmapInterpolationMode.HighQuality
