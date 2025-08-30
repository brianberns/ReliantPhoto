namespace Reliant.Photo

open System.IO

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Interactivity
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Platform.Storage

module Color =

    /// Dark gray.
    let darkGray = Color.Parse "#181818"

module Button =

    /// Button height and width.
    let buttonSize = 50

    /// Creates a text button.
    let createText text (tooltip : string) onClick =
        Button.create [
            Button.content (
                Viewbox.create [
                    Viewbox.stretch Stretch.Uniform
                    Viewbox.stretchDirection StretchDirection.Both
                    Viewbox.child (
                        TextBlock.create [
                            TextBlock.text text
                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                            TextBlock.verticalAlignment VerticalAlignment.Center
                            TextBlock.textWrapping TextWrapping.NoWrap
                        ]
                    )
                ]
            )
            ToolTip.tip tooltip
            Button.height buttonSize
            Button.minWidth buttonSize
            Button.horizontalAlignment HorizontalAlignment.Stretch
            Button.verticalAlignment VerticalAlignment.Stretch
            Button.horizontalContentAlignment HorizontalAlignment.Center
            Button.verticalContentAlignment VerticalAlignment.Center
            Button.onClick onClick
        ]

module FileSystemView =

    /// Allows user to select an image.
    let onSelectImage dispatch args =
        let topLevel =
            (args : RoutedEventArgs).Source
                :?> Control
                |> TopLevel.GetTopLevel
        async {
            let! folders =
                let options = FilePickerOpenOptions()
                topLevel
                    .StorageProvider
                    .OpenFilePickerAsync(options)
                    |> Async.AwaitTask
            if folders.Count > 0 then
                folders[0].Path.LocalPath
                    |> FileInfo
                    |> LoadImage
                    |> dispatch
        } |> Async.StartImmediate

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
            StackPanel.children [
                TextBlock.create [
                    TextBlock.text $"{numImages} images"
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
                    ToolTip.tip file.Name
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
                ]
            ]
        ]
