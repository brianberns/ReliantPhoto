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
                    |> SetSource
                    |> dispatch
        } |> Async.StartImmediate

    let private borderStyle : IStyle =
        let style = Style(_.OfType<Border>())
        let setter =
            Setter(
                Border.BorderBrushProperty,
                DynamicResourceExtension(
                    "SystemControlBackgroundBaseLowBrush")
            )
        style.Setters.Add(setter)
        style

    let view model dispatch =
        StackPanel.create [
            StackPanel.children [
                StackPanel.create [

                    StackPanel.orientation Orientation.Horizontal
                    StackPanel.margin 10
                    StackPanel.spacing 10
                    StackPanel.children [

                        TextBlock.create [
                            TextBlock.text "Import images from:"
                            TextBlock.verticalAlignment VerticalAlignment.Center
                        ]

                        Border.create [
                            Border.styles [
                                borderStyle
                            ]
                            Border.borderThickness 2
                            Border.padding 10
                            Border.child (
                                TextBlock.create [
                                    TextBlock.text model.Source.Name
                                    TextBlock.tip model.Source.FullName
                                    TextBlock.verticalAlignment VerticalAlignment.Center
                                ]
                            )
                        ]

                        Button.createIcon
                            Icon.folderOpen
                            (onSelectDirectory dispatch)
                    ]
                ]
            ]
        ]
