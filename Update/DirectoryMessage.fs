namespace Reliant.Photo

open Elmish

/// Messages that can change the directory model.
type DirectoryMessage =

    /// Load the current directory, if possible.
    | LoadDirectory

module DirectoryMessage =

    /// Browses to the given directory.
    let init dir =
        DirectoryModel.init dir,
        Cmd.ofMsg LoadDirectory

    /// Updates the given model based on the given message.
    let update message (model : DirectoryModel) =
        match message with
            | LoadDirectory ->
                model, (Cmd.none : Cmd<DirectoryMessage>)
