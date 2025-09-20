namespace Reliant.Photo

open System.IO

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Interactivity
open Avalonia.Layout
open Avalonia.Markup.Xaml.MarkupExtensions
open Avalonia.Platform.Storage
open Avalonia.Styling

module View =

    /// Allows user to select a directory.
    let private onSelectDirectory dispatchDir args =
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
                    |> dispatchDir
        } |> Async.StartImmediate

    /// Border style.
    let private createBorderStyle () : IStyle =
        let style = Style(_.OfType<Border>())
        style.Setters.Add(
            Setter(
                Border.BorderBrushProperty,
                DynamicResourceExtension(
                    "SystemControlBackgroundBaseLowBrush")))
        style

    /// Creates a directory view.
    let private createDirectoryView
        label (dir : DirectoryInfo) dispatchDir =
        StackPanel.create [

            StackPanel.orientation Orientation.Horizontal
            StackPanel.margin 10
            StackPanel.spacing 10
            StackPanel.children [

                TextBlock.create [
                    TextBlock.text label
                    TextBlock.width 150   // make this dynamic
                    TextBlock.verticalAlignment
                        VerticalAlignment.Center
                ]

                Border.create [
                    Border.styles [
                        createBorderStyle ()
                    ]
                    Border.borderThickness 2
                    Border.padding 10
                    Border.child (
                        TextBlock.create [
                            TextBlock.text dir.Name
                            TextBlock.tip dir.FullName
                            TextBlock.width 200
                            TextBlock.verticalAlignment
                                VerticalAlignment.Center
                        ]
                    )
                ]

                Button.createIcon
                    Icon.folderOpen
                    (onSelectDirectory dispatchDir)
            ]
        ]

    /// Creates source directory view.
    let private createSourceDirectoryView model dispatch =
        createDirectoryView
            "Import images from:"
            model.Source
            (SetSource >> dispatch)

    /// Creates destination directory view.
    let private createDestinationDirectoryView model dispatch =
        createDirectoryView
            "Import images to:"
            model.Destination
            (SetSource >> dispatch)

    let view model dispatch =
        Window.create [
            Window.sizeToContent SizeToContent.WidthAndHeight
            Window.child (
                StackPanel.create [
                    StackPanel.children [
                        createSourceDirectoryView model dispatch
                        createDestinationDirectoryView model dispatch
                    ]
                ]
            )
        ]
