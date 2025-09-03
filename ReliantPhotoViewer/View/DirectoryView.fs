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
    let private createToolbar dispatch =
        Toolbar.create [
            Button.createIcon
                Icon.folderOpen
                "Open directory"
                (FileSystemView.onSelectDirectory dispatch)
        ]

    /// Creates a status bar.
    let private createStatusBar
        (dir : DirectoryInfo) (numImages : int) =
        StatusBar.create [

                // directory path
            StatusBar.createSelectableTextBlock
                dir.FullName "Directory path"

                // number of images
            TextBlock.create [
                TextBlock.text $"{numImages} images"
                TextBlock.background Brush.darkGray
                TextBlock.padding 5.0
            ]
        ]

    /// Creates a thumbnail.
    let private createThumbnail (source : IImage) =
        Image.create [
            Image.source source
            Image.height source.Size.Height   // why is this necessary?
            Image.stretch Stretch.Uniform
            Image.margin 8.0
        ]

    /// Creates an thumbnail control with hover effect.
    let private createThumbnailView
        (file : FileInfo) source dispatch =
        let hoverScale = 1.05
        Component.create (
            file.FullName,
            fun ctx ->
                let isHovered = ctx.useState false
                Border.create [
                    Border.child (createThumbnail source)
                    Border.tip file.Name
                    if isHovered.Current then
                        Border.renderTransform (
                            ScaleTransform(hoverScale, hoverScale))
                    Border.onPointerEntered (fun _ ->
                        isHovered.Set true)
                    Border.onPointerExited (fun _ ->
                        isHovered.Set false)
                    Border.onTapped (fun _ ->
                        dispatch (LoadImage file))
                ]
        )

    /// Creates a view of the given model.
    let view model dispatch =

        let thumbnails =
            [
                for file, result in model.FileImageResults do
                    match result with
                        | Ok source ->
                            createThumbnailView file source dispatch
                                :> IView
                        | _ -> ()
            ]

        DockPanel.create [
            DockPanel.children [

                createToolbar dispatch
                createStatusBar model.Directory thumbnails.Length

                ScrollViewer.create [
                    ScrollViewer.background Brush.darkGray
                    ScrollViewer.content (
                        WrapPanel.create [
                            WrapPanel.orientation
                                Orientation.Horizontal
                            WrapPanel.margin 8.0
                            WrapPanel.children thumbnails
                        ]
                    )
                    if thumbnails.IsEmpty && model.IsLoading then
                        ScrollViewer.cursor Cursor.wait
                ]
            ]
        ]
