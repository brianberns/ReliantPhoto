namespace Reliant.Photo

open System.Diagnostics
open System.IO

open Elmish

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Media.Imaging
open Avalonia.Threading

open SixLabors.ImageSharp
open SixLabors.ImageSharp.Formats.Png

type State =
    {
        /// Directory containing images to browse.
        Directory : DirectoryInfo

        /// Current image file, if any. This is set before
        /// the image itself is loaded.
        FileOpt : Option<FileInfo>

        /// Current loaded image, if any.
        ImageOpt : Option<Bitmap>
    }

type Message =

    /// Load the current image file, if possible.
    | LoadImage

    /// The current image file was (maybe) loaded.
    | ImageLoaded of Option<Bitmap>

    /// Browse to previous image in directory, if possible.
    | PreviousImage

    /// Browse to next image in directory, if possible.
    | NextImage

module Image =

    let init (file : FileInfo) =
        {
            Directory = file.Directory
            FileOpt = Some file
            ImageOpt = None
        },
        Cmd.ofMsg LoadImage

    let private pngEncoder =
        PngEncoder(
            CompressionLevel =
                PngCompressionLevel.NoCompression)

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

    let private supportedExtensions =
        set [
            ".bmp"
            ".gif"
            ".jpeg"
            ".jpg"
            ".pbm"
            ".png"
            ".tif"
            ".tiff"
            ".tga"
            ".qoi"
            ".webp"
        ]

    let private tryIncrImage incr state =
        let files =
            state.Directory.GetFiles()
                |> Array.where (fun file ->
                    supportedExtensions.Contains(
                        file.Extension.ToLower()))
        option {
            let! curFile = state.FileOpt
            let! curIdx =
                files
                    |> Array.tryFindIndex (fun file ->
                        file.FullName = curFile.FullName)
            let nextIdx = curIdx + incr
            if nextIdx >= 0 && nextIdx < files.Length then
                let hasPrev = nextIdx > 0
                let hasNext = nextIdx < files.Length - 1
                return files[nextIdx] //, hasPrev, hasNext
        }

    let update msg state =
        match msg with

            | LoadImage ->
                match state.FileOpt with
                    | Some file ->
                        let cmd =
                            Cmd.OfAsync.perform
                                tryLoadBitmap
                                file.FullName
                                ImageLoaded
                        state, cmd
                    | None -> failwith "No file to load"

            | ImageLoaded bitmapOpt ->
                { state with ImageOpt = bitmapOpt },
                Cmd.none

            | PreviousImage  ->
                let fileOpt = tryIncrImage -1 state
                { state with
                    FileOpt = fileOpt
                    ImageOpt = None },
                Cmd.ofMsg LoadImage

            | NextImage  ->
                let fileOpt = tryIncrImage 1 state
                { state with
                    FileOpt = fileOpt
                    ImageOpt = None },
                Cmd.ofMsg LoadImage

    let view state dispatch =
        DockPanel.create [
            DockPanel.children [
                Button.create [
                    Button.content "◀"
                    Button.dock Dock.Left
                    Button.onClick (fun _ -> dispatch PreviousImage)
                ]
                Button.create [
                    Button.content "▶"
                    Button.dock Dock.Right
                    Button.onClick (fun _ -> dispatch NextImage)
                ]
                match state.ImageOpt with
                    | Some image ->
                        Image.create [
                            Image.source image
                        ]
                    | None ->
                        TextBlock.create []
            ]
        ]
