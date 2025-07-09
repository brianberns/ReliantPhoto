namespace Reliant.Photo

open Elmish
open Avalonia

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

    /// Updates displayed image size.
    let private onImageSized imageSize model =
        let model =
            { model with ImageSizeOpt = Some imageSize }
        model, Cmd.none

    /// Updates zoom scale and origin.
    let private onWheelZoom sign (pointerPos : Point) model =
        assert(abs sign = 1)
        let zoomScaleOpt = ImageModel.getZoomScale model
        match zoomScaleOpt, model.ImageSizeOpt with
            | Some zoomScale, Some imageSize ->
                let zoomScale =
                    let factor = 1.1
                    if sign >= 0 then zoomScale * factor
                    else zoomScale / factor
                let origin =
                    let originX = pointerPos.X / imageSize.Width
                    let originY = pointerPos.Y / imageSize.Height
                    RelativePoint(originX, originY, RelativeUnit.Relative)
                let model =
                    { model with
                        ZoomScaleOpt = Some zoomScale
                        ZoomOrigin = origin }
                model, Cmd.none
            | None, _ -> failwith "Zoom scale not set"
            | _, None -> failwith "Image size not set"

    /// Updates the given model based on the given message.
    let update message model =
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
                onImageSized imageSize model

                // update zoom
            | WheelZoom (sign, pointerPos) ->
                onWheelZoom sign pointerPos model
