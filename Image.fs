namespace Reliant.Photo

open System.Diagnostics
open System.IO

open Elmish

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media.Imaging
open Avalonia.Threading

open SixLabors.ImageSharp
open SixLabors.ImageSharp.Formats.Png

type State =
    {
        Directory : DirectoryInfo
        FileOpt : Option<FileInfo>
        ImageOpt : Option<Bitmap>
    }

type Message =
    | LoadImage
    | ImageLoaded of Option<Bitmap>
    | NextImage
    | PreviousImage

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
                return files[nextIdx]
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

            | NextImage  ->
                let fileOpt = tryIncrImage 1 state
                { state with
                    FileOpt = fileOpt
                    ImageOpt = None },
                Cmd.ofMsg LoadImage

            | PreviousImage  ->
                let fileOpt = tryIncrImage -1 state
                { state with
                    FileOpt = fileOpt
                    ImageOpt = None },
                Cmd.ofMsg LoadImage

    let view state dispatch =
        DockPanel.create [
            DockPanel.children [
                Button.create [
                    Button.content "Prev"
                    Button.dock Dock.Left
                    Button.onClick (fun _ -> dispatch PreviousImage)
                ]
                Button.create [
                    Button.content "Next"
                    Button.dock Dock.Right
                    Button.onClick (fun _ -> dispatch NextImage)
                ]
                for bitmap in Option.toArray state.ImageOpt do
                    Image.create [
                        Image.source bitmap
                    ]
            ]
        ]
