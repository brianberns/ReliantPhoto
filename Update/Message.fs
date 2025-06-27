namespace Reliant.Photo

open System.IO
open Elmish

/// Messages that can change the model.
type Message =
    | MkDirectoryMessage of DirectoryMessage
    | MkImageMessage of ImageMessage
    | SwitchToImage of FileInfo
    | SwitchToDirectory of DirectoryInfo

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

                // process directory message
            | MkDirectoryMessage dirMsg, MkDirectoryModel dirModel ->
                let dirModel, dirCmd =
                    DirectoryMessage.update setTitle dirMsg dirModel
                MkDirectoryModel dirModel,
                Cmd.map MkDirectoryMessage dirCmd

                // process image message
            | MkImageMessage imgMsg, MkImageModel imgModel ->
                let imgModel, imgCmd =
                    ImageMessage.update setTitle imgMsg imgModel
                MkImageModel imgModel,
                Cmd.map MkImageMessage imgCmd

                // switch to image mode
            | SwitchToImage file, _ ->
                MkImageModel (ImageModel.init file),
                Cmd.ofMsg (MkImageMessage LoadImage)

                // switch to directory mode
            | SwitchToDirectory dir, _ ->
                MkDirectoryModel (DirectoryModel.init dir),
                Cmd.ofMsg (MkDirectoryMessage LoadDirectory)

                // ignore for now
            | MkDirectoryMessage (ImagesLoaded _), MkImageModel _ ->
                model, Cmd.none

            | _ -> failwith $"Invalid message {message} for model {model}"
