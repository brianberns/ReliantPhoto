namespace Reliant.Photo

open Elmish

/// Messages that can change the model.
type Message =
    | MkDirectoryMessage of DirectoryMessage
    | MkImageMessage of ImageMessage

module Message =

    /// Browses to the given image or directory.
    let init arg =
        let model = Model.init arg
        let cmd =
            match model with
                | MkDirectoryModel _ ->
                    Cmd.ofMsg (MkDirectoryMessage LoadDirectory)
                | MkImageModel _ ->
                    Cmd.ofMsg (MkImageMessage LoadImage)
        model, cmd

    /// Updates the given model based on the given message.
    let update setTitle message model =
        match message, model with
            | MkDirectoryMessage dirMsg, MkDirectoryModel dirModel ->
                let dirModel, dirCmd =
                    DirectoryMessage.update dirMsg dirModel
                MkDirectoryModel dirModel,
                Cmd.map MkDirectoryMessage dirCmd
            | MkImageMessage imgMsg, MkImageModel imgModel ->
                let imgModel, imgCmd =
                    ImageMessage.update setTitle imgMsg imgModel
                MkImageModel imgModel,
                Cmd.map MkImageMessage imgCmd
            | _ -> failwith "Invalid message"
