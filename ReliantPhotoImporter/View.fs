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

    /// Creates a directory view's components.
    let private createDirectoryViewParts
        row label dirOpt dispatchDir =
        [
                // label
            TextBlock.create [
                TextBlock.text label
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.row row
                TextBlock.column 0
            ] :> IView

                // directory name
            TextBox.create [
                match dirOpt with
                    | Some (dir : DirectoryInfo) ->
                        TextBox.text dir.Name
                        TextBox.tip dir.FullName
                    | None -> ()
                TextBox.isReadOnly true
                TextBox.focusable false
                TextBox.width 200
                TextBox.verticalAlignment VerticalAlignment.Center
                TextBox.padding 10
                TextBox.margin 10
                TextBox.row row
                TextBox.column 1
                TextBox.onPointerPressed (
                    onSelectDirectory dispatchDir)
            ]

                // directory selection
            Button.createIcon
                Icon.folderOpen
                [
                    Button.row row
                    Button.column 2
                ]
                (onSelectDirectory dispatchDir)
        ]

    /// Creates import name components.
    let private createNameParts row model dispatch =
        [
                // label
            TextBlock.create [
                TextBlock.text "Name:"
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.row row
                TextBlock.column 0
            ] :> IView

                // name
            TextBox.create [
                match model.NameOpt with
                    | Some name -> TextBox.text name
                    | None -> ()
                TextBox.width 200
                TextBox.verticalAlignment VerticalAlignment.Center
                TextBox.padding 10
                TextBox.margin 10
                TextBox.row row
                TextBox.column 1
                TextBox.onTextChanged (
                    SetName >> dispatch)
            ]
        ]

    /// Creates example components.
    let private createExampleParts row model =
        [
                // label
            TextBlock.create [
                TextBlock.text "Example:"
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.row row
                TextBlock.column 0
            ] :> IView

                // example
            let name =
                model.NameOpt
                    |> Option.defaultValue "Himalayas"
            TextBlock.create [
                TextBlock.text $"{name}/{name} 001.jpg"
                TextBlock.width 200
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.padding 10
                TextBlock.margin 10
                TextBlock.row row
                TextBlock.column 1
            ]
        ]

    /// Creates import parts.
    let private createImportParts row =
        [
            Button.create [
                Button.content "Import"
                Button.width 200
                Button.horizontalContentAlignment
                    HorizontalAlignment.Center
                Button.verticalAlignment VerticalAlignment.Center
                Button.padding 10
                Button.margin 10
                Button.row row
                Button.column 1
            ] :> IView
        ]

    /// Creates a view of the given model.
    let view model dispatch =
        Window.create [
            Window.sizeToContent SizeToContent.WidthAndHeight
            Window.child (
                Grid.create [
                    Grid.margin 10
                    Grid.columnDefinitions "Auto, Auto, Auto"
                    Grid.rowDefinitions "Auto, Auto, Auto, Auto, Auto"
                    Grid.children [

                            // source
                        yield! createDirectoryViewParts
                            0
                            "Import images from:"
                            model.SourceOpt
                            (SetSource >> dispatch)

                            // destination
                        yield! createDirectoryViewParts
                            1
                            "Import images to:"
                            (Some model.Destination)
                            (SetDestination >> dispatch)

                        yield! createNameParts 2 model dispatch
                        yield! createExampleParts 3 model
                        yield! createImportParts 4
                    ]
                ]
            )
        ]
