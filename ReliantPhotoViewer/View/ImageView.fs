namespace Reliant.Photo

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Interactivity
open Avalonia.Layout
open Avalonia.Media
open Avalonia.VisualTree

open Aether.Operators

module ImageView =

    /// Creates a slider that can be used to adjust the zoom scale.
    let private createZoomSlider
        zoomScale defaultZoomScale dispatch =
        Component.create($"zoomSlider/{defaultZoomScale}", fun ctx ->
            let pointerPressed = ctx.useState false
            Slider.create [
                Slider.minimum (log defaultZoomScale)
                Slider.maximum (log ImageLayout.zoomScaleCeiling)
                Slider.value (log zoomScale)
                Slider.tip "Adjust zoom scale"
                Slider.focusable false
                Slider.width 150.0
                Slider.margin (5.0, 0.0)
                Slider.verticalAlignment VerticalAlignment.Center

                    // force slider to fit in toolbar (see https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.Themes.Fluent/Controls/Slider.xaml)
                let sliderHeight = Button.buttonSize
                let thumbSize = 18.0
                let contentMargin =
                    GridLength ((sliderHeight - thumbSize) / 2.0)
                Slider.height sliderHeight
                Slider.onLoaded (fun args ->
                    let slider = args.Source :?> Slider
                    slider.Resources["SliderHorizontalThumbHeight"] <- thumbSize
                    slider.Resources["SliderHorizontalThumbWidth"] <- thumbSize
                    slider.Resources["SliderPreContentMargin"] <- contentMargin
                    slider.Resources["SliderPostContentMargin"] <- contentMargin)

                    // zoom iff user is dragging the thumb
                Slider.onPointerPressed(fun _ ->
                    pointerPressed.Set true)
                Slider.onPointerReleased(fun _ ->
                    pointerPressed.Set false)
                Slider.onValueChanged (fun value ->
                    if pointerPressed.Current then
                        exp value
                            |> ZoomTo
                            |> MkImageMessage
                            |> dispatch)
            ]
        )

    /// Creates a toolbar.
    let private createToolbar model dispatch =
        Toolbar.create [

                // switch to directory mode
            Button.createIcon
                Icon.viewFolder
                "View thumbnails"
                (fun _ -> dispatch SwitchToDirectory)

                // open image
            Button.createIcon
                Icon.image
                "Open image file"
                (FileInfo.onPick (LoadImage >> dispatch))

                // delete file
            Button.createIconImpl
                Icon.delete
                "Delete file"
                [
                    (model ^. ImageModel.TrySituation_)
                        .IsSome
                        |> Button.isVisible   // don't allow file deletion before it's been situated
                    Button.dock Dock.Right
                ]
                (fun _ -> dispatch (MkImageMessage DeleteFile))

            match model with
                | Loaded loaded ->

                        // zoom to actual size
                    let zoomScale = loaded ^. LoadedImage.ZoomScale_
                    Button.createIconImpl
                        Icon.viewRealSize
                        "Zoom to actual size"
                        [ Button.isEnabled (zoomScale <> 1.0) ]
                        (fun _ ->
                            ZoomToActualSize None
                                |> MkImageMessage
                                |> dispatch)

                        // zoom to fit container
                    let defaultZoomScale =   // to-do: move to model?
                        let containerSize =
                            loaded ^. LoadedImage.ContainerSize_
                        ImageLayout.getDefaultZoomScale
                            containerSize
                            loaded.BitmapSize
                    Button.createIconImpl
                        Icon.fitScreen
                        "Zoom to fit screen"
                        [ Button.isEnabled
                            (defaultZoomScale < 1.0
                                && defaultZoomScale <> zoomScale) ]
                        (fun _ -> MkImageMessage ZoomToFit |> dispatch)

                        // adjust zoom scale
                    createZoomSlider
                        zoomScale
                        defaultZoomScale
                        dispatch

                        // zoom scale
                    TextBlock.create [
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.text $"%0.1f{zoomScale * 100.0}%%"
                        TextBlock.tip "Zoom scale"
                        TextBlock.margin (5.0, 0.0)
                    ]

                        // full screen on
                    Button.createIcon
                        Icon.fullScreen
                        "Full screen"
                        (fun _ ->
                            FullScreen true
                                |> MkImageMessage
                                |> dispatch)

                | _ -> ()
        ]

    /// Number of bytes in a kilobyte.
    let private kb = pown 2 10

    /// Number of bytes in a megabyte.
    let private mb = pown 2 20

    /// Number of bytes in a gigabyte.
    let private gb = pown 2 30

    module private Decimal =

        /// Converts a decimal to a string.
        let toString (n : decimal) =
            n.ToString("0.##")

    /// Creates status bar items from the given optional
    /// property.
    let private createPropertyStatusItems
        propertyOpt tooltips mapping =
        propertyOpt
            |> Option.map (fun property ->
                ((mapping property), tooltips)
                    ||> Seq.map2 (fun text tooltip ->
                        StatusBar.createSelectableTextBlock
                            text tooltip
                            :> IView))
            |> Option.defaultValue Seq.empty

    /// Creates EXIF status bar items.
    let private createExifStatusItems (exif : ExifMetadata) =
        [
                // date taken
            yield! createPropertyStatusItems
                exif.DateTakenOpt
                [ "Date taken" ]
                (string >> List.singleton)

                // camera make
            yield! createPropertyStatusItems
                exif.CameraMakeOpt
                [ "Camera make" ]
                List.singleton

                // camera model
            yield! createPropertyStatusItems
                exif.CameraModelOpt
                [ "Camera model" ]
                List.singleton

                // f-stop
            yield! createPropertyStatusItems
                exif.FStopOpt
                [ "F-stop" ]
                (fun fStop ->
                    [ $"f/{Decimal.toString fStop}" ])

                // exposure time
            yield! createPropertyStatusItems
                exif.ExposureTimeOpt
                [ "Exposure time" ]
                (fun time ->
                    let str =
                        if time < 1m then
                            $"1/{Decimal.toString (1m/time)}"
                        else Decimal.toString time
                    [ $"{str} sec." ])

                // exposure compensation
            yield! createPropertyStatusItems
                exif.ExposureCompensationOpt
                [ "Exposure compensation" ]
                (fun ev ->
                    let sign =
                        if ev > 0m then "+" else ""
                    [ $"{sign}{Decimal.toString ev} EV"])

                // ISO rating
            yield! createPropertyStatusItems
                exif.IsoRatingOpt
                [ "ISO speed rating" ]
                (fun iso ->
                    [ $"ISO {Decimal.toString iso}" ])

                // focal length
            yield! createPropertyStatusItems
                exif.FocalLengthOpt
                [
                    "Focal length"
                    "Full-frame focal length equivalent"
                ]
                (fun len ->
                    [
                        $"{Decimal.toString len} mm"

                        match exif.FocalLengthFullFrameOpt with
                            | Some len35 when len35 <> len ->
                                $"{Decimal.toString len35} mm"
                            | _ -> ()
                    ])
        ]

    /// Creates status items for the given file.
    let private createFileStatusItems situated =
        [
                // file name
            let file = situated.File
            StatusBar.createSelectableTextBlock
                file.Name file.FullName
                :> IView
        ]

    /// Creates bitmap status bar items.
    let private createBitmapStatusItems (bitmap : Imaging.Bitmap) =
        [
                // bitmap dimensions
            let dims = bitmap.Size
            StatusBar.createSelectableTextBlock
                $"{dims.Width} x {dims.Height}" "Dimensions"
                :> IView
        ]

    /// Creates situation status bar items.
    let private createSituationStatusItems situation =
        [
                // file size
            match situation.FileLengthOpt with
                | Some nBytes ->
                    let sizeStr =
                        if nBytes < kb then $"{nBytes} bytes"
                        elif nBytes < mb then
                            $"%.2f{float nBytes / float kb} KiB"
                        elif nBytes < gb then
                            $"%.2f{float nBytes / float mb} MiB"
                        else $"%.2f{float nBytes / float gb} GiB"
                    StatusBar.createSelectableTextBlock
                        sizeStr "File size"
                        :> IView
                | None -> ()

                // EXIF
            match situation.ExifMetadataOpt with
                | Some exif -> yield! createExifStatusItems exif
                | None -> ()
        ]

    /// Creates situation status bar items.
    let private createSituationOptStatusItems situationOpt =
        [
            match situationOpt with
                | Some situation ->
                    yield! createSituationStatusItems situation
                | None -> ()
        ]

    /// Creates a status bar.
    let private createStatusBar model =
        StatusBar.create [
            match model with

                | Initialized _
                | Sized _ ->
                    StatusBar.createSelectableTextBlock   // placeholder to ensure status bar has a constant height
                        "Loading..." ""
                        :> IView

                | ImageModel.Situated situated ->
                    assert(situated.SituationOpt.IsNone)
                    yield! createFileStatusItems situated

                | Loaded loaded ->
                    yield! createFileStatusItems loaded.Situated
                    yield! createBitmapStatusItems loaded.Bitmap
                    yield! createSituationOptStatusItems
                        loaded.Situated.SituationOpt

                | LoadError errored ->
                    yield! createFileStatusItems errored.Situated
                    yield! createSituationOptStatusItems
                        errored.Situated.SituationOpt

                | Empty _ ->
                    StatusBar.createSelectableTextBlock   // placeholder to ensure status bar has a constant height
                        "No file" ""
        ]

    /// Creates a browse panel, with or without a button.
    let private createBrowsePanel
        dock icon tooltip resultOpt dispatch =

        let createButton message =
            Button.createIcon icon tooltip (fun _ ->
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
                | Situation_ situation ->
                    situation.PreviousResultOpt,
                    situation.NextResultOpt
                | _ -> None, None

        [
                // "previous image" button
            createBrowsePanel
                Dock.Left Icon.arrowLeft "Previous image"
                prevResultOpt dispatch
                :> IView

                // "next image" button
            createBrowsePanel
                Dock.Right Icon.arrowRight "Next image"
                nextResultOpt dispatch
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
            Canvas.background Brushes.Transparent   // needed to trigger wheel events when the pointer is not over the image

                // zoom in/out
            Canvas.onPointerWheelChanged (fun args ->
                if args.Delta.Y <> 0 then   // y-coord: vertical wheel movement
                    let pointerPos = getPointerPosition<PointerWheelEventArgs> args
                    args.Handled <- true
                    (sign args.Delta.Y, pointerPos)
                        |> WheelZoom
                        |> MkImageMessage
                        |> dispatch)

                // double-click
            if loaded ^. LoadedImage.ZoomScale_ = 1.0 then

                    // zoom to fit container
                Canvas.onDoubleTapped(fun args ->
                    args.Handled <- true
                    ZoomToFit
                        |> MkImageMessage
                        |> dispatch)
            else
                    // zoom to actual size
                Canvas.onDoubleTapped(fun args ->
                    args.Handled <- true
                    let pointerPos = getPointerPosition args
                    ZoomToActualSize (Some pointerPos)
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
                        when loaded ^. LoadedImage.ZoomScaleLock_ 
                            && loaded ^. LoadedImage.ZoomScale_ <> 1.0 ->
                            Brush.darkGray
                    | _ -> Brushes.Black
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

    /// Creates a view of the given model.
    let view (model : ImageModel) dispatch =
        DockPanel.create [

                // loading?
            if model.IsInitialized || model.IsSized then
                DockPanel.cursor Cursor.wait
                DockPanel.background Brushes.Transparent   // needed to force the cursor change for some reason

                // full screen?
            let fullScreen =
                match model with
                    | Sized_ sized ->
                        sized.FullScreen
                    | _ -> false

                // content
            DockPanel.children [
                if not fullScreen then
                    createToolbar model dispatch
                    createStatusBar model
                createBrowseDisplayPanel model dispatch
            ]
        ]
