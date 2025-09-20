namespace Reliant.Photo

open System.IO

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Interactivity
open Avalonia.Layout
open Avalonia.Platform.Storage

module View =

    /// Allows user to select a directory.
    let onSelectDirectory dispatch args =
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

    let view model dispatch =
        Window.create [
            Window.child (
                StackPanel.create [
                    StackPanel.children [
                        StackPanel.create [
                            StackPanel.orientation Orientation.Horizontal
                            StackPanel.children [
                                TextBlock.create [
                                    TextBlock.text "Import images from:"
                                ]
                                TextBlock.create [
                                    TextBlock.text model.Source.FullName
                                ]
                                Button.create [
                                    Button.content "Browse"
                                    Button.onClick (onSelectDirectory dispatch)
                                ]
                            ]
                        ]
                    ]
                ]
            )
        ]
