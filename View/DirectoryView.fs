namespace Reliant.Photo

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media

module DirectoryView =

    let private waitCursor = new Cursor(StandardCursorType.Wait)

    /// Creates a view of the given model.
    let view model dispatch =
        DockPanel.create [
            if model.IsLoading then
                DockPanel.cursor waitCursor
                DockPanel.background "Transparent"   // needed to force the cursor change for some reason
            else

                let images =
                    [
                        for file, result in model.ImageResults do
                            match result with
                                | Ok image ->
                                    Image.create [
                                        Image.source image
                                        Image.height image.Size.Height   // why is this necessary?
                                        Image.stretch Stretch.Uniform
                                        Image.margin 8.0
                                        Image.onDoubleTapped (fun _ ->
                                            dispatch (SwitchToImage file))
                                    ] :> IView
                                | Error _ -> ()
                    ]

                DockPanel.children [
                    ScrollViewer.create [
                        ScrollViewer.content (
                            WrapPanel.create [
                                WrapPanel.orientation Orientation.Horizontal
                                WrapPanel.margin 8.0
                                WrapPanel.children images
                            ]
                        )
                    ]
            ]
        ]
