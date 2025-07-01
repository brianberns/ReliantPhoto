namespace Reliant.Photo

open System.IO
open Elmish

/// Messages that can change the model.
type Message =
    | MkDirectoryMessage of DirectoryMessage
    | MkImageMessage of ImageMessage
    | SwitchToImage of FileInfo
    | SwitchToDirectory

module Message =

    /// Browses to the given image or directory.
    let init arg =
        let model = Model.init arg
        let cmd =
            if model.ImageModelOpt.IsNone then
                Cmd.ofMsg (MkDirectoryMessage LoadDirectory)
            else
                Cmd.ofMsg (MkImageMessage LoadImage)
        model, cmd

    /// Updates the given model based on the given message.
    let update message model =
        match message, model.ImageModelOpt with

                // process directory message
            | MkDirectoryMessage dirMsg, None ->
                let dirModel, dirCmd =
                    DirectoryMessage.update dirMsg model.DirectoryModel
                { model with DirectoryModel = dirModel },
                Cmd.map MkDirectoryMessage dirCmd

                // process image message
            | MkImageMessage imgMsg, Some imgModel ->
                let imgModel, imgCmd =
                    ImageMessage.update imgMsg imgModel
                { model with ImageModelOpt = Some imgModel },
                Cmd.map MkImageMessage imgCmd

                // switch to image mode
            | SwitchToImage file, None ->
                { model with
                    ImageModelOpt = Some (ImageModel.init file) },
                Cmd.ofMsg (MkImageMessage LoadImage)

                // switch to directory mode
            | SwitchToDirectory, Some _ ->
                { model with ImageModelOpt = None },
                Cmd.none

                // ignore stale message
            | MkDirectoryMessage (ImagesLoaded _), Some _ ->
                model, Cmd.none

            | _ -> failwith $"Invalid message {message} for model {model}"
