namespace Reliant.Photo

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Input

module DirectoryView =

    let private waitCursor = new Cursor(StandardCursorType.Wait)

    /// Creates a view of the given model.
    let view model dispatch =
        DockPanel.create [
            if model.IsLoading then
                DockPanel.cursor waitCursor
                DockPanel.background "Transparent"   // needed to force the cursor change for some reason
            else
                DockPanel.children [
                    for file, result in model.ImageResults do
                        match result with
                            | Ok image ->
                                Image.create [
                                    Image.source image
                                    Image.height 100.0
                                    Image.margin 10.0
                                    Image.onDoubleTapped (fun _ ->
                                        dispatch (SwitchToImage file))
                                ]
                            | Error _ -> ()
                ]
        ]
