namespace Reliant.Photo

open System.IO

open Avalonia.Media.Imaging

open SixLabors.ImageSharp
open SixLabors.ImageSharp.Formats
open SixLabors.ImageSharp.Formats.Png

/// Result of trying to load an image.
type ImageResult = Result<Bitmap, string (*error message*)>

module ImageFile =

    /// PNG encoder.
    let private pngEncoder =
        PngEncoder(
            CompressionLevel =
                PngCompressionLevel.NoCompression)

    /// Creates decoder options for loading an image.
    let private getDecoderOptions targetHeightOpt (file : FileInfo) =
        match targetHeightOpt with

                // decode to the given height
            | Some targetHeight ->
                let targetWidth =
                    let imgInfo = Image.Identify(file.FullName)
                    int (float imgInfo.Width
                        / float imgInfo.Height * float targetHeight)
                DecoderOptions(
                    TargetSize = Size(targetWidth, targetHeight))

            | None -> DecoderOptions()

    /// Tries to load an image from the given file.
    let tryLoadImage targetHeightOpt file =
        async {
            try
                    // load image to PNG format
                use stream = new MemoryStream()
                do
                    let options =
                        getDecoderOptions targetHeightOpt file
                    use image = Image.Load(options, file.FullName)
                    image.SaveAsPng(stream, pngEncoder)
                    stream.Position <- 0

                    // create Avalonia bitmap
                let image = new Bitmap(stream)
                return (Ok image : ImageResult)

            with exn ->
                return Error exn.Message
        }
