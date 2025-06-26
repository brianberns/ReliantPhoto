namespace Reliant.Photo

open System.IO
open Elmish

/// Messages that can change the directory model.
type DirectoryMessage =

    /// Load the current directory, if possible.
    | LoadDirectory

    /// The current directory was loaded.
    | DirectoryLoaded of (FileInfo * ImageResult)[]

    /// User has opened a directory.
    | DirectorySelected of DirectoryInfo

module DirectoryMessage =

    /// Browses to the given directory.
    let init dir =
        DirectoryModel.init dir,
        Cmd.ofMsg LoadDirectory

    /// Updates the given model based on the given message.
    let update setTitle message (model : DirectoryModel) =
        match message with

            | LoadDirectory ->
                let model =
                    { model with IsLoading = true }
                let cmd =
                    Cmd.OfAsync.perform
                        (DirectoryModel.tryLoadDirectory 150)
                        model.Directory
                        DirectoryLoaded
                model, cmd

            | DirectoryLoaded results ->
                setTitle model.Directory.FullName   // side-effect
                { model with
                    IsLoading = false
                    ImageLoadPairs = results },
                Cmd.none

            | DirectorySelected dir ->
                init dir
