namespace Reliant.Photo

open System.IO

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Input
open Avalonia.Interactivity
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Platform.Storage

module Color =

    /// Dark gray.
    let darkGray = Color.Parse "#181818"

module Cursor =

    /// Wait cursor.
    let wait = new Cursor(StandardCursorType.Wait)

    /// Hand cursor.
    let hand = new Cursor(StandardCursorType.Hand)

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

module Button =

    /// Button height and width.
    let buttonSize = 50

    /// Creates a text button.
    let createTextImpl text (tooltip : string) enabled onClick =
        Button.create [
            Button.content (
                Viewbox.create [
                    Viewbox.stretch Stretch.Uniform
                    Viewbox.stretchDirection StretchDirection.Both
                    Viewbox.child (
                        TextBlock.create [
                            TextBlock.text text
                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                            TextBlock.verticalAlignment VerticalAlignment.Center
                            TextBlock.textWrapping TextWrapping.NoWrap
                        ]
                    )
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
            Button.onClick onClick
        ]

    /// Creates a text button.
    let createText text tooltip onClick =
        createTextImpl text tooltip true onClick

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
