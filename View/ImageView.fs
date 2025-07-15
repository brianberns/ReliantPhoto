namespace Reliant.Photo

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Media.Imaging

open Aether.Operators

module ImageView =

    /// Creates a toolbar.
    let private createToolbar dock model dispatch =
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
                    match model with
                        | Loaded loaded ->
                            let pct = loaded.ZoomScale * 100.0
                            TextBlock.text $"%0.1f{pct}%%"
                        | _ -> ()
                ]
            ]
        ]

    /// Creates a browse panel, with or without a button.
    let private createBrowsePanel
        dock text hasButton message dispatch =
        DockPanel.create [
            DockPanel.width Button.buttonSize
            DockPanel.dock dock
            DockPanel.children [
                if hasButton then
                    Button.createText text (fun _ ->
                        MkImageMessage message |> dispatch)
            ]
        ]

    /// Creates a zoomable image.
    let private createZoomableImage
        (dpiScale : float) model dispatch =

        let image =
            Image.create [

                    // ensure clean edges in image (make sure this is present the first time through, even when there are no other attributes)
                Image.init (fun image ->
                    RenderOptions.SetBitmapInterpolationMode(
                        image,
                        BitmapInterpolationMode.None))

                match model with
                    | Loaded loaded ->

                        Image.source loaded.Bitmap

                        Canvas.left 0.0
                        Canvas.top 0.0

                        let size =
                            loaded.Bitmap.Size
                                * loaded.ZoomScale
                                / dpiScale
                        Image.width size.Width
                        Image.height size.Height

                    | _ -> ()
            ]

        Canvas.create [
            Canvas.children [ image ]
            Canvas.clipToBounds true
            Canvas.background "Transparent"   // needed to trigger wheel events when the pointer is not over the image

            Canvas.onSizeChanged (fun args ->
                args.Handled <- true
                args.NewSize
                    |> ContainerSized
                    |> MkImageMessage
                    |> dispatch)

            Canvas.onPointerWheelChanged (fun args ->
                let pointerPos =
                    args.GetPosition(args.Source :?> Visual)
                args.Handled <- true
                (sign args.Delta.Y, pointerPos)   // y-coord: vertical wheel movement
                    |> WheelZoom
                    |> MkImageMessage
                    |> dispatch)
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
        dpiScale (model : ImageModel) dispatch =
        DockPanel.create [
                
            if model.IsBrowsed || model.IsContained then
                DockPanel.cursor Cursor.wait
                DockPanel.background "Transparent"   // needed to force the cursor change for some reason

            DockPanel.children [

                    // toolbar
                createToolbar Dock.Top model dispatch

                    // browse buttons?
                match model ^. ImageModel.TryBrowsed_ with
                    | Some browsed ->

                            // "previous image" button
                        createBrowsePanel
                            Dock.Left "◀"
                            browsed.HasPreviousImage
                            PreviousImage
                            dispatch

                            // "next image" button
                        createBrowsePanel
                            Dock.Right "▶"
                            browsed.HasNextImage
                            NextImage
                            dispatch

                    | None -> ()

                    // image?
                match model with
                    | BrowseError errored ->
                        createErrorMessage errored.Message
                    | LoadError errored ->
                        createErrorMessage errored.Message
                    | _ ->
                        createZoomableImage
                            dpiScale model dispatch
            ]
        ]

    /// Creates an invisible border that handles key bindings.
    let private createKeyBindingBorder
        (model : ImageModel) dispatch child =
        Border.create [

            Border.focusable true
            Border.background "Transparent"

            if not model.IsBrowseError then
                Border.keyBindings [
                    let browsed = model ^. ImageModel.Browsed_
                    if browsed.HasPreviousImage then
                        for key in [ Key.Left; Key.PageUp ] do
                            KeyBinding.create [
                                KeyBinding.key key
                                KeyBinding.execute (fun _ ->
                                    dispatch (MkImageMessage PreviousImage))
                            ]
                    if browsed.HasNextImage then
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
    let view dpiScale model dispatch =
        createImagePanel dpiScale model dispatch
            |> createKeyBindingBorder model dispatch
