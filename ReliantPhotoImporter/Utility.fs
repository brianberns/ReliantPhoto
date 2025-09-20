namespace Reliant.Photo

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Media.Imaging

module Asset =

    /// Asset path.
    let path = "ReliantPhotoImporter.Assets"

module Icon =

    /// Creates an icon.
    let private create name =
        let stream =
            $"{name}_48dp_E3E3E3_FILL0_wght400_GRAD0_opsz48.png"
                |> Resource.get Asset.path
        new Bitmap(stream)

    /// Folder open.
    let folderOpen = create "folder_open"

module Button =

    /// Button height and width.
    let buttonSize = 42

    /// Creates an icon button.
    let createIcon icon attrs onClick =
        Button.create [
            Button.content (
                Image.create [
                    Image.source icon
                    Image.stretch Stretch.Uniform
                ]
            )
            Button.height buttonSize
            Button.minWidth buttonSize
            Button.horizontalAlignment HorizontalAlignment.Stretch
            Button.verticalAlignment VerticalAlignment.Center
            Button.horizontalContentAlignment HorizontalAlignment.Center
            Button.verticalContentAlignment VerticalAlignment.Center
            Button.cornerRadius 4.0
            yield! attrs
            Button.onClick onClick
        ]
