namespace Reliant.Photo

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Media.Imaging
open Avalonia.VisualTree

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

                    // switch to directory mode
                Button.createText "↩" (fun _ ->
                    dispatch SwitchToDirectory)

                    // open file
                Button.createText "🗀" (
                    FileSystemView.onSelectImage dispatch)

                    // zoom scale
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

    /// Creates browse panels, with or without buttons.
    let private createBrowsePanels model dispatch =

            // browse buttons?
        let hasPrev, hasNext =
            model ^. ImageModel.TryBrowsed_
                |> Option.map (fun browsed ->
                    browsed.HasPreviousImage,
                    browsed.HasNextImage)
                |> Option.defaultValue (false, false)

        [
                // "previous image" button
            createBrowsePanel
                Dock.Left "◀"
                hasPrev
                PreviousImage
                dispatch
                :> IView

                // "next image" button
            createBrowsePanel
                Dock.Right "▶"
                hasNext
                NextImage
                dispatch
        ]

    /// Creates an image.
    let private createImage loaded =
        Image.create [

            let bitmap = loaded.Bitmap
            Image.source bitmap

                // ensure clean edges in image
            Image.init (fun image ->
                RenderOptions.SetBitmapInterpolationMode(
                    image,
                    BitmapInterpolationMode.None))

                // image layout
            let imageSize =
                ImageLayout.getImageSize
                    loaded.BitmapSize loaded.ZoomScale
            Image.width imageSize.Width
            Image.height imageSize.Height
            Image.left loaded.Offset.X
            Image.top loaded.Offset.Y
        ]

    /// Gets the pointer position relative to the canvas.
    let private getPointerPosition<'t
        when 't :> Visual
            and 't : not struct> (args : PointerEventArgs) =
        let visual = args.Source :?> Visual
        let container = visual.FindAncestorOfType<'t>()
        args.GetPosition(container)

    /// Creates a zoomable image.
    let private createZoomableImage model dispatch =

        Canvas.create [

            Canvas.onSizeChanged (fun args ->
                args.Handled <- true
                args.NewSize
                    |> ContainerSized
                    |> MkImageMessage
                    |> dispatch)

            match model with
                | Loaded loaded ->

                    let image = createImage loaded
                    Canvas.children [ image ]
                    Canvas.clipToBounds true
                    Canvas.background "Transparent"   // needed to trigger wheel events when the pointer is not over the image

                    Canvas.onPointerWheelChanged (fun args ->
                        let pointerPos = getPointerPosition<Canvas> args
                        args.Handled <- true
                        (sign args.Delta.Y, pointerPos)   // y-coord: vertical wheel movement
                            |> WheelZoom
                            |> MkImageMessage
                            |> dispatch)

                    if loaded.PanOpt.IsNone then

                        Canvas.onPointerPressed (fun args ->
                            let pointerPos = getPointerPosition<Canvas> args
                            args.Handled <- true
                            pointerPos
                                |> PanStart
                                |> MkImageMessage
                                |> dispatch)

                    else

                        Canvas.onPointerMoved (fun args ->
                            let pointerPos = getPointerPosition<Canvas> args
                            printfn $"{pointerPos}"
                            args.Handled <- true
                            pointerPos
                                |> PanMove
                                |> MkImageMessage
                                |> dispatch)

                        Canvas.onPointerReleased (fun args ->
                            args.Handled <- true
                            PanEnd
                                |> MkImageMessage
                                |> dispatch)

                | _ -> ()
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
    let private createImagePanel (model : ImageModel) dispatch =
        DockPanel.create [

            if model.IsUninitialized
                || model.IsInitialized
                || model.IsBrowsed then
                DockPanel.cursor Cursor.wait
                DockPanel.background "Transparent"   // needed to force the cursor change for some reason

            DockPanel.children [

                    // toolbar
                createToolbar Dock.Top model dispatch

                    // prev/next browse panels
                yield! createBrowsePanels model dispatch

                    // image or error message
                match model with
                    | BrowseError errored ->
                        createErrorMessage errored.Message
                    | LoadError errored ->
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

            match model ^. ImageModel.TryBrowsed_ with
                | Some browsed ->
                    Border.keyBindings [
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
                | None -> ()

            Border.onLoaded (fun e ->
                let border = e.Source :?> Border   // grab focus
                border.Focus() |> ignore)

            Border.child (child : IView)
        ]

    /// Creates a view of the given model.
    let view model dispatch =
        createImagePanel model dispatch
            |> createKeyBindingBorder model dispatch
