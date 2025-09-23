namespace Reliant.Photo

open System
open System.IO

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Layout

module Asset =

    /// Asset path.
    let path = "ReliantPhotoImporter.Assets"

module View =

    /// Creates source components.
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
                ComboBox.width 200
                ComboBox.horizontalAlignment HorizontalAlignment.Left
                ComboBox.verticalAlignment VerticalAlignment.Center
                ComboBox.padding 10
                ComboBox.margin 10
                ComboBox.row row
                ComboBox.column 1
            ]
        ]

    /// Creates destination components.
    let private createDestinationParts row model dispatch =

        let fullName =
            model.Destination.FullName
        let shortName =
            let maxLen = 26
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
                Button.width 200
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

    /// Creates import name components.
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
                TextBox.width 200
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

    /// Creates example components.
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
                    model.ImportStatus = NotStarted)
                Button.width 200
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

    /// Creates a view of the given model.
    let view model dispatch =
        Window.create [
            Window.sizeToContent SizeToContent.WidthAndHeight
            Window.child (
                Grid.create [
                    Grid.margin 10
                    Grid.columnDefinitions "Auto, Auto"
                    Grid.rowDefinitions "Auto, Auto, Auto, Auto, Auto"
                    Grid.children [
                        yield! createSourceParts 0 model dispatch
                        yield! createDestinationParts 1 model dispatch
                        yield! createNameParts 2 model dispatch
                        yield! createExampleParts 3 model
                        yield! createImportParts 4 model dispatch
                    ]
                ]
            )
        ]
