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
    let private getZoomTotal
        systemScale (bitmap : Bitmap) (imageSize : Size) zoomScale =
        float imageSize.Width
            * zoomScale
            * systemScale
            / float bitmap.Size.Width

    let private getZoomScale
        systemScale (bitmap : Bitmap) (imageSize : Size) zoomTotal =
        (zoomTotal * bitmap.Size.Width)
            / (imageSize.Width * systemScale)

    let private getZoom systemScale bitmap imageSize zoomScale =
        let zoomTotal =
            min
                (getZoomTotal
                    systemScale bitmap imageSize zoomScale)
                1.0
        let zoomScale =
            getZoomScale systemScale bitmap imageSize zoomTotal
        zoomScale, zoomTotal

    /// Updates displayed image size.
    let private onImageSized systemScale imageSize model =
        let model =
            match model.Result with
                | Ok bitmap ->
                    let zoomScale, zoomTotal =
                        getZoom
                            systemScale bitmap imageSize model.ZoomScale
                    { model with
                        ImageSize = imageSize
                        ZoomScale = zoomScale
                        ZoomTotal = zoomTotal }
                | _ -> model
        model, Cmd.none

    /// Updates user zoom.
    let private onWheelZoom
        systemScale sign (pointerPos : Point) model =
        assert(abs sign = 1)
        let zoomScale =
            let factor = 1.1
            if sign >= 0 then model.ZoomScale * factor
            else model.ZoomScale / factor
        let origin =
            let originX = pointerPos.X / model.ImageSize.Width
            let originY = pointerPos.Y / model.ImageSize.Height
            RelativePoint(originX, originY, RelativeUnit.Relative)
        let model =
            match model.Result with
                | Ok bitmap ->
                    let zoomTotal =
                        getZoomTotal
                            systemScale bitmap
                            model.ImageSize model.ZoomScale
                    { model with
                        ZoomScale = zoomScale
                        ZoomOrigin = origin
                        ZoomTotal = zoomTotal }
                | _ -> model
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
