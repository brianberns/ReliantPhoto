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
    let private createToolbar dock zoomPercent dispatch =
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
                    if zoomPercent > 0.0 then
                        TextBlock.text $"%0.1f{zoomPercent}%%"
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

    /// Creates a zoomable image.
    let private createZoomableImage
        (osScale : float) source zoomScale zoomOrigin dispatch =
        Border.create [
            Border.clipToBounds true
            Border.child (
                Image.create [
                    Image.source source
                    Image.renderTransform (
                        ScaleTransform(zoomScale, zoomScale))
                    Image.renderTransformOrigin zoomOrigin
                    Image.onPointerWheelChanged (fun e ->
                        let pointerPos = e.GetPosition(e.Source :?> Visual)
                        e.Handled <- true
                        (sign e.Delta.Y, pointerPos)   // y-coord: vertical wheel movement
                            |> WheelZoom
                            |> MkImageMessage
                            |> dispatch)
                    Image.onSizeChanged (fun args ->
                        (args.NewSize * osScale)       // undo any OS-level scaling
                            |> ImageSized
                            |> MkImageMessage
                            |> dispatch)
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
    let private createImagePanel osScale model dispatch =

        let zoomPercent =
            match model.Result with
                | Ok bitmap when bitmap.Size.Width > 0 ->
                    float model.ImageSize.Width
                        * model.ZoomScale
                        * osScale
                        * 100.0
                        / float bitmap.Size.Width
                | _ -> 0.0

        DockPanel.create [

            if model.IsLoading then
                DockPanel.cursor Cursor.wait
                DockPanel.background "Transparent"   // needed to force the cursor change for some reason

            DockPanel.children [

                createToolbar
                    Dock.Top
                    zoomPercent
                    dispatch

                createBrowsePanel
                    Dock.Left "◀"
                    model.HasPreviousImage
                    (fun _ -> dispatch (MkImageMessage PreviousImage))

                createBrowsePanel
                    Dock.Right "▶"
                    model.HasNextImage
                    (fun _ -> dispatch (MkImageMessage NextImage))

                match model.Result with
                    | Ok bitmap ->
                        createZoomableImage
                            osScale
                            bitmap
                            model.ZoomScale
                            model.ZoomOrigin
                            dispatch
                    | Error str ->
                        createErrorMessage str
            ]
        ]

    /// Creates an invisible border that handles key bindings.
    let private createKeyBindingBorder model dispatch child =
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
    let view osScale model dispatch =
        createImagePanel osScale model dispatch
            |> createKeyBindingBorder model dispatch
