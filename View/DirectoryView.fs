namespace Reliant.Photo

open System.IO

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Interactivity
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Platform.Storage

module Cursor =

    /// Wait cursor.
    let wait = new Cursor(StandardCursorType.Wait)

module Button =

    /// Button height and width.
    let buttonSize = 50

    /// Creates a text button.
    let createText text callback =
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
            Button.height buttonSize
            Button.horizontalAlignment HorizontalAlignment.Stretch
            Button.verticalAlignment VerticalAlignment.Stretch
            Button.horizontalContentAlignment HorizontalAlignment.Center
            Button.verticalContentAlignment VerticalAlignment.Center
            Button.onClick callback
        ]

module DirectoryView =

    /// Allows user to select a directory.
    let private onSelectDirectory dispatch args =
        let topLevel =
            (args : RoutedEventArgs).Source
                :?> Control
                |> TopLevel.GetTopLevel
        async {
            let! folders =
                let options = FolderPickerOpenOptions()
                topLevel
                    .StorageProvider
                    .OpenFolderPickerAsync(options)
                    |> Async.AwaitTask
            if folders.Count > 0 then
                folders[0].Path.LocalPath
                    |> DirectoryInfo
                    |> (DirectorySelected >> MkDirectoryMessage)
                    |> dispatch
        } |> Async.StartImmediate

    /// Creates a toolbar.
    let private createToolbar dock dispatch =
        StackPanel.create [
            StackPanel.dock dock
            StackPanel.orientation Orientation.Horizontal
            StackPanel.spacing 5.0
            StackPanel.margin 5.0
            StackPanel.children [
                Button.createText "🗀" (
                    onSelectDirectory dispatch)
            ]
        ]

    /// Creates an image control with hover effect.
    let private createImage
        (file : FileInfo) (source : IImage) dispatch =
        Component.create (
            file.FullName,
            fun ctx ->
                let isHovered = ctx.useState false
                Border.create [
                    Border.background (
                        if isHovered.Current then "DarkGray"
                        else "Transparent")
                    Border.child (
                        Image.create [
                            Image.source source
                            Image.height source.Size.Height   // why is this necessary?
                            Image.stretch Stretch.Uniform
                            Image.margin 8.0
                            Image.onTapped (fun _ ->
                                dispatch (SwitchToImage file))
                        ])
                    Border.onPointerEntered (fun _ -> isHovered.Set true)
                    Border.onPointerExited (fun _ -> isHovered.Set false)
                ]
        )

    /// Creates a view of the given model.
    let view (model : DirectoryModel) dispatch =

        let images =
            [
                for file, result in model.ImageLoadPairs do
                    match result with
                        | Ok source ->
                            createImage file source dispatch
                                :> IView
                        | _ -> ()
            ]

        DockPanel.create [
            DockPanel.children [

                createToolbar Dock.Top dispatch

                ScrollViewer.create [
                    ScrollViewer.background "#181818"
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
