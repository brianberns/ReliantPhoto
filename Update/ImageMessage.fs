namespace Reliant.Photo

open Elmish

open Avalonia
open Avalonia.Media.Imaging

open Aether.Operators

/// Messages that can update the image model.
type ImageMessage =

    /// Load image, if possible.
    | LoadImage

    /// Size of the container has been set or updated.
    | ContainerSized of Size

    /// Bitmap has been loaded.
    | BitmapLoaded of Bitmap

    /// Size of the displayed image has been set or updated.
    | ImageSized of Size

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

    /// Browses to the given file.
    let init file =
        ImageModel.init file,
        Cmd.none

    /// Sets or updates container size for a browsed image.
    let private onContainerSized containerSize model =

        let contained =
            {
                Browsed = model ^. ImageModel.Browsed_
                ContainerSize = containerSize
            }

        let model =
            match model with
                | Browsed _ -> Contained contained
                | _ ->
                    model
                        |> contained ^= ImageModel.Contained_
        model, Cmd.none

    /// Starts loading an image.
    let private onLoadImage model =
        match model ^. ImageModel.TryBrowsed_ with
            | Some browsed ->
                let cmd =
                    Cmd.ofAsyncResult
                        (ImageFile.tryLoadImage None)
                        browsed.File
                        BitmapLoaded
                        HandleLoadError
                model, cmd
            | None -> failwith "Invalid state"

    /// Sets bitmap for a contained image.
    let private onBitmapLoaded bitmap model =
        let model =
            match model with
                | Contained contained ->
                    Loaded {
                        Contained = contained
                        Bitmap = bitmap
                    }
                | _ -> failwith "Invalid state"
        model, Cmd.none

    /// Acceptable rounding error.
    let private epsilon = 0.001

    /// Sets or updates image size for a displayed image.
    let private onImageSized dpiScale imageSize model =

        let displayed =
            let loaded = model ^. ImageModel.Loaded_
            let vector =
                imageSize / loaded.Bitmap.Size
            assert(abs (vector.X - vector.Y) < epsilon)
            let imageScale = vector.X * dpiScale   // e.g. Avalonia thinks image is at 100%, but OS actually shows it at 125%
            {
                Loaded = loaded
                ImageSize = imageSize
                ImageScale = imageScale
            }

        let model =
            match model with
                | Loaded _ -> Displayed displayed
                | _ ->
                    model
                        |> displayed ^= ImageModel.Displayed_
        model, Cmd.none

    /// Browses to a file, if possible.
    let private onBrowse incr model =
        let contained = model ^. ImageModel.Contained_
        let file = contained.Browsed.File
        match ImageModel.browse incr file with

                // browse succeeded, keep container size
            | Browsed browsed ->
                Contained {
                    contained with
                        Browsed = browsed },
                Cmd.ofMsg LoadImage

                // browse failed
            | BrowseError _ as model ->
                model, Cmd.none

            | _ -> failwith "Invalid state"

    /// Determines the lowest allowable zoom scale.
    let private getZoomScaleFloor
        (dpiScale : float)
        (displayed : DisplayedImage)
        imageScale =
        if displayed.Loaded.Bitmap.Size.Width
            > displayed.ImageSize.Width * dpiScale then
            assert(imageScale < 1.0)
            imageScale   // large image: fill view
        else 1.0         // small image: 100%

    /// Updates zoom scale based on user input.
    let private updateZoomScale
        sign imageScale zoomScaleFloor model =
        assert(abs sign = 1)

        let zoomScale =
            match model with
                | Zoomed zoomed -> zoomed.ZoomScale
                | _ -> imageScale

        let factor = 1.1
        if sign >= 0 then zoomScale * factor
        else
            let newScale = zoomScale / factor
            if zoomScaleFloor - newScale > epsilon then
                zoomScale   // don't jump suddenly
            else newScale

    /// Updates zoom origin based on user input.
    let private updateZoomOrigin (pointerPos : Point) displayed =
        let imageSize = displayed.ImageSize
        let originX = pointerPos.X / imageSize.Width
        let originY = pointerPos.Y / imageSize.Height
        RelativePoint(originX, originY, RelativeUnit.Relative)

    /// Updates zoom scale and origin.
    let private onWheelZoom dpiScale sign pointerPos model =

            // update zoom scale and origin
        let displayed = model ^. ImageModel.Displayed_
        let imageScale = displayed.ImageScale
        let zoomScale =
            let zoomScaleFloor =
                getZoomScaleFloor dpiScale displayed imageScale
            updateZoomScale sign imageScale zoomScaleFloor model
        let zoomOrigin = updateZoomOrigin pointerPos displayed

            // update model
        let model =
            Zoomed {
                Displayed = displayed
                ZoomScale = zoomScale
                ZoomOrigin = zoomOrigin
            }
        model, Cmd.none

    /// Handles a load error.
    let private onHandleLoadError error model =
        let model =
            LoadError {
                Browsed = model ^. ImageModel.Browsed_
                Message = error
            }
        model, Cmd.none

    /// Updates the given model based on the given message.
    let update dpiScale message model =
        match message with

                // start loading an image
            | LoadImage ->
                onLoadImage model

                // finish loading an image
            | BitmapLoaded bitmap ->
                onBitmapLoaded bitmap model

                // update container size
            | ContainerSized containerSize ->
                 onContainerSized containerSize model

                // update image size
            | ImageSized imageSize ->
                onImageSized dpiScale imageSize model

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
