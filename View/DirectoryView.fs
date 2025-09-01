namespace Reliant.Photo

open System.IO

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Layout
open Avalonia.Media

module DirectoryView =

    /// Creates a toolbar.
    let private createToolbar dock dispatch =
        StackPanel.create [
            StackPanel.dock dock
            StackPanel.orientation Orientation.Horizontal
            StackPanel.spacing 5.0
            StackPanel.margin 5.0
            StackPanel.children [
                Button.createText "🗀" "Open image file" (
                    FileSystemView.onSelectImage dispatch)
            ]
        ]

    /// Creates a status bar.
    let private createStatusBar dock (numImages : int) =
        StackPanel.create [
            StackPanel.dock dock
            StackPanel.orientation Orientation.Horizontal
            StackPanel.spacing 5.0
            StackPanel.margin 5.0
            StackPanel.fontSize 12.0
            StackPanel.children [
                TextBlock.create [
                    TextBlock.text $"{numImages} images"
                    TextBlock.background Color.darkGray
                    TextBlock.padding 5.0
                ]
            ]
        ]

    /// Creates an image.
    let private createImage (source : IImage) =
        Image.create [
            Image.source source
            Image.height source.Size.Height   // why is this necessary?
            Image.stretch Stretch.Uniform
            Image.margin 8.0
        ]

    /// Creates an image control with hover effect.
    let private createImageView
        (file : FileInfo) source dispatch =
        Component.create (
            file.FullName,
            fun ctx ->
                let isHovered = ctx.useState false
                Border.create [
                    Border.child (createImage source)
                    Border.tip file.Name
                    Border.background (
                        if isHovered.Current then "#808080"
                        else "Transparent")
                    Border.onPointerEntered (fun _ ->
                        isHovered.Set true)
                    Border.onPointerExited (fun _ ->
                        isHovered.Set false)
                    Border.onTapped (fun _ ->
                        dispatch (LoadImage file))
                ]
        )

    /// Creates a view of the given model.
    let view (model : DirectoryModel) dispatch =

        let images =
            [
                for file, result in model.FileImageResults do
                    match result with
                        | Ok source ->
                            createImageView file source dispatch
                                :> IView
                        | _ -> ()
            ]

        DockPanel.create [
            DockPanel.children [

                createToolbar Dock.Top dispatch
                createStatusBar Dock.Bottom images.Length

                ScrollViewer.create [
                    ScrollViewer.background Color.darkGray
                    ScrollViewer.content (
                        WrapPanel.create [
                            WrapPanel.orientation
                                Orientation.Horizontal
                            WrapPanel.margin 8.0
                            WrapPanel.children images
                        ]
                    )
                    if images.IsEmpty && model.IsLoading then
                        ScrollViewer.cursor Cursor.wait
                ]
            ]
        ]
