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
        BitmapOpt : Option<Bitmap>
    }

type Message =
    | LoadImage of FileInfo
    | ImageLoaded of Option<Bitmap>

module Image =

    let init path =
        { BitmapOpt = None },
        Cmd.ofMsg (LoadImage path)

    let private pngEncoder =
        PngEncoder(
            CompressionLevel =
                PngCompressionLevel.NoCompression)

    let tryLoadBitmap path =
        async {
            try
                    // convert image to PNG format
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

    let update msg state =
        match msg with
            | LoadImage fileInfo ->
                let cmd =
                    Cmd.OfAsync.perform
                        tryLoadBitmap
                        fileInfo.FullName
                        ImageLoaded
                state, cmd
            | ImageLoaded bitmapOpt ->
                { state with BitmapOpt = bitmapOpt },
                Cmd.none

    let view state dispatch =
        DockPanel.create [
            DockPanel.children [
                for bitmap in Option.toArray state.BitmapOpt do
                    Image.create [
                        Image.source bitmap
                    ]
            ]
        ]
