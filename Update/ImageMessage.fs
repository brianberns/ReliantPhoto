namespace Reliant.Photo

open Elmish

open Avalonia
open Avalonia.Media.Imaging

/// Messages that can change the image model.
type ImageMessage =

    /// Load the current image file, if possible.
    | LoadImage

    /// The current image file has (maybe) loaded.
    | ImageLoaded of ImageResult

    /// Browse to previous image in directory, if possible.
    | PreviousImage

    /// Browse to next image in directory, if possible.
    | NextImage

    /// The size of the displayed image has changed.
    | ImageSized of Size

    /// The pointer wheel position has changed.
    | WheelZoom of int (*sign*) * Point (*pointer position*)

module ImageMessage =

    /// Browses to the given file.
    let init file =
        ImageModel.init file,
        Cmd.ofMsg LoadImage

    /// Starts loading the current image.
    let private onLoadImage (model : ImageModel) =
        let model =
            { model with IsLoading = true }
        let cmd =
            Cmd.OfAsync.perform
                (ImageFile.tryLoadImage None)
                model.File
                ImageLoaded
        model, cmd

    /// Calculates total zoom.
    let private getZoomTotal systemScale model =
        match model.Result with
            | Ok bitmap when bitmap.Size.Width > 0.0 ->
                float model.ImageSize.Width
                    * model.ZoomScale
                    * systemScale
                    / float bitmap.Size.Width
            | _ -> 0.0

    let private getZoomScale systemScale model =
        match model.Result with
            | Ok bitmap when bitmap.Size.Width > 0.0 ->
                (model.ZoomTotal * bitmap.Size.Width)
                    / (model.ImageSize.Width * systemScale)
            | _ -> 1.0

    /// Updates displayed image size.
    let private onImageSized systemScale imageSize model =
        let model =
            { model with ImageSize = imageSize }
        let model =
            { model with
                ZoomTotal =
                    min
                        (getZoomTotal systemScale model)
                        1.0 }
        let model =
            { model with
                ZoomScale =
                    getZoomScale systemScale model }
        model, Cmd.none

    /// Updates user zoom.
    let private onWheelZoom
        systemScale sign (pointerPos : Point) model =
        assert(abs sign = 1)
        let zoom =
            let factor = 1.1
            if sign >= 0 then model.ZoomScale * factor
            else model.ZoomScale / factor
        let origin =
            let originX = pointerPos.X / model.ImageSize.Width
            let originY = pointerPos.Y / model.ImageSize.Height
            RelativePoint(originX, originY, RelativeUnit.Relative)
        let zoomTotal = getZoomTotal systemScale model
        let model =
            { model with
                ZoomScale = zoom
                ZoomOrigin = origin
                ZoomTotal = zoomTotal }
        model, Cmd.none

    /// Updates the given model based on the given message.
    let update systemScale message model =
        match message with

                // start loading an image
            | LoadImage ->
                onLoadImage model

                // finish loading an image
            | ImageLoaded result ->
                { model with
                    IsLoading = false
                    Result = result },
                Cmd.none

                // browse to previous image
            | PreviousImage  ->
                ImageModel.browse -1 model.File,
                Cmd.ofMsg LoadImage

                // browse to next image
            | NextImage  ->
                ImageModel.browse 1 model.File,
                Cmd.ofMsg LoadImage

                // update image size
            | ImageSized imageSize ->
                onImageSized systemScale imageSize model

                // update zoom
            | WheelZoom (sign, pointerPos) ->
                onWheelZoom systemScale sign pointerPos model
