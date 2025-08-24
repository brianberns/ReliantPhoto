namespace Reliant.Photo

open System.IO

open Elmish

open Avalonia
open Avalonia.Media.Imaging

open Aether.Operators

/// Messages that can update the image model.
type ImageMessage =

    /// Size of the image container has been set or updated.
    | ContainerSized of Size

    /// Load image from file, if possible.
    | LoadImage of FileInfo

    /// Image has been loaded.
    | ImageLoaded of FileInfo * Bitmap

    /// Load error occurred.
    | HandleLoadError of string

    /// Browse to previous/next image in directory, if possible.
    | Browse of int (*increment*)

    /// Pointer wheel position has changed.
    | WheelZoom of int (*sign*) * Point (*pointer position*)

    /// Zoom image to actual size.
    | ZoomToActualSize

    /// Pointer pan has started.
    | PanStart of Point

    /// Pointer pan has moved.
    | PanMove of Point

    /// Pointer pan has ended.
    | PanEnd

module ImageMessage =

    /// Creates a command to load an image from the given file.
    /// This is an asynchronous command to allow the image
    /// view to create a container before loading its first
    /// image.
    let loadImageCommand file =
        Cmd.OfAsync.perform
            async.Return
            file
            LoadImage

    /// Initializes model to start loading the given file.
    let init file =
        ImageModel.init (),
        loadImageCommand file

    /// Updates layout due to container resize.
    let private resize loaded =

            // keep zoom scale?
        let zoomScaleOpt =
            loaded
                ^. LoadedImage.Initialized_
                |> InitializedContainer.tryGetLockedZoomScale

            // get layout for new container size
        let offset, zoomScale =
            ImageLayout.getImageLayout
                (loaded ^. LoadedImage.ContainerSize_)
                loaded.BitmapSize
                (Some loaded.Offset)
                zoomScaleOpt

        loaded
            |> offset ^= LoadedImage.Offset_
            |> zoomScale ^= LoadedImage.ZoomScale_

    /// Sets or updates container size. This occurs when the
    /// container is first created (before it contains an
    /// image), and any time the container is resized by the
    /// user.
    let private onContainerSized containerSize model =
        let model =
            match model with

                    // creation: set container size
                | Uninitialized ->
                    Initialized (
                        InitializedContainer.create containerSize)

                    // resize: update container size and layout
                | Loaded loaded ->
                    loaded
                        |> containerSize ^= LoadedImage.ContainerSize_
                        |> resize
                        |> Loaded

                    // resize: just update container size
                | _ ->
                    model
                        |> containerSize ^= ImageModel.ContainerSize_
        model, Cmd.none

    /// Browses to and starts loading a file, if possible.
    let private browse inited incr fromFile =
        let model = ImageModel.browse inited incr fromFile
        let cmd =
            match model with
                | Browsed browsed ->
                    Cmd.OfAsync.perform
                        ImageFile.tryLoadImage
                        browsed.File
                        (function
                            | Ok bitmap ->
                                ImageLoaded (browsed.File, bitmap)
                            | Error msg -> HandleLoadError msg)
                | BrowseError _ -> Cmd.none
                | _ -> failwith "Invalid state"
        model, cmd

    /// Starts loading an image from the given file.
    let private onLoadImage file model =
        let inited = model ^. ImageModel.Initialized_
        browse inited 0 file

    /// Applies default layout rules to the given bitmap.
    let private layoutImage
        (dpiScale : float) (bitmap : Bitmap) browsed =

            // get size of bitmap, adjusted for DPI scale
        let bitmapSize =
            bitmap.PixelSize.ToSize(dpiScale)

            // keep zoom scale and offset?
        let zoomScaleOpt =
            browsed
                ^. BrowsedImage.Initialized_
                |> InitializedContainer.tryGetLockedZoomScale
        let offsetOpt =
            if zoomScaleOpt.IsSome then
                browsed ^. BrowsedImage.Offset_
            else None

            // layout image
        let offset, zoomScale =
            let containerSize =
                browsed ^. BrowsedImage.ContainerSize_
            ImageLayout.getImageLayout
                containerSize bitmapSize offsetOpt zoomScaleOpt

            // zoom scale lock succeeded?
        let zoomScaleLock = (Some zoomScale = zoomScaleOpt)

            // update offset/zoom
        let browsed =
            browsed
                |> offset ^= BrowsedImage.Offset_
                |> zoomScale ^= BrowsedImage.ZoomScale_
                |> zoomScaleLock ^= BrowsedImage.ZoomScaleLock_

        Loaded {
            Browsed = browsed
            Bitmap = bitmap
            BitmapSize = bitmapSize
            PanOpt = None
        }

    /// Sets image's bitmap.
    let private onImageLoaded
        dpiScale (file : FileInfo) bitmap model =
        let model =
            match model with
                | Browsed browsed
                    when FileSystemInfo.same file model.File ->
                    layoutImage dpiScale bitmap browsed
                | _ -> model   // stale async message
        model, Cmd.none

    /// Handles a load error.
    let private onHandleLoadError error = function
        | Browsed browsed ->
            let model =
                LoadError {
                    Browsed = browsed
                    Message = error
                }
            model, Cmd.none
        | _ -> failwith "Invalid state"

    /// Browses to a file, if possible.
    let private onBrowse incr model =
        let inited = model ^. ImageModel.Initialized_
        browse inited incr model.File

    /// Zooms the current image.
    let private zoom
        zoomScale zoomScaleLock pointerPosOpt loaded =
        let offset =
            ImageLayout.updateImageOffset
                pointerPosOpt zoomScale loaded

            // update offset/zoom
        let loaded =
            loaded
                |> offset ^= LoadedImage.Offset_
                |> zoomScale ^= LoadedImage.ZoomScale_
                |> zoomScaleLock ^= LoadedImage.ZoomScaleLock_

        Loaded loaded, Cmd.none

    /// Updates zoom scale and origin.
    let private onWheelZoom sign pointerPos = function
        | Loaded loaded ->
            let zoomScale, zoomScaleLock =
                ImageLayout.incrementZoomScale sign loaded
            zoom zoomScale zoomScaleLock (Some pointerPos) loaded
        | _ -> failwith "Invalid state"

    /// Zoom to actual size.
    let private onZoomToActualSize = function
        | Loaded loaded -> zoom 1.0 true None loaded
        | _ -> failwith "Invalid state"

    /// Starts panning.
    let private onPanStart pointerPos = function
        | Loaded loaded ->
            let pan =
                {
                    ImageOffset = loaded.Offset
                    PointerPos = pointerPos
                }
            Loaded { loaded with PanOpt = Some pan },
            Cmd.none
        | _ -> failwith "Invalid state"

    /// Moves the image during a pan.
    let private panImage pointerPos pan loaded =

            // track pointer
        let offset =
            pan.ImageOffset
                + (pointerPos - pan.PointerPos)

            // enforce layout rules
        let offset =
            ImageLayout.getImageOffset
                (loaded ^. LoadedImage.ContainerSize_)
                loaded.BitmapSize
                (Some offset)
                (loaded ^. LoadedImage.ZoomScale_)

        loaded
            |> offset ^= LoadedImage.Offset_

    /// Continues panning.
    let private onPanMove pointerPos = function
        | Loaded loaded as model ->
            match loaded.PanOpt with
                | Some pan ->
                    let model =
                        loaded
                            |> panImage pointerPos pan
                            |> Loaded
                    model, Cmd.none
                | None -> model, Cmd.none
        | _ -> failwith "Invalid state"

    /// Ends panning.
    let private onPanEnd = function
        | Loaded loaded ->
            let model =
                Loaded {
                    loaded with PanOpt = None }
            model, Cmd.none
        | _ -> failwith "Invalid state"

    /// Updates the given model based on the given message.
    let update dpiScale message model =
        match message with

                // set/update container size
            | ContainerSized containerSize ->
                 onContainerSized containerSize model

                // start loading an image
            | LoadImage file ->
                onLoadImage file model

                // finish loading an image
            | ImageLoaded (file, bitmap) ->
                onImageLoaded dpiScale file bitmap model

                // handle load error
            | HandleLoadError error ->
                onHandleLoadError error model

                // browse to previous/next image
            | Browse incr ->
                onBrowse incr model

                // update zoom
            | WheelZoom (sign, pointerPos) ->
                onWheelZoom sign pointerPos model

                // zoom to actual size
            | ZoomToActualSize ->
                onZoomToActualSize model

                // start pan
            | PanStart pointerPos ->
                onPanStart pointerPos model

                // continue pan
            | PanMove pointerPos ->
                onPanMove pointerPos model

                // finish pan
            | PanEnd ->
                onPanEnd model
