namespace Reliant.Photo

open Elmish

open Avalonia
open Avalonia.Media.Imaging

open Aether.Operators

/// Messages that can update the image model.
type ImageMessage =

    /// Load image, if possible.
    | LoadImage

    /// Size of the image container has been set or updated.
    | ContainerSized of Size

    /// Bitmap has been loaded.
    | BitmapLoaded of Bitmap

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

    /// Starts loading an image, if possible.
    let private onLoadImage (model : ImageModel) =
        let cmd =
            if model.IsBrowseError then Cmd.none   // browse failed
            else
                let browsed = model ^. ImageModel.Browsed_
                Cmd.ofAsyncResult
                    (ImageFile.tryLoadImage None)
                    browsed.File
                    BitmapLoaded
                    HandleLoadError
        model, cmd

    /// Sets bitmap for a contained image.
    let private onBitmapLoaded bitmap model =
        let model =
            match model with
                | Contained contained ->
                    Loaded {
                        Contained = contained
                        Bitmap = bitmap
                        ZoomScale = 1.0
                        ZoomOrigin = RelativePoint(0.5, 0.5, RelativeUnit.Relative)
                    }
                | _ -> failwith "Invalid state"
        model, Cmd.none

    /// Acceptable rounding error.
    let private epsilon = 0.001

    /// Browses to a file, if possible.
    let private onBrowse incr model =

            // browse to file
        let contained = model ^. ImageModel.Contained_
        let model =
            contained.Browsed.File
                |> ImageModel.browse incr

            // keep container size if browse succeeded
        let model =
            match model ^. ImageModel.TryBrowsed_ with
                | Some browsed ->
                    Contained {
                        contained with
                            Browsed = browsed }
                | None -> model

        model, Cmd.ofMsg LoadImage

    /// Determines the lowest allowable zoom scale.
    (*
    let private getZoomScaleFloor
        (dpiScale : float)
        (displayed : DisplayedImage)
        imageScale =
        if displayed.Loaded.Bitmap.Size.Width
            > displayed.ImageSize.Width * dpiScale then
            assert(imageScale < 1.0)
            imageScale   // large image: fill view
        else 1.0         // small image: 100%
    *)

    /// Updates zoom scale based on user input.
    let private updateZoomScale sign zoomScaleFloor loaded =
        assert(abs sign = 1)

        let factor = 1.1
        let zoomScale = loaded.ZoomScale
        if sign >= 0 then zoomScale * factor
        else
            let newScale = zoomScale / factor
            (*
            if zoomScaleFloor - newScale > epsilon then
                zoomScale   // don't jump suddenly
            else newScale
            *)
            newScale

    /// Updates zoom origin based on user input.
    (*
    let private updateZoomOrigin (pointerPos : Point) displayed =
        let imageSize = displayed.ImageSize
        let originX = pointerPos.X / imageSize.Width
        let originY = pointerPos.Y / imageSize.Height
        RelativePoint(originX, originY, RelativeUnit.Relative)
    *)

    /// Updates zoom scale and origin.
    let private onWheelZoom dpiScale sign pointerPos = function

        | Loaded loaded ->

                // update zoom scale and origin
            // let imageScale = displayed.ImageScale
            let zoomScale =
                let zoomScaleFloor = 0.0
                    // getZoomScaleFloor dpiScale displayed imageScale
                updateZoomScale sign zoomScaleFloor loaded
            let zoomOrigin = RelativePoint(0.5, 0.5, RelativeUnit.Relative) // updateZoomOrigin pointerPos displayed

                // update model
            let model =
                Loaded {
                    loaded with
                        ZoomScale = zoomScale
                        ZoomOrigin = zoomOrigin
                }
            model, Cmd.none

        | _ -> failwith "Invalid state"

    /// Handles a load error.
    let private onHandleLoadError error model =
        let model =
            LoadError {
                Contained = model ^. ImageModel.Contained_
                Message = error
            }
        model, Cmd.none

    /// Updates the given model based on the given message.
    let update dpiScale message model =
        match message with

                // start loading an image
            | LoadImage ->
                onLoadImage model

                // update container size
            | ContainerSized containerSize ->
                 onContainerSized containerSize model

                // finish loading an image
            | BitmapLoaded bitmap ->
                onBitmapLoaded bitmap model

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
