namespace Reliant.Photo

open System.IO

open Avalonia.Media.Imaging

open SixLabors.ImageSharp
open SixLabors.ImageSharp.Formats.Png

type ImageResult = Result<Bitmap, string>

module ImageFile =

    /// PNG encoder.
    let private pngEncoder =
        PngEncoder(
            CompressionLevel =
                PngCompressionLevel.NoCompression)

    /// Tries to load an image from the given file.
    let tryLoadImage (file : FileInfo) =
        async {
            try
                    // load image to PNG format
                use stream = new MemoryStream()
                do
                    use image = Image.Load(file.FullName)
                    image.SaveAsPng(stream, pngEncoder)
                    stream.Position <- 0

                    // create Avalonia bitmap
                return (Ok (new Bitmap(stream)) : ImageResult)

            with exn ->
                return Error exn.Message
        }
