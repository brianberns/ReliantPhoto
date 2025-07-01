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
    open SixLabors.ImageSharp.Formats
    open SixLabors.ImageSharp.Formats.Png

    /// PNG encoder.
    let private pngEncoder =
        PngEncoder(
            CompressionLevel =
                PngCompressionLevel.NoCompression)

    /// Creates decoder options for loading an image.
    let private getDecoderOptions heightOpt (file : FileInfo) =
        match heightOpt with

                // decode to the given height
            | Some height ->
                let width =
                    let imgInfo = Image.Identify(file.FullName)
                    int (float imgInfo.Width
                        / float imgInfo.Height * float height)
                DecoderOptions(
                    TargetSize = Size(width, height))

            | None -> DecoderOptions()

    /// Loads an image from the given file.
    let loadImage heightOpt file =

            // load image to PNG format
        use stream = new MemoryStream()
        do
            let options =
                getDecoderOptions heightOpt file
            use image = Image.Load(options, file.FullName)
            image.SaveAsPng(stream, pngEncoder)
            stream.Position <- 0

            // create Avalonia bitmap
        new Bitmap(stream)

/// Result of trying to load an image.
type ImageResult = Result<Bitmap, string (*error message*)>

/// An image for a specific file.
type FileImage = FileInfo * Bitmap

module ImageFile =

    /// Loads an image from the given file.
    let private loadImage heightOpt (file : FileInfo) =

        try
            use stream = file.OpenRead()
            match heightOpt with
                | Some height ->
                    Bitmap.DecodeToHeight(stream, height)
                | None -> new Bitmap(stream)

            // try SixLabors for images not supported by Avalonia
        with _ ->
            SixLabors.loadImage heightOpt file

    /// Tries to load an image from the given file.
    let tryLoadImage heightOpt file =
        async {
            try
                let image = loadImage heightOpt file
                return (Ok image : ImageResult)
            with exn ->
                return Error exn.Message
        }

    /// Tries to load the contents of the given directory.
    let tryLoadDirectory height (dir : DirectoryInfo) =
        dir.EnumerateFiles()
            |> Seq.map (fun file ->
                async {
                    let! result =
                        tryLoadImage
                            (Some height)
                            file
                    match result with
                        | Ok image ->
                            return Some ((file, image) : FileImage)
                        | _ -> return None   // ignore error message
                })
