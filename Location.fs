namespace Reliant.Photo

open System
open System.Diagnostics
open System.IO

open Avalonia.Controls
open Avalonia.Layout
open Avalonia.Media.Imaging
open Avalonia.FuncUI.DSL
    
type LocationState =
    {
        DirectoryInfo : DirectoryInfo
    }

type LocationMessage =
    | NoOp

module Location =

    let init path =
        {
            DirectoryInfo =
                Environment.SpecialFolder.MyPictures
                    |> Environment.GetFolderPath
                    |> DirectoryInfo
        }

    let update msg state =
        match msg with
            | NoOp -> state

    let tryLoadBitmap (fileInfo : FileInfo) =
        let path = fileInfo.FullName
        try
            new Bitmap(path) |> Some
        with exn ->
            Trace.WriteLine($"{path}: {exn.Message}")
            None

    let view state dispatch =

        let bitmaps =
            state.DirectoryInfo
                .GetFiles()
                |> Array.choose tryLoadBitmap
                    
        DockPanel.create [
            DockPanel.children [
                for bitmap in bitmaps do
                    Image.create [
                        Image.source bitmap
                    ]
            ]
        ]
