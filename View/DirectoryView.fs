namespace Reliant.Photo

open Avalonia.Controls
open Avalonia.FuncUI.DSL

module DirectoryView =

    /// Creates a view of the given model.
    let view model dispatch =
        DockPanel.create [
            DockPanel.children [
                for file, result in model.ImageResults do
                    match result with
                        | Ok image ->
                            Image.create [
                                Image.source image
                                Image.width 100.0
                                Image.margin 10.0
                                Image.onDoubleTapped (fun _ ->
                                    dispatch (SwitchToImage file))
                            ]
                        | Error _ -> ()
            ]
        ]
