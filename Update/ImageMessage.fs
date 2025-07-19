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
    | ImageLoaded of Bitmap

    /// Browse to previous image in directory, if possible.
    | PreviousImage

    /// Browse to next image in directory, if possible.
    | NextImage

    /// Pointer wheel position has changed.
    | WheelZoom of int (*sign*) * Point (*pointer position*)

    /// Load error occurred.
    | HandleLoadError of string

module Cmd =

    /// Creates a command that handles an async result.
    let ofAsyncResult task arg ofSuccess ofError =
        Cmd.OfAsync.perform
            task arg
            (function
                | Ok success -> ofSuccess success
                | Error error -> ofError error)

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

    /// Gets the (positive or negative) gap between the image
    /// and its container.
    let private getMarginSize
        (dpiScale : float)
        (containerSize : Size)
        (bitmap : Bitmap)
        (zoomScale : float) =
        let imageSize =
            bitmap.PixelSize.ToSize(dpiScale) * zoomScale
        containerSize - imageSize

    /// Gets the default image offset, which is centered in both
    /// dimensions.
    let private getDefaultOffset
        dpiScale containerSize bitmap zoomScale =
        let offsetSize =
            (getMarginSize
                dpiScale containerSize bitmap zoomScale)
                / 2.0
        Point(offsetSize.Width, offsetSize.Height)

    /// Default zoom scale for the given bitmap in the given
    /// container.
    let private getDefaultZoomScale
        (dpiScale : float)
        (containerSize : Size)
        (bitmap : Bitmap) =
        let ratio =
            containerSize / bitmap.PixelSize.ToSize(dpiScale)
        Array.min [| ratio.X; ratio.Y; 1.0 |]

    /// Gets image offset and zoom scale.
    let private getImageLayout dpiScale containerSize bitmap =

            // first scale the image to fit in the container
        let zoomScale =
            getDefaultZoomScale
                dpiScale containerSize bitmap

            // then compute the correct offset for that zoom scale
        let offset =
            getDefaultOffset
                dpiScale containerSize bitmap zoomScale

        offset, zoomScale

    /// Sets or updates container size. This occurs when the
    /// container is first created (before it contains an
    /// image), and any time the container is resized by the
    /// user.
    let private onContainerSized dpiScale containerSize model =
        let inited =
            { ContainerSize = containerSize }
        let model =
            match model with

                    // creation: set container size
                | Uninitialized -> Initialized inited

                    // resize: update container size and image layout
                | Loaded loaded ->
                    let offset, zoomScale =
                        getImageLayout
                            dpiScale containerSize loaded.Bitmap
                    Loaded {
                        loaded with
                            Offset = offset
                            ZoomScale = zoomScale
                    } |> inited ^= ImageModel.Initialized_

                    // resize: just update container size
                | _ ->
                    model
                        |> inited ^= ImageModel.Initialized_
        model, Cmd.none

    /// Browses to and starts loading a file, if possible.
    let private browse inited incr fromFile =
        let model = ImageModel.browse inited incr fromFile
        let cmd =
            match model with
                | Browsed browsed ->
                    Cmd.ofAsyncResult
                        (ImageFile.tryLoadImage None)
                        browsed.File
                        ImageLoaded
                        HandleLoadError
                | BrowseError _ -> Cmd.none
                | _ -> failwith "Invalid state"
        model, cmd

    /// Starts loading an image from the given file.
    let private onLoadImage file model =
        let inited = model ^. ImageModel.Initialized_
        browse inited 0 file

    /// Sets image's bitmap.
    let private onImageLoaded dpiScale bitmap model =
        let model =
            match model with
                | Browsed browsed ->
                    let containerSize =
                        browsed.Initialized.ContainerSize
                    let zoomScale =
                        getDefaultZoomScale
                            dpiScale containerSize bitmap
                    let offset =
                        getDefaultOffset
                            dpiScale containerSize bitmap zoomScale
                    Loaded {
                        Browsed = browsed
                        Bitmap = bitmap
                        Offset = offset
                        ZoomScale = zoomScale
                    }
                | _ -> failwith "Invalid state"
        model, Cmd.none

    /// Browses to a file, if possible.
    let private onBrowse incr model =
        let inited = model ^. ImageModel.Initialized_
        browse inited incr model.File

    /// Acceptable rounding error.
    let private epsilon = 0.001

    /// Zooms in or out one step.
    let private updateZoomScale dpiScale zoomSign loaded =
        assert(abs zoomSign = 1)

            // compute new zoom scale
        let zoomScale = loaded.ZoomScale
        let factor = 1.1

            // zoom in?
        if zoomSign >= 0 then
            zoomScale * factor

            // zoom out?
        else
                // get minimum allowable zoom scale
            let zoomScaleFloor =
                let containerSize =
                    loaded.Browsed.Initialized.ContainerSize
                getDefaultZoomScale
                    dpiScale containerSize loaded.Bitmap

                // zoom out
            let newScale = zoomScale / factor

                // enforce floor
            let newScale =
                if zoomScaleFloor - newScale > epsilon then
                    zoomScale   // don't jump suddenly
                else newScale

                // snap to 1.0?
            if newScale > 1.0 && zoomScale < 1.0
                || newScale < 1.0 && zoomScale > 1.0 then
                1.0
            else newScale

    let private updateImageOffset
        dpiScale (pointerPos : Point) newZoomScale loaded =

            // ensure the point under the cursor stays stationary
        let newOffset =
            pointerPos
                - (pointerPos - loaded.Offset)
                    * (newZoomScale / loaded.ZoomScale)

            // compute (positive or negative) gap between image and container
        let marginSize =
            getMarginSize
                dpiScale
                loaded.Browsed.Initialized.ContainerSize
                loaded.Bitmap
                newZoomScale

            // positive margin: center image
            // negative margin: clamp image edges to container edges, if necessary
        let offsetX =
            if marginSize.Width > 0.0 then
                marginSize.Width / 2.0
            else
                max marginSize.Width (min 0.0 newOffset.X)
        let offsetY =
            if marginSize.Height > 0.0 then
                marginSize.Height / 2.0
            else
                max marginSize.Height (min 0.0 newOffset.Y)
        Point(offsetX, offsetY)

    /// Updates zoom scale and origin.
    let private onWheelZoom
        dpiScale sign pointerPos = function

        | Loaded loaded ->

                // update zoom scale
            let zoomScale =
                updateZoomScale dpiScale sign loaded

                // update image offset
            let offset =
                updateImageOffset
                    dpiScale pointerPos zoomScale loaded

                // update model
            let model =
                Loaded {
                    loaded with
                        ZoomScale = zoomScale
                        Offset = offset
                }
            model, Cmd.none

        | _ -> failwith "Invalid state"

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

    /// Updates the given model based on the given message.
    let update dpiScale message model =
        match message with

                // set/update container size
            | ContainerSized containerSize ->
                 onContainerSized dpiScale containerSize model

                // start loading an image
            | LoadImage file ->
                onLoadImage file model

                // finish loading an image
            | ImageLoaded bitmap ->
                onImageLoaded dpiScale bitmap model

                // browse to previous image
            | PreviousImage  ->
                onBrowse -1 model

                // browse to next image
            | NextImage  ->
                onBrowse 1 model

                // update zoom
            | WheelZoom (sign, pointerPos) ->
                onWheelZoom dpiScale sign pointerPos model

                // handle load error
            | HandleLoadError error ->
                onHandleLoadError error model
