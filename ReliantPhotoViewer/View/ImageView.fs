namespace Reliant.Photo

open System

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Interactivity
open Avalonia.Layout
open Avalonia.Media
open Avalonia.VisualTree

open Aether.Operators

module ImageView =

    /// Creates a toolbar.
    let private createToolbar model dispatch =
        Toolbar.create [

                // switch to directory mode
            Button.createText "↩" "View folder contents" (fun _ ->
                dispatch SwitchToDirectory)

                // open file
            Button.createText "🗀" "Open image file" (
                FileSystemView.onSelectImage dispatch)

                // delete file
            Button.createText "🗑" "Delete file" (fun _ ->
                dispatch (MkImageMessage DeleteFile))

            match model with
                | Loaded loaded ->

                        // zoom to specific size
                    let curZoomScale = loaded ^. LoadedImage.ZoomScale_
                    match curZoomScale, loaded.SavedZoomOpt with   // to-do: why doesn't refactoring this work?
                        | 1.0, Some savedZoom ->
                            Button.createText
                                "🔍" "Zoom to previous size" (fun _ ->
                                ZoomTo (savedZoom, None)
                                    |> MkImageMessage
                                    |> dispatch)
                        | _ ->
                            let enabled = (curZoomScale <> 1.0)
                            Button.createTextImpl
                                "🔎" "Zoom to actual size" enabled (fun _ ->
                                ZoomTo (Zoom.actualSize, None)
                                    |> MkImageMessage
                                    |> dispatch)

                        // zoom scale
                    TextBlock.create [
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.text $"%0.1f{curZoomScale * 100.0}%%"
                    ]

                | _ -> ()
        ]

    /// Number of bytes in a kilobyte.
    let private kb = pown 2 10

    /// Number of bytes in a megabyte.
    let private mb = pown 2 20

    /// Number of bytes in a gigabyte.
    let private gb = pown 2 30

    /// Creates a status bar.
    let private createStatusBar model =
        StatusBar.create [
            match model with
                | Loaded loaded ->

                        // file name
                    let file = loaded.Situated.File
                    StatusBar.createSelectableTextBlock
                        file.Name "File name"

                        // file size
                    let sizeStr =
                        let nBytes = file.Length
                        if nBytes < kb then $"{nBytes} bytes"
                        elif nBytes < mb then $"%.2f{float nBytes / float kb} KiB"
                        elif nBytes < gb then $"%.2f{float nBytes / float mb} MiB"
                        else $"%.2f{float nBytes / float gb} GiB"
                    StatusBar.createSelectableTextBlock
                        sizeStr "File size"

                        // image dimensions
                    let dims = loaded.Bitmap.Size
                    StatusBar.createSelectableTextBlock
                        $"{dims.Width} x {dims.Height}" "Dimensions"

                        // date taken
                    match loaded.Situated.Situation.DateTakenOpt with
                        | Some dateTaken ->
                            StatusBar.createSelectableTextBlock
                                $"{dateTaken}" "Date taken"
                        | _ -> ()
                | _ -> ()
        ]

    /// Creates a browse panel, with or without a button.
    let private createBrowsePanel
        dock text tooltip resultOpt dispatch =

        let createButton message =
            Button.createText text tooltip (fun _ ->
                message
                    |> MkImageMessage
                    |> dispatch)

        DockPanel.create [
            DockPanel.width Button.buttonSize
            DockPanel.dock dock
            DockPanel.children [
                match resultOpt with
                    | Some ((file, result) : FileImageResult) ->
                        result
                            |> ImageMessage.ofResult file
                            |> createButton
                    | None -> ()
            ]
        ]

    /// Creates browse panels, with or without buttons.
    let private createBrowsePanels model dispatch =

            // get previous/next results
        let prevResultOpt, nextResultOpt =
            match model with
                | Situated_ situated ->
                    situated.Situation.PreviousResultOpt,
                    situated.Situation.NextResultOpt
                | _ -> None, None

        [
                // "previous image" button
            createBrowsePanel
                Dock.Left "◀" "Previous image"
                prevResultOpt
                dispatch
                :> IView

                // "next image" button
            createBrowsePanel
                Dock.Right "▶" "Next image"
                nextResultOpt
                dispatch
        ]

    /// Creates an image.
    let private createImage loaded =
        Image.create [

            Image.source loaded.Bitmap

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

    /// Gets the pointer position relative to the canvas.
    let inline private getPointerPosition<'t
        when 't :> RoutedEventArgs
        and 't : (member GetPosition : Canvas -> Point)>(args : 't) =
        (args.Source :?> Visual)
            .FindAncestorOfType<Canvas>()
            |> args.GetPosition

    /// Image canvas attributes.
    let private getImageCanvasAttributes loaded dispatch =

        [
            let image = createImage loaded
            Canvas.children [ image ]
            Canvas.clipToBounds true
            Canvas.background "Transparent"   // needed to trigger wheel events when the pointer is not over the image

                // zoom in/out
            Canvas.onPointerWheelChanged (fun args ->
                if args.Delta.Y <> 0 then   // y-coord: vertical wheel movement
                    let pointerPos = getPointerPosition<PointerWheelEventArgs> args
                    args.Handled <- true
                    (sign args.Delta.Y, pointerPos)
                        |> WheelZoom
                        |> MkImageMessage
                        |> dispatch)

                // panning
            if loaded ^. LoadedImage.ZoomScaleLock_ then
                Canvas.cursor Cursor.hand
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

                // zoom to actual size
            else
                Canvas.onDoubleTapped(fun args ->
                    args.Handled <- true
                    let pointerPos = getPointerPosition args
                    ZoomTo (Zoom.actualSize, Some pointerPos)
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
                    | _ -> Colors.Black
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
                createToolbar model dispatch
                createStatusBar model
                createBrowseDisplayPanel model dispatch
            ]
        ]

    /// Creates key bindings.
    let private createKeyBindings situated dispatch =

        let createBindings keys message =
            [
                for key in keys do
                    KeyBinding.create [
                        KeyBinding.key key
                        KeyBinding.execute (fun _ ->
                            message
                                |> MkImageMessage
                                |> dispatch)
                    ]
            ]

        let createResultBindings keys = function
            | Some ((file, result) : FileImageResult) ->
                result
                    |> ImageMessage.ofResult file
                    |> createBindings keys
            | None -> []

        Border.keyBindings [

                // previous image
            yield! createResultBindings
                [ Key.Left; Key.PageUp ]
                situated.Situation.PreviousResultOpt

                // next image
            yield! createResultBindings
                [ Key.Right; Key.PageDown ]
                situated.Situation.NextResultOpt

                // delete file
            yield! createBindings
                [ Key.Delete ]
                DeleteFile
        ]

    /// Creates an invisible border that handles key bindings.
    let private createKeyBindingBorder model dispatch child =
        Border.create [

            Border.focusable true
            Border.focusAdorner null
            Border.background "Transparent"

            match model with
                | Situated_ situated ->
                    createKeyBindings situated dispatch
                | _ -> ()

            Border.onLoaded (fun e ->
                let border = e.Source :?> Border   // grab focus
                border.Focus() |> ignore)

            Border.child (child : IView)
        ]

    /// Creates a view of the given model.
    let view model dispatch =
        createWorkPanel model dispatch
            |> createKeyBindingBorder model dispatch
