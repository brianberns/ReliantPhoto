namespace Reliant.Photo

open Elmish
open Avalonia.Media.Imaging

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

module ImageMessage =

    /// Browses to the given file.
    let init file =
        ImageModel.init file,
        Cmd.ofMsg LoadImage

    /// Updates the given model based on the given message.
    let update setTitle message (model : ImageModel) =
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
                setTitle model.File.Name   // side-effect
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
