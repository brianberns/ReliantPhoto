namespace Reliant.Photo

open System.IO
open FSharp.Control
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

    let private createEffect asyncPairs dispatch =
        async {
            do! asyncPairs
                |> AsyncSeq.chunkBySize 10
                |> AsyncSeq.iter (
                    ImagesLoaded >> dispatch)
        } |> Async.Start

    /// Updates the given model based on the given message.
    let update setTitle message (model : DirectoryModel) =
        match message with

            | LoadDirectory ->
                let model =
                    { model with IsLoading = true }
                let cmd =
                    model.Directory
                        |> DirectoryModel.tryLoadDirectory 150 
                        |> createEffect
                        |> Cmd.ofEffect
                model, cmd

            | ImagesLoaded pairs ->
                // setTitle model.Directory.FullName   // side-effect
                { model with
                    IsLoading = false
                    ImageLoadPairs =
                        Array.append
                            model.ImageLoadPairs
                            pairs },
                Cmd.none

            | DirectorySelected dir ->
                init dir
