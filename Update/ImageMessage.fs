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
    let private onImageSized systemScale size model =
        let model =
            { model with ImageSize = size }
                |> ImageModel.setZoomTotal systemScale
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
        let model =
            { model with
                ZoomScale = zoom
                ZoomOrigin = origin }
                |> ImageModel.setZoomTotal systemScale
        model, Cmd.none

    /// Updates the given model based on the given message.
    let update systemScale message model=
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
            | ImageSized size ->
                onImageSized systemScale size model

                // update zoom
            | WheelZoom (sign, pointerPos) ->
                onWheelZoom systemScale sign pointerPos model
