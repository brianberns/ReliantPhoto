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

    /// Unload current image.
    | UnloadImage

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

    /// Initializes model to start loading the given file, if
    /// specified.
    let init fileOpt =
        let model = ImageModel.init ()
        let cmd =
            fileOpt
                |> Option.map loadImageCommand
                |> Option.defaultValue Cmd.none
        model, cmd

    /// Updates layout due to container resize.
    let private updateLayout loaded =

            // keep zoom scale constant?
        let zoomScaleOpt =
            if loaded.ZoomScaleLock then
                Some loaded.ZoomScale
            else None

            // get layout for new container size
        let offset, zoomScale =
            ImageLayout.getImageLayout
                (loaded ^. LoadedImage.ContainerSize_)
                loaded.BitmapSize
                (Some loaded.Offset)
                zoomScaleOpt

        {
            loaded with
                Offset = offset
                ZoomScale = zoomScale
        }

    /// Sets or updates container size. This occurs when the
    /// container is first created (before it contains an
    /// image), and any time the container is resized by the
    /// user.
    let private onContainerSized containerSize model =
        let model =
            match model with

                    // creation: set container size
                | Uninitialized ->
                    Initialized { ContainerSize = containerSize }

                    // resize: update container size and layout
                | Loaded loaded ->
                    loaded
                        |> containerSize ^= LoadedImage.ContainerSize_
                        |> updateLayout
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

    /// Unloads the current image, if any.
    let private onUnloadImage model =
        let model =
            model ^. ImageModel.TryInitialized_
                |> Option.map Initialized
                |> Option.defaultValue model
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

    /// Applies default layout rules to the given bitmap.
    let private layoutImage
        (dpiScale : float) (bitmap : Bitmap) browsed =

            // get default layout
        let bitmapSize =
            bitmap.PixelSize.ToSize(dpiScale)
        let offset, zoomScale =
            let containerSize =
                (browsed ^. BrowsedImage.ContainerSize_)
            ImageLayout.getImageLayout
                containerSize bitmapSize None None

        Loaded {
            Browsed = browsed
            Bitmap = bitmap
            BitmapSize = bitmapSize
            Offset = offset
            ZoomScale = zoomScale
            ZoomScaleLock = false
            PanOpt = None
        }

    /// Sets image's bitmap.
    let private onImageLoaded
        dpiScale (file : FileInfo) bitmap model =
        let model =
            match model with
                | Browsed browsed
                    when file.FullName = model.File.FullName ->
                    layoutImage dpiScale bitmap browsed
                | _ -> model   // stale async message
        model, Cmd.none

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
        Loaded {
            loaded with
                Offset = offset
                ZoomScale = zoomScale
                ZoomScaleLock = zoomScaleLock
        }, Cmd.none

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
                loaded.ZoomScale

        { loaded with Offset = offset }

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

                // unload current image
            | UnloadImage ->
                onUnloadImage model

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
