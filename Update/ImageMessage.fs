namespace Reliant.Photo

open Elmish
open Avalonia.Media.Imaging

/// Messages that can change the image model.
type ImageMessage =

    /// Load the current image file, if possible.
    | LoadImage

    /// The current image file was (maybe) loaded.
    | ImageLoaded of Result<Bitmap, string>

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
    let update setTitle message model =
        match message with

                // start browsing to an image
            | LoadImage ->
                let cmd =
                    Cmd.OfAsync.perform
                        ImageModel.tryLoadImage
                        model.File.FullName
                        ImageLoaded
                model, cmd

                // finish browsing to an image
            | ImageLoaded result ->
                setTitle model   // side-effect
                { model with Result = result },
                Cmd.none

                // browse to previous image
            | PreviousImage  ->
                ImageModel.browseImage -1 model,
                Cmd.ofMsg LoadImage

                // browse to next image
            | NextImage  ->
                ImageModel.browseImage 1 model,
                Cmd.ofMsg LoadImage
