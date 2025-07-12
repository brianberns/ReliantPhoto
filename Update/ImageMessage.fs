namespace Reliant.Photo

open Elmish

open Avalonia
open Avalonia.Media.Imaging

open Aether
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

    /// Sets or updates container size for a loaded image.
    let private onContainerSized containerSize model =
        let model =
            model
                |> containerSize
                ^= (ImageModel.Contained_
                    >-> ContainedImage.ContainerSize_)
        model, Cmd.none

    /// Starts loading an image.
    let private onLoadImage = function

            // browse succeeded, try to load image
        | Browsed browsed as model ->
            let cmd =
                Cmd.ofAsyncResult
                    (ImageFile.tryLoadImage None)
                    browsed.File
                    BitmapLoaded
                    HandleLoadError
            model, cmd

            // browse failed, can't load image
        | BrowseError _ as model ->
            model, Cmd.none

        | _ -> failwith "Invalid state"

    /// Finishes loading an image.
    let private onBitmapLoaded bitmap model =
        let model =
            model
                |> bitmap
                ^= (ImageModel.Loaded_
                    >-> LoadedImage.Bitmap_)
        model, Cmd.none

    /// Acceptable rounding error.
    let private epsilon = 0.001

    /// Sets or updates image size for a displayed image.
    let private onImageSized
        dpiScale imageSize (model : ImageModel) =

        let display loaded =
            let imageScale =
                let vector =
                    imageSize / loaded.Bitmap.Size
                assert(abs (vector.X - vector.Y) < epsilon)
                vector.X * dpiScale   // e.g. Avalonia thinks image is at 100%, but OS actually shows it at 125%
            {
                Loaded = loaded
                ImageSize = imageSize
                ImageScale = imageScale
            }

        let model =
            match model with
                | Loaded loaded ->
                    Displayed (display loaded)
                | Displayed displayed ->
                    Displayed (display displayed.Loaded)
                | Zoomed zoomed ->
                    Zoomed {
                        zoomed with
                            Displayed =
                                display zoomed.Loaded }
                | _ -> failwith "Invalid state"

        model, Cmd.none

    /// Browses to a file, if possible.
    let private onBrowse incr (model : ImageModel) =
        ImageModel.browse incr model.BrowsedImage.File,
        Cmd.ofMsg LoadImage

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
    let private onWheelZoom
        dpiScale sign pointerPos (model : ImageModel) =

            // get image attributes
        let displayed = model.DisplayedImage
        let imageScale = displayed.ImageScale

            // update zoom scale and origin
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
    let private onHandleLoadError error (model : ImageModel) =
        let model =
            LoadError {
                Browsed = model.BrowsedImage
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
