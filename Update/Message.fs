namespace Reliant.Photo

open System.IO
open Elmish

/// Messages that can change the model.
type Message =

    /// A directory-mode message.
    | MkDirectoryMessage of DirectoryMessage

    /// An image-mode message.
    | MkImageMessage of ImageMessage

    /// Switch from directory mode to image mode.
    | SwitchToImage of FileInfo

    /// Switch from image mode to directory mode.
    | SwitchToDirectory

    /// User has selected an image to load from the file
    /// system.
    | ImageSelected of FileInfo

module Message =

    /// Browses to the given image or directory.
    let init arg =
        let model = Model.init arg
        let cmd =
            Cmd.batch [
                Cmd.ofMsg (MkDirectoryMessage LoadDirectory)
                if model.ImageModelOpt.IsSome then
                    Cmd.ofMsg (MkImageMessage LoadImage)
            ]
        model, cmd

    /// Handles a directory-mode message.
    let private onDirectoryMessage dirMsg model =
        let dirModel, dirCmd =
            DirectoryMessage.update dirMsg model.DirectoryModel
        { model with DirectoryModel = dirModel },
        Cmd.map MkDirectoryMessage dirCmd

    /// Handles an image-mode message.
    let private onImageMessage dpiScale imgMsg imgModel model =
        let imgModel, imgCmd =
            ImageMessage.update dpiScale imgMsg imgModel
        { model with ImageModelOpt = Some imgModel },
        Cmd.map MkImageMessage imgCmd

    /// Switches to image mode.
    let private onSwitchToImage file model =
        let imgModel = ImageModel.init file
        let model = 
            { model with
                ImageModelOpt = Some imgModel }
        let cmd =
            if imgModel.IsBrowseError then Cmd.none
            else Cmd.ofMsg (MkImageMessage LoadImage)
        model, cmd

    /// Switches to directory mode.
    let private onSwitchToDirectory model =
        { model with ImageModelOpt = None },
        Cmd.none

    /// Updates the given model based on the given message.
    let update dpiScale message model =
        match message, model.ImageModelOpt with
            | MkDirectoryMessage dirMsg, _ ->
                onDirectoryMessage dirMsg model
            | MkImageMessage imgMsg, Some imgModel ->
                onImageMessage dpiScale imgMsg imgModel model
            | SwitchToImage file, None ->
                onSwitchToImage file model
            | SwitchToDirectory, Some _ ->
                onSwitchToDirectory model
            | ImageSelected file, _ ->
                init file
            | _ -> failwith $"Invalid message {message} for model {model}"
