namespace Reliant.Photo

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media

module View =

    /// Button height and width.
    let private browseButtonSize = 50

    /// Creates a browse button.
    let private createBrowseButton text callback =
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
            Button.height browseButtonSize
            Button.horizontalAlignment HorizontalAlignment.Stretch
            Button.verticalAlignment VerticalAlignment.Stretch
            Button.horizontalContentAlignment HorizontalAlignment.Center
            Button.verticalContentAlignment VerticalAlignment.Center
            Button.onClick callback
        ]

    /// Creates a browse panel, with or without a button.
    let private createBrowsePanel dock text hasButton callback =
        DockPanel.create [
            DockPanel.width browseButtonSize
            DockPanel.dock dock
            DockPanel.children [
                if hasButton then
                    createBrowseButton text callback
            ]
        ]

    /// Creates a panel that can display images.
    let private createImagePanel state dispatch =
        DockPanel.create [
            DockPanel.children [

                createBrowsePanel
                    Dock.Left "◀"
                    state.HasPreviousImage
                    (fun _ -> dispatch PreviousImage)

                createBrowsePanel
                    Dock.Right "▶"
                    state.HasNextImage
                    (fun _ -> dispatch NextImage)

                match state.ImageOpt with
                    | Some image ->
                        Image.create [
                            Image.source image
                        ]
                    | None ->
                        TextBlock.create []
            ]
        ]

    /// Creates an invisible border that handles key bindings.
    let private createKeyBindingBorder state dispatch child =
        Border.create [

            Border.focusable true
            Border.background "Transparent"

            Border.keyBindings [
                if state.HasPreviousImage then
                    KeyBinding.create [
                        KeyBinding.key Key.Left
                        KeyBinding.execute (fun _ ->
                            dispatch PreviousImage)
                    ]
                if state.HasNextImage then
                    KeyBinding.create [
                        KeyBinding.key Key.Right
                        KeyBinding.execute (fun _ ->
                            dispatch NextImage)
                    ]
            ]

            Border.onLoaded (fun e ->
                let border = e.Source :?> Border   // grab focus
                border.Focus() |> ignore)

            Border.child (child : IView)
        ]

    /// Creates a view of the given state.
    let view state dispatch =
        createImagePanel state dispatch
            |> createKeyBindingBorder state dispatch
