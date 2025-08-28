namespace Reliant.Photo

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
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
                Button.createText "↩" "View folder contents" (fun _ ->
                    dispatch SwitchToDirectory)

                    // open file
                Button.createText "🗀" "Open image file" (
                    FileSystemView.onSelectImage dispatch)

                    // delete file
                Button.createText "🗑" "Delete file" (fun _ ->
                    dispatch (MkImageMessage DeleteFile))

                    // zoom to actual size
                Button.createText "▦" "Zoom to actual size" (fun _ ->
                    dispatch (MkImageMessage ZoomToActualSize))

                    // zoom scale
                TextBlock.create [
                    TextBlock.verticalAlignment VerticalAlignment.Center
                    match model with
                        | Loaded loaded ->
                            let pct =
                                (loaded ^. LoadedImage.ZoomScale_) * 100.0
                            TextBlock.text $"%0.1f{pct}%%"
                        | _ -> ()
                ]
            ]
        ]

    /// Creates a browse panel, with or without a button.
    let private createBrowsePanel
        dock text tooltip fileOpt dispatch =
        DockPanel.create [
            DockPanel.width Button.buttonSize
            DockPanel.dock dock
            DockPanel.children [
                match fileOpt with
                    | Some file ->
                        Button.createText text tooltip (fun _ ->
                            file
                                |> ImageMessage.LoadImage
                                |> MkImageMessage
                                |> dispatch)
                    | None -> ()
            ]
        ]

    /// Creates browse panels, with or without buttons.
    let private createBrowsePanels model dispatch =

            // get previous/next files
        let prevFileOpt, nextFileOpt =
            match model ^. ImageModel.TrySituated_ with
                | Some situated ->
                    situated.PreviousFileOpt,
                    situated.NextFileOpt
                | _ -> None, None

        [
                // "previous image" button
            createBrowsePanel
                Dock.Left "◀" "Previous image"
                prevFileOpt
                dispatch
                :> IView

                // "next image" button
            createBrowsePanel
                Dock.Right "▶" "Next image"
                nextFileOpt
                dispatch
        ]

    /// Creates an image.
    let private createImage loaded =
        Image.create [

            let bitmap = loaded.Bitmap
            Image.source bitmap

            Image.init (fun image ->
                let mode =
                    ImageFile.getInterpolationMode
                        loaded.Situated.File
                RenderOptions.SetBitmapInterpolationMode(
                    image, mode))

                // image layout
            let imageSize =
                ImageLayout.getImageSize
                    loaded.BitmapSize
                    (loaded ^. LoadedImage.ZoomScale_)
            Image.width imageSize.Width
            Image.height imageSize.Height
            Image.left loaded.Offset.X
            Image.top loaded.Offset.Y
        ]

    /// Image canvas attributes.
    let private getImageCanvasAttributes loaded dispatch =

        /// Gets the pointer position relative to the canvas.
        let getPointerPosition (args : PointerEventArgs) =
            (args.Source :?> Visual)
                .FindAncestorOfType<Canvas>()
                |> args.GetPosition

        [
            let image = createImage loaded
            Canvas.children [ image ]
            Canvas.clipToBounds true
            Canvas.background "Transparent"   // needed to trigger wheel events when the pointer is not over the image

                // zoom
            Canvas.onPointerWheelChanged (fun args ->
                if args.Delta.Y <> 0 then   // y-coord: vertical wheel movement
                    let pointerPos = getPointerPosition args
                    args.Handled <- true
                    (sign args.Delta.Y, pointerPos)
                        |> WheelZoom
                        |> MkImageMessage
                        |> dispatch)

            if loaded.PanOpt.IsNone then

                    // start pan
                Canvas.onPointerPressed (fun args ->
                    args.Handled <- true
                    getPointerPosition args
                        |> PanStart
                        |> MkImageMessage
                        |> dispatch)

            else
                    // continue pan
                Canvas.onPointerMoved (fun args ->
                    args.Handled <- true
                    getPointerPosition args
                        |> PanMove
                        |> MkImageMessage
                        |> dispatch)

                    // end pan
                Canvas.onPointerReleased (fun args ->
                    args.Handled <- true
                    PanEnd
                        |> MkImageMessage
                        |> dispatch)
        ]

    /// Creates a canvas in which an image can be displayed.
    let private createImageCanvas model dispatch =
        Canvas.create [

                // canvas size
            Canvas.onSizeChanged (fun args ->
                args.Handled <- true
                args.NewSize
                    |> ContainerSized
                    |> MkImageMessage
                    |> dispatch)

                // canvas content
            match model with
                | Loaded loaded ->
                    yield! getImageCanvasAttributes loaded dispatch
                | _ -> ()   // trigger canvas size event on creation
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

    /// Creates a panel that can browse and display images.
    let private createBrowseDisplayPanel model dispatch =
        DockPanel.create [

                // background color
            let background =
                match model with
                    | Loaded loaded
                        when loaded ^. LoadedImage.ZoomScaleLock_ ->
                            Color.darkGray
                    | _ -> "Black"
            DockPanel.background background

            DockPanel.children [

                    // prev/next browse panels
                yield! createBrowsePanels model dispatch

                    // image canvas
                match model with
                    | LoadError errored ->
                        createErrorMessage errored.Message
                    | _ ->
                        createImageCanvas model dispatch
            ]
        ]

    /// Creates a panel that can work with images.
    let private createWorkPanel (model : ImageModel) dispatch =
        DockPanel.create [

            if model.IsUninitialized
                || model.IsInitialized then
                DockPanel.cursor Cursor.wait
                DockPanel.background "Transparent"   // needed to force the cursor change for some reason

            DockPanel.children [
                createToolbar Dock.Top model dispatch
                createBrowseDisplayPanel model dispatch
            ]
        ]

    /// Creates an invisible border that handles key bindings.
    let private createKeyBindingBorder situated dispatch child =
        Border.create [

            Border.focusable true
            Border.focusAdorner null
            Border.background "Transparent"

            Border.keyBindings [

                    // previous image
                match situated.PreviousFileOpt with
                    | Some file ->
                        for key in [ Key.Left; Key.PageUp ] do
                            KeyBinding.create [
                                KeyBinding.key key
                                KeyBinding.execute (fun _ ->
                                    dispatch (LoadImage file))
                            ]
                    | None -> ()

                    // next image
                match situated.NextFileOpt with
                    | Some file ->
                        for key in [ Key.Right; Key.PageDown ] do
                            KeyBinding.create [
                                KeyBinding.key key
                                KeyBinding.execute (fun _ ->
                                    dispatch (LoadImage file))
                            ]
                    | None -> ()

                    // delete file
                KeyBinding.create [
                    KeyBinding.key Key.Delete
                    KeyBinding.execute (fun _ ->
                        dispatch (MkImageMessage DeleteFile))
                ]
            ]

            Border.onLoaded (fun e ->
                let border = e.Source :?> Border   // grab focus
                border.Focus() |> ignore)

            Border.child (child : IView)
        ]

    /// Creates a view of the given model.
    let view model dispatch =
        let panel = createWorkPanel model dispatch
        match model ^. ImageModel.TrySituated_ with
            | Some situated ->
                createKeyBindingBorder situated dispatch panel
                    :> IView
            | _ -> panel
