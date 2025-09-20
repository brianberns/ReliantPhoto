namespace Reliant.Photo

open System.IO

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
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

    /// Creates a border style.
    let private createBorderStyle () : IStyle =
        let style = Style(_.OfType<Border>())
        style.Setters.Add(
            Setter(
                Border.BorderBrushProperty,
                DynamicResourceExtension(
                    "SystemControlBackgroundBaseLowBrush")))
        style

    /// Creates a directory view's components.
    let private createDirectoryViewParts
        row label dirOpt dispatchDir =
        [
            TextBlock.create [
                TextBlock.text label
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.row row
                TextBlock.column 0
            ] :> IView

            Border.create [
                Border.styles [ createBorderStyle () ]
                Border.width 200
                Border.borderThickness 2
                Border.padding 10
                Border.margin 10
                Border.row row
                Border.column 1
                Border.child (
                    TextBlock.create [
                        match dirOpt with
                            | Some (dir : DirectoryInfo) ->
                                TextBlock.text dir.Name
                                TextBlock.tip dir.FullName
                            | None -> ()
                        TextBlock.verticalAlignment VerticalAlignment.Center
                    ]
                )
            ]

            Button.createIcon
                Icon.folderOpen
                [
                    Button.row row
                    Button.column 2
                ]
                (onSelectDirectory dispatchDir)
        ]

    let view model dispatch =
        Window.create [
            Window.sizeToContent SizeToContent.WidthAndHeight
            Window.child (
                Grid.create [
                    Grid.margin 10
                    Grid.columnDefinitions "Auto,Auto,Auto"
                    Grid.rowDefinitions "Auto,Auto"
                    Grid.children [

                        yield! createDirectoryViewParts
                            0
                            "Import images from:"
                            model.SourceOpt
                            (SetSource >> dispatch)

                        yield! createDirectoryViewParts
                            1
                            "Import images to:"
                            (Some model.Destination)
                            (SetDestination >> dispatch)
                    ]
                ]
            )
        ]
