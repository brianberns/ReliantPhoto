namespace Reliant.Photo

open Elmish
open Avalonia.Media.Imaging

/// Messages that can change the underlying state.
type Message =

    /// Load the current image file, if possible.
    | LoadImage

    /// The current image file was (maybe) loaded.
    | ImageLoaded of Result<Bitmap, string>

    /// Browse to previous image in directory, if possible.
    | PreviousImage

    /// Browse to next image in directory, if possible.
    | NextImage

module Message =

    /// Browses to the given file.
    let init file =
        State.init file,
        Cmd.ofMsg LoadImage

    /// Updates the given state based on the given message.
    let update setTitle message state =
        match message with

                // start browsing to an image
            | LoadImage ->
                let cmd =
                    Cmd.OfAsync.perform
                        State.tryLoadBitmap
                        state.File.FullName
                        ImageLoaded
                state, cmd

                // finish browsing to an image
            | ImageLoaded bitmapResult ->
                setTitle state   // side-effect
                { state with ImageResult = bitmapResult },
                Cmd.none

                // browse to previous image
            | PreviousImage  ->
                State.browseImage -1 state,
                Cmd.ofMsg LoadImage

                // browse to next image
            | NextImage  ->
                State.browseImage 1 state,
                Cmd.ofMsg LoadImage
