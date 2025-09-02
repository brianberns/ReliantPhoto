namespace Reliant.Photo

open System.IO
open System.Reflection

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Input
open Avalonia.Interactivity
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Media.Imaging
open Avalonia.Platform.Storage

module Resource =

    /// Gets a resource by name.
    let get name =
        Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream(
                $"ReliantPhotoViewer.Assets.{name : string}")

module Brush =

    /// Creates a brush.
    let private create (colorName : string) =
        Color.Parse colorName
            |> SolidColorBrush
            :> IBrush

    /// Dark gray.
    let darkGray = create "#181818"

    /// Light gray.
    let lightGray = create "#808080"

module Cursor =

    /// Wait cursor.
    let wait = new Cursor(StandardCursorType.Wait)

    /// Hand cursor.
    let hand = new Cursor(StandardCursorType.Hand)

module Icon =

    /// Creates an icon.
    let private create name =
        let stream =
            $"{name}_48dp_E3E3E3_FILL0_wght400_GRAD0_opsz48.png"
                |> Resource.get
        new Bitmap(stream)

    /// Delete.
    let delete = create "delete"

    /// Folder open.
    let folderOpen = create "folder_open"

    /// Switch access.
    let switchAccess = create "switch_access_shortcut"

    /// View real size.
    let viewRealSize = create "view_real_size"

    /// Zoom in map.
    let zoomInMap = create "zoom_in_map"

    /// Zoom out map.
    let zoomOutMap = create "zoom_out_map"

module Button =

    /// Button height and width.
    let buttonSize = 42

    /// Creates an icon button.
    let createIconImpl icon (tooltip : string) enabled onClick =
        Button.create [
            Button.content (
                Image.create [
                    Image.source icon
                    Image.stretch Stretch.Uniform
                ]
            )
            Button.tip tooltip
            Button.isEnabled enabled
            Button.height buttonSize
            Button.minWidth buttonSize
            Button.horizontalAlignment HorizontalAlignment.Stretch
            Button.verticalAlignment VerticalAlignment.Stretch
            Button.horizontalContentAlignment HorizontalAlignment.Center
            Button.verticalContentAlignment VerticalAlignment.Center
            Button.cornerRadius 4.0
            Button.background Brushes.Transparent
            Button.onClick onClick
        ]

    /// Creates an icon button.
    let createIcon icon tooltip onClick =
        createIconImpl icon tooltip true onClick

module Toolbar =

    /// Creates a toolbar.
    let create children =
        StackPanel.create [
            StackPanel.dock Dock.Top
            StackPanel.orientation Orientation.Horizontal
            StackPanel.spacing 5.0
            StackPanel.margin 5.0
            StackPanel.children children
        ]

module StatusBar =

    /// Creates a status bar.
    let create children =
        StackPanel.create [
            StackPanel.dock Dock.Bottom
            StackPanel.orientation Orientation.Horizontal
            StackPanel.spacing 5.0
            StackPanel.margin 5.0
            StackPanel.fontSize 12.0
            StackPanel.children children
        ]

    /// Creates a selectable text block.
    let createSelectableTextBlock text (tooltip : string) =
        SelectableTextBlock.create [
            SelectableTextBlock.text text
            SelectableTextBlock.background Brush.darkGray
            SelectableTextBlock.padding 5.0
            SelectableTextBlock.tip tooltip
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
