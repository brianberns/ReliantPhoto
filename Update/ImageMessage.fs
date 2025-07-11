namespace Reliant.Photo

open Elmish

open Avalonia
open Avalonia.Media.Imaging

/// Messages that can update the image model.
type ImageMessage =

    /// Load image, if possible.
    | LoadImage

    /// Display image.
    | Display of Bitmap

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
        Cmd.ofMsg LoadImage

    /// Starts loading an image.
    let private onLoadImage = function

            // browse succeeded, try to load image
        | Browsed browsed as model ->
            let cmd =
                Cmd.ofAsyncResult
                    (ImageFile.tryLoadImage None)
                    browsed.File
                    Display
                    HandleLoadError
            model, cmd

            // browse failed, can't load image
        | BrowseError _ as model ->
            model, Cmd.none

        | _ -> failwith "Invalid state"

    /// Finishes loading an image and starts displaying it.
    let private onDisplayImage bitmap (model : ImageModel) =
        let model =
            match model with
                | Browsed browsed ->
                    Loaded {
                        Browsed = browsed
                        Bitmap = bitmap
                    }
                | _ -> failwith "Invalid state"
        model, Cmd.none

    /// Sets or updates image size for a displayed image.
    let private onImageSized imageSize (model : ImageModel) =

        let toDisplayed loaded =
            {
                Loaded = loaded
                ImageSize = imageSize
            }

        let model =
            match model with
                | Loaded loaded ->
                    Displayed (toDisplayed loaded)
                | Displayed displayed ->
                    Displayed (toDisplayed displayed.Loaded)
                | Zoomed zoomed ->
                    Zoomed {
                        zoomed with
                            Displayed =
                                toDisplayed zoomed.Loaded }
                | _ -> failwith "Invalid state"

        model, Cmd.none

    /// Browses to a file, if possible.
    let private onBrowse incr (model : ImageModel) =
        ImageModel.browse incr model.BrowsedImage.File,
        Cmd.ofMsg LoadImage

    /// Updates zoom scale and origin.
    let private onWheelZoom
        dpiScale sign (pointerPos : Point) model =
        assert(abs sign = 1)

            // get relevant image attributes
        let displayed, zoomScale, imageScale =
            match model with

                    // variable zoom scale 
                | Displayed displayed ->
                    let zoomScale =
                        DisplayedImage.getImageScale
                            dpiScale displayed
                    displayed, zoomScale, zoomScale

                    // fixed zoom scale
                | Zoomed zoomed ->
                    let imageScale =
                        DisplayedImage.getImageScale
                            dpiScale zoomed.Displayed
                    zoomed.Displayed,
                    zoomed.Scale,
                    imageScale

                | _ -> failwith "Invalid state"

            // determine the lowest allowable zoom scale
        let zoomScaleFloor =
            let floorSize =
                displayed.Loaded.Bitmap.Size / dpiScale
            if floorSize.Width > displayed.ImageSize.Width then
                assert(imageScale < 1.0)
                imageScale   // large image: fill view
            else 1.0         // small image: 100%

            // update zoom scale
        let zoomScale =
            let factor = 1.1
            let scale =
                if sign >= 0 then zoomScale * factor
                else zoomScale / factor
            max zoomScaleFloor scale

            // update zoom origin
        let zoomOrigin =
            let imageSize = displayed.ImageSize
            let originX = pointerPos.X / imageSize.Width
            let originY = pointerPos.Y / imageSize.Height
            RelativePoint(originX, originY, RelativeUnit.Relative)

            // update model
        let model =
            Zoomed {
                Displayed = displayed
                Scale = zoomScale
                Origin = zoomOrigin
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
            | Display bitmap ->
                onDisplayImage bitmap model

                // update image size
            | ImageSized imageSize ->
                onImageSized imageSize model

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
