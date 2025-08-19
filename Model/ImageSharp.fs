namespace Reliant.Photo

open System.IO

open Avalonia.Media.Imaging

open SixLabors.ImageSharp
open SixLabors.ImageSharp.Processing
open SixLabors.ImageSharp.Formats
open SixLabors.ImageSharp.Formats.Png
open SixLabors.ImageSharp.Metadata.Profiles.Exif

module private ImageSharp =

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

    /// PNG encoder.
    let private pngEncoder =
        PngEncoder(
            CompressionLevel =
                PngCompressionLevel.NoCompression)

    /// Loads an image from the given file.
    let private loadImageImpl options (file : FileInfo) =

        use stream = new MemoryStream()
        do
                // load image
            use image = Image.Load(options, file.FullName)
            image.Mutate(_.AutoOrient() >> ignore)

                // re-encode image to stream
            let encoder =
                match image.Metadata.DecodedImageFormat.Name.ToLower() with
                    | "tiff" -> pngEncoder :> IImageEncoder
                    | _ ->
                        image.Configuration
                            .ImageFormatsManager
                            .GetEncoder(
                                image.Metadata.DecodedImageFormat)
            image.Save(stream, encoder)
            stream.Position <- 0

            // create Avalonia bitmap
        new Bitmap(stream)

    module private DecoderOptions =

        /// Default decoder options.
        let empty = DecoderOptions()

    /// Loads an image from the given file.
    let loadImage file =
        loadImageImpl DecoderOptions.empty file

    /// Loads a thumbnail image from the given file.
    let loadThumbnail height (file : FileInfo) =
        let imageInfo = Image.Identify file.FullName
        let options =
            DecoderOptions(
                TargetSize =   // decode to the given height
                    getTargetSize height imageInfo)
        loadImageImpl options file
