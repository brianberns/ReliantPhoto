namespace Reliant.Photo

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media

module ImageView =

    /// Creates a toolbar.
    let private createToolbar dock model dispatch =

        let zoomScaleOpt =
            match model with
                | Displayed displayed ->
                    displayed
                        |> DisplayedImage.getImageScale
                        |> Some
                | Zoomed zoomed ->
                    Some zoomed.Scale
                | _ -> None

        StackPanel.create [
            StackPanel.dock dock
            StackPanel.orientation Orientation.Horizontal
            StackPanel.spacing 5.0
            StackPanel.margin 5.0
            StackPanel.children [
                Button.createText "↩" (fun _ ->
                    dispatch SwitchToDirectory)
                Button.createText "🗀" (
                    FileSystemView.onSelectImage dispatch)
                TextBlock.create [
                    TextBlock.verticalAlignment VerticalAlignment.Center
                    match zoomScaleOpt with
                        | Some zoomScale ->
                            TextBlock.text $"%0.1f{zoomScale * 100.0}%%"
                        | None -> ()
                ]
            ]
        ]

    /// Creates a browse panel, with or without a button.
    let private createBrowsePanel dock text hasButton callback =
        DockPanel.create [
            DockPanel.width Button.buttonSize
            DockPanel.dock dock
            DockPanel.children [
                if hasButton then
                    Button.createText text callback
            ]
        ]

    let private onPointerWheelChanged (args : PointerWheelEventArgs) =
        let pointerPos = args.GetPosition(args.Source :?> Visual)
        args.Handled <- true
        (sign args.Delta.Y, pointerPos)   // y-coord: vertical wheel movement
            |> WheelZoom
            |> MkImageMessage

    let private onSizeChanged (args : SizeChangedEventArgs) =
        args.Handled <- true
        args.NewSize
            |> ImageSized
            |> MkImageMessage

    /// Creates a zoomable image.
    let private createZoomableImage model dispatch =
        Border.create [
            Border.clipToBounds true
            Border.child (
                Image.create [

                    match model with

                        | Loaded loaded ->
                            Image.source loaded.Bitmap
                            Image.onPointerWheelChanged (
                                onPointerWheelChanged >> dispatch)
                            Image.onSizeChanged (
                                onSizeChanged >> dispatch)

                        | Displayed displayed ->
                            Image.source displayed.Bitmap
                            Image.onPointerWheelChanged (
                                onPointerWheelChanged >> dispatch)
                            Image.onSizeChanged (
                                onSizeChanged >> dispatch)

                        | Zoomed zoomed ->
                            Image.source zoomed.Bitmap
                            Image.onPointerWheelChanged (
                                onPointerWheelChanged >> dispatch)
                            Image.onSizeChanged (
                                onSizeChanged >> dispatch)

                            let zoomScale =
                                let imageScale =
                                    DisplayedImage.getImageScale zoomed.Displayed
                                zoomed.Scale / imageScale
                            Image.renderTransform (
                                ScaleTransform(zoomScale, zoomScale))
                            Image.renderTransformOrigin zoomed.Origin

                        | _ -> ()
                ]
            )
        ]

    /// Creates an error message.
    let private createErrorMessage str =
        TextBlock.create [
            TextBlock.text str
            TextBlock.horizontalAlignment
                HorizontalAlignment.Center
            TextBlock.verticalAlignment
                VerticalAlignment.Center
            TextBlock.textAlignment
                TextAlignment.Center
        ]

    /// Creates a panel that can display images.
    let private createImagePanel
        (model : ImageModel) dispatch =
        DockPanel.create [

            if model.IsBrowsed then
                DockPanel.cursor Cursor.wait
                DockPanel.background "Transparent"   // needed to force the cursor change for some reason

            DockPanel.children [

                    // toolbar
                createToolbar Dock.Top model dispatch

                    // "previous image" button
                createBrowsePanel
                    Dock.Left "◀"
                    model.HasPreviousImage
                    (fun _ -> dispatch (MkImageMessage PreviousImage))

                    // "next image" button
                createBrowsePanel
                    Dock.Right "▶"
                    model.HasNextImage
                    (fun _ -> dispatch (MkImageMessage NextImage))

                    // image?
                match model with
                    | Errored errored ->
                        createErrorMessage errored.Message
                    | _ ->
                        createZoomableImage model dispatch
            ]
        ]

    /// Creates an invisible border that handles key bindings.
    let private createKeyBindingBorder
        (model : ImageModel) dispatch child =
        Border.create [

            Border.focusable true
            Border.background "Transparent"

            Border.keyBindings [
                if model.HasPreviousImage then
                    for key in [ Key.Left; Key.PageUp ] do
                        KeyBinding.create [
                            KeyBinding.key key
                            KeyBinding.execute (fun _ ->
                                dispatch (MkImageMessage PreviousImage))
                        ]
                if model.HasNextImage then
                    for key in [ Key.Right; Key.PageDown ] do
                        KeyBinding.create [
                            KeyBinding.key key
                            KeyBinding.execute (fun _ ->
                                dispatch (MkImageMessage NextImage))
                        ]
            ]

            Border.onLoaded (fun e ->
                let border = e.Source :?> Border   // grab focus
                border.Focus() |> ignore)

            Border.child (child : IView)
        ]

    /// Creates a view of the given model.
    let view model dispatch =
        createImagePanel model dispatch
            |> createKeyBindingBorder model dispatch
