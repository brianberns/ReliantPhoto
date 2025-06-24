namespace Reliant.Photo

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media

module DirectoryView =

    /// Creates a view of the given model.
    let view model dispatch =
        DockPanel.create [
            DockPanel.children [
                for file in model.Directory.GetFiles() do
                    let result =
                        ImageModel.tryLoadImage file.FullName
                            |> Async.RunSynchronously
                    match result with
                        | Ok image ->
                            Image.create [
                                Image.source image
                                Image.width 100.0
                                Image.margin 10.0
                                Image.onDoubleTapped (fun _ ->
                                    dispatch (SwitchToImage file))
                            ]
                        | _ -> ()
            ]
        ]
