namespace Reliant.Photo

open Elmish
open Avalonia

/// Messages that can change the image model.
type ImageMessage =

    /// Load the current image file, if possible.
    | LoadImage

    /// The current image file was (maybe) loaded.
    | ImageLoaded of ImageResult

    /// Browse to previous image in directory, if possible.
    | PreviousImage

    /// Browse to next image in directory, if possible.
    | NextImage

    | ImageSized of Size
    | WheelZoom of int (*sign*) * Point

module ImageMessage =

    /// Browses to the given file.
    let init file =
        ImageModel.init file,
        Cmd.ofMsg LoadImage

    /// Updates the given model based on the given message.
    let update message (model : ImageModel) =
        match message with

                // start browsing to an image
            | LoadImage ->
                let model =
                    { model with IsLoading = true }
                let cmd =
                    Cmd.OfAsync.perform
                        (ImageFile.tryLoadImage None)
                        model.File
                        ImageLoaded
                model, cmd

                // finish browsing to an image
            | ImageLoaded result ->
                { model with
                    IsLoading = false
                    Result = result },
                Cmd.none

                // browse to previous image
            | PreviousImage  ->
                ImageModel.browseImage -1 model,
                Cmd.ofMsg LoadImage

                // browse to next image
            | NextImage  ->
                ImageModel.browseImage 1 model,
                Cmd.ofMsg LoadImage

            | ImageSized size ->
                { model with ImageSize = size },
                Cmd.none

            | WheelZoom (sign, pointerPos) ->
                assert(abs sign = 1)
                let incr = 10.0   // increment by tenths
                let zoom =
                    floor ((model.ZoomScale * incr) + float sign) / incr
                        |> max 0.1
                        |> min 10.0
                let origin =
                    let originX = pointerPos.X / model.ImageSize.Width
                    let originY = pointerPos.Y / model.ImageSize.Height
                    RelativePoint(originX, originY, RelativeUnit.Relative)
                { model with
                    ZoomScale = zoom
                    ZoomOrigin = origin },
                Cmd.none
