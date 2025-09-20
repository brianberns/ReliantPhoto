namespace Reliant.Photo

open System
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
    let darkGray = create "#2D2D30"

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

    /// Arrow left.
    let arrowLeft = create "keyboard_arrow_left"

    /// Arrow right.
    let arrowRight = create "keyboard_arrow_right"

    /// Delete.
    let delete = create "delete"

    /// Fit screen.
    let fitScreen = create "fit_screen"

    /// Folder open.
    let folderOpen = create "folder_open"

    /// Full screen.
    let fullScreen = create "fullscreen"

    /// Image.
    let image = create "image"

    /// Folder eye.
    let viewFolder = create "dashboard_2"

    /// View real size.
    let viewRealSize = create "view_real_size"

module Button =

    /// Button height and width.
    let buttonSize = 42

    /// Creates an icon button.
    let createIconImpl
        icon (tooltip : string) attrs onClick =
        Button.create [
            Button.content (
                Image.create [
                    Image.source icon
                    Image.stretch Stretch.Uniform
                ]
            )
            Button.tip tooltip
            Button.height buttonSize
            Button.minWidth buttonSize
            Button.margin (5.0, 0.0)
            Button.horizontalAlignment HorizontalAlignment.Stretch
            Button.verticalAlignment VerticalAlignment.Stretch
            Button.horizontalContentAlignment HorizontalAlignment.Center
            Button.verticalContentAlignment VerticalAlignment.Center
            Button.cornerRadius 4.0
            Button.background Brushes.Transparent
            Button.focusable false
            yield! attrs
            Button.onClick onClick
        ]

    /// Creates an icon button.
    let createIcon icon tooltip onClick =
        createIconImpl
            icon tooltip [] onClick

module Toolbar =

    /// Creates a toolbar.
    let create children =
        DockPanel.create [
            DockPanel.dock Dock.Top
            DockPanel.margin 5.0
            DockPanel.lastChildFill false
            DockPanel.children children
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
            SelectableTextBlock.focusable false
            if not (String.IsNullOrWhiteSpace(tooltip)) then
                SelectableTextBlock.tip tooltip
        ]

module FileSystemView =

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
                folders[0].TryGetLocalPath()
                    |> Option.ofObj
                    |> Option.iter (
                        DirectoryInfo
                            >> LoadDirectory
                            >> dispatch)
        } |> Async.StartImmediate

    /// Allows user to select an image.
    let onSelectImage dispatch args =
        let topLevel =
            (args : RoutedEventArgs).Source
                :?> Control
                |> TopLevel.GetTopLevel
        async {
            let! files =
                let options = FilePickerOpenOptions()
                topLevel
                    .StorageProvider
                    .OpenFilePickerAsync(options)
                    |> Async.AwaitTask
            if files.Count > 0 then
                files[0].TryGetLocalPath()
                    |> Option.ofObj
                    |> Option.iter (
                        FileInfo
                            >> LoadImage
                            >> dispatch)
        } |> Async.StartImmediate
