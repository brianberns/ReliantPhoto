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
        let state = State.init file
        let cmd =
            if state.FileOpt.IsSome then
                Cmd.ofMsg LoadImage
            else Cmd.none
        state, cmd

    /// Updates the given state based on the given message.
    let update setTitle message state =
        match message with

                // start browsing to an image
            | LoadImage ->
                match state.FileOpt with
                    | Some file ->
                        let cmd =
                            Cmd.OfAsync.perform
                                State.tryLoadBitmap
                                file.FullName
                                ImageLoaded
                        state, cmd
                    | None -> failwith "No file to load"

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
