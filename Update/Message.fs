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
            if model.ImageModelOpt.IsSome then
                Cmd.ofMsg (MkImageMessage LoadImage)
            else
                Cmd.ofMsg (MkDirectoryMessage LoadDirectory)
        model, cmd

    /// Updates the given model based on the given message.
    let update setTitle message model =
        match message, model.ImageModelOpt with
            | MkDirectoryMessage msg, _ ->
                let submodel, subcmd =
                    DirectoryMessage.update
                        msg
                        model.DirectoryModel
                { model with
                    DirectoryModel = submodel },
                Cmd.map MkDirectoryMessage subcmd
            | MkImageMessage msg, Some imageModel ->
                let submodel, subcmd =
                    ImageMessage.update setTitle
                        msg
                        imageModel
                { model with
                    ImageModelOpt = Some submodel },
                Cmd.map MkImageMessage subcmd
            | _ -> failwith "Invalid message"
