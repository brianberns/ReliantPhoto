namespace Reliant.Photo

open System.Diagnostics
open System.IO

open Elmish

open Avalonia.Controls
open Avalonia.Layout
open Avalonia.Media.Imaging
open Avalonia.FuncUI.DSL

open SixLabors.ImageSharp
    
type State =
    {
        BitmapOpt : Option<Bitmap>
    }

type Message =
    | LoadImage of FileInfo
    // | ImageLoaded of Bitmap

module Location =

    let init path =
        { BitmapOpt = None },
        Cmd.ofMsg (LoadImage path)

    let tryLoadBitmap path =
        try
            use image = Image.Load(path : string)
            use stream = new MemoryStream()
            image.SaveAsPng(stream)
            stream.Position <- 0
            new Bitmap(stream) |> Some
        with exn ->
            Trace.WriteLine($"{path}: {exn.Message}")
            None

    let update msg state =
        match msg with
            | LoadImage fileInfo ->
                {
                    BitmapOpt =
                        tryLoadBitmap fileInfo.FullName
                },
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
