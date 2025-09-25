namespace Reliant.Photo

open System
open System.IO

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Layout
open Avalonia.Media

module Asset =

    /// Asset path.
    let path = "ReliantPhotoImporter.Assets"

module View =

    /// Window icon.
    let private icon =
        "ReliantPhotoImporter.png"
            |> Resource.get Asset.path
            |> WindowIcon

    /// Widget width.
    let private widgetWidth = 300.0

    /// Creates source parts.
    let private createSourceParts
        row (model : Model) dispatch =
        [
                // label
            TextBlock.create [
                TextBlock.text "Import images from:"
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.row row
                TextBlock.column 0
            ] :> IView

                // drive
            ComboBox.create [
                ComboBox.dataItems Model.drives
                ComboBox.itemTemplate (
                    DataTemplateView.create<_, _>(fun (drive : DriveInfo) ->
                        TextBlock.create [
                            TextBlock.text (
                                $"{drive.VolumeLabel} ({drive.Name.TrimEnd('\\')})")
                        ]
                    )
                )
                ComboBox.selectedItem model.Source
                ComboBox.width widgetWidth
                ComboBox.horizontalAlignment HorizontalAlignment.Left
                ComboBox.verticalAlignment VerticalAlignment.Center
                ComboBox.padding 10
                ComboBox.margin 10
                ComboBox.row row
                ComboBox.column 1
                ComboBox.onSelectedItemChanged (function
                    | :? DriveInfo as drive ->
                        SetSource drive |> dispatch
                    | _ -> ())
            ]
        ]

    /// Creates destination parts.
    let private createDestinationParts row model dispatch =

        let fullName =
            model.Destination.FullName
        let shortName =
            let maxLen = 40
            if fullName.Length > maxLen then
                $"...{fullName[^maxLen..]}"
            else fullName

        [
                // label
            TextBlock.create [
                TextBlock.text "Import images to:"
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.row row
                TextBlock.column 0
            ] :> IView

                // drive name
            Button.create [
                Button.content shortName
                if fullName <> shortName then
                    Button.tip fullName
                Button.focusable false
                Button.width widgetWidth
                Button.horizontalAlignment HorizontalAlignment.Left
                Button.verticalAlignment VerticalAlignment.Center
                Button.padding 10
                Button.margin 10
                Button.row row
                Button.column 1
                Button.onClick (
                    DirectoryInfo.onPick (SetDestination >> dispatch))
            ]
        ]

    /// Creates import name parts.
    let private createNameParts row (model : Model) dispatch =
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
                TextBox.text model.Name
                TextBox.width widgetWidth
                TextBox.horizontalAlignment HorizontalAlignment.Left
                TextBox.verticalAlignment VerticalAlignment.Center
                TextBox.padding 10
                TextBox.margin 10
                TextBox.row row
                TextBox.column 1
                TextBox.onTextChanged (
                    SetName >> dispatch)
            ]
        ]

    /// Creates example parts.
    let private createExampleParts row (model : Model) =
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
                if String.IsNullOrWhiteSpace model.Name then
                    "Image"
                else model.Name.Trim()
            TextBlock.create [
                TextBlock.text $"{name}/{name} 001.jpg"
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.padding 10
                TextBlock.margin 10
                TextBlock.row row
                TextBlock.column 1
            ]
        ]

    /// Creates import parts.
    let private createImportParts row model dispatch =
        [
            Button.create [
                Button.content "Import"
                Button.isEnabled (
                    not model.ImportStatus.IsImporting)
                Button.width widgetWidth
                Button.horizontalContentAlignment
                    HorizontalAlignment.Center
                Button.verticalAlignment VerticalAlignment.Center
                Button.padding 10
                Button.margin 10
                Button.row row
                Button.column 1
                Button.onClick (fun _ ->
                    dispatch StartImport)
            ] :> IView
        ]

    /// Creates not-started parts.
    let private createNotStartedParts row =
        [
                // label
            TextBlock.create [
                TextBlock.text "Status:"
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.row row
                TextBlock.column 0
            ] :> IView

                // status
            TextBlock.create [
                TextBlock.text "Import not yet started"
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.padding 10
                TextBlock.margin 10
                TextBlock.row row
                TextBlock.column 1
            ]
        ]

    /// Creates starting parts.
    let private createStartingParts row =
        [
                // label
            TextBlock.create [
                TextBlock.text "Status:"
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.row row
                TextBlock.column 0
            ] :> IView

                // status
            TextBlock.create [
                TextBlock.text "Searching for files..."
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.padding 10
                TextBlock.margin 10
                TextBlock.row row
                TextBlock.column 1
            ]
        ]

    /// Creates progress parts.
    let private createProgressParts row import =
        [
                // label
            TextBlock.create [
                TextBlock.text "Status:"
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.margin (0, 10)   // in lieu of progress bar padding
                TextBlock.padding (0, 10)
                TextBlock.row row
                TextBlock.column 0
            ] :> IView

                // progress
            ProgressBar.create [
                ProgressBar.minimum 0
                ProgressBar.maximum import.FileGroups.Length
                ProgressBar.value import.NumGroupsImported
                ProgressBar.tip $"{import.NumGroupsImported} of {import.FileGroups.Length} images imported"
                ProgressBar.margin 10   // padding doesn't work here?
                ProgressBar.row row
                ProgressBar.column 1
            ]
        ]

    /// Creates finished parts.
    let private createFinishedParts row numImages =
        [
                // label
            TextBlock.create [
                TextBlock.text "Status:"
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.row row
                TextBlock.column 0
            ] :> IView

                // status
            TextBlock.create [
                TextBlock.text $"{numImages} images imported"
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.padding 10
                TextBlock.margin 10
                TextBlock.row row
                TextBlock.column 1
            ]
        ]

    /// Creates error parts.
    let private createErrorParts row error =
        [
                // label
            TextBlock.create [
                TextBlock.text "Error:"
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.row row
                TextBlock.column 0
            ] :> IView

                // error
            Border.create [
                Border.borderBrush Brushes.Red
                Border.borderThickness 2
                Border.width widgetWidth
                Border.verticalAlignment VerticalAlignment.Center
                Border.padding 10
                Border.margin 10
                Border.row row
                Border.column 1
                Border.child (
                    SelectableTextBlock.create [
                        SelectableTextBlock.text error
                        SelectableTextBlock.textWrapping TextWrapping.Wrap
                    ]
                )
            ]
        ]

    let createStatusParts row status =
        match status with
            | NotStarted ->
                createNotStartedParts row
            | Starting ->
                createStartingParts row
            | InProgress import ->
                createProgressParts row import
            | Finished numImages ->
                createFinishedParts row numImages
            | Error error ->
                createErrorParts row error

    /// Creates a view of the given model.
    let view model dispatch =
        Window.create [
            Window.sizeToContent SizeToContent.WidthAndHeight
            Window.icon icon
            Window.child (
                Grid.create [
                    Grid.margin 10
                    Grid.columnDefinitions "Auto, Auto"
                    Grid.rowDefinitions "Auto, Auto, Auto, Auto, Auto, Auto"
                    Grid.children [
                        yield! createSourceParts 0 model dispatch
                        yield! createDestinationParts 1 model dispatch
                        yield! createNameParts 2 model dispatch
                        yield! createExampleParts 3 model
                        yield! createImportParts 4 model dispatch
                        yield! createStatusParts 5 model.ImportStatus
                    ]
                ]
            )
        ]
