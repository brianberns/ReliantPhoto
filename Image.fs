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
    
type State =
    {
        BitmapOpt : Option<Bitmap>
    }

type Message =
    | LoadImage of FileInfo
    | ImageLoaded of Option<Bitmap>

module Location =

    let init path =
        { BitmapOpt = None },
        Cmd.ofMsg (LoadImage path)

    let tryLoadBitmap path =
        async {
            try
                use image = Image.Load(path : string)
                use stream = new MemoryStream()
                image.SaveAsPng(stream)
                stream.Position <- 0
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
                for bitmap in state.BitmapOpt |> Option.toArray do
                    Image.create [
                        Image.source bitmap
                    ]
            ]
        ]
