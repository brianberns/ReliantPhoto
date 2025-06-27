namespace Reliant.Photo

open System.IO
open Elmish

/// Messages that can change the directory model.
type DirectoryMessage =

    /// Load the current directory, if possible.
    | LoadDirectory

    /// Images in the current directory were loaded.
    | ImagesLoaded of (FileInfo * ImageResult)[]

    /// User has selected a directory to load.
    | DirectorySelected of DirectoryInfo

module DirectoryMessage =

    /// Browses to the given directory.
    let init dir =
        DirectoryModel.init dir,
        Cmd.ofMsg LoadDirectory

    let private createEffect chunks dispatch =
        async {
            for chunk in chunks do
                let! pairs = Async.Parallel chunk
                dispatch (ImagesLoaded pairs)
        } |> Async.Start

    /// Updates the given model based on the given message.
    let update setTitle message (model : DirectoryModel) =
        match message with

            | LoadDirectory ->
                setTitle model.Directory.FullName   // side-effect
                let model =
                    { model with IsLoading = true }
                let cmd =
                    model.Directory
                        |> DirectoryModel.tryLoadDirectory 150
                        |> Seq.chunkBySize 25
                        |> createEffect
                        |> Cmd.ofEffect
                model, cmd

            | ImagesLoaded pairs ->
                { model with
                    IsLoading = false
                    ImageLoadPairs =
                        Array.append
                            model.ImageLoadPairs
                            pairs },
                Cmd.none

            | DirectorySelected dir ->
                init dir
