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

    /// Initializes model.
    let init arg =

            // extract arguments
        let dir, fileOpt =
            match arg with
                | Choice1Of2 dir -> dir, None
                | Choice2Of2 (file : FileInfo) ->
                    file.Directory, Some file

            // initialize sub-models
        let dirModel, dirCmd = DirectoryMessage.init dir
        let imgModel, imgCmd = ImageMessage.init fileOpt

            // build top-level model and command
        let model =
            {
                DirectoryModel = dirModel
                ImageModel = imgModel
                Mode = Mode.Directory
            }
        let cmd =
            Cmd.batch [
                Cmd.map MkDirectoryMessage dirCmd
                Cmd.map MkImageMessage imgCmd
            ]
        model, cmd

    /// Handles a directory-mode message.
    let private onDirectoryMessage dirMsg model =
        let dirModel, dirCmd =
            DirectoryMessage.update
                dirMsg model.DirectoryModel
        { model with DirectoryModel = dirModel },
        Cmd.map MkDirectoryMessage dirCmd

    /// Handles an image-mode message.
    let private onImageMessage dpiScale imgMsg model =
        let imgModel, imgCmd =
            ImageMessage.update
                dpiScale imgMsg model.ImageModel
        { model with ImageModel = imgModel },
        Cmd.map MkImageMessage imgCmd

    /// Switches to image mode.
    let private onSwitchToImage file model =
        { model with Mode = Mode.Image },
        file
            |> ImageMessage.loadImageCommand
            |> Cmd.map MkImageMessage

    /// Switches to directory mode.
    let private onSwitchToDirectory model =
        { model with Mode = Mode.Directory },
        Cmd.none

    /// Opens the given file in its directory.
    let private onImageSelected (file : FileInfo) model =
        let dirModel, dirCmd =
            DirectoryMessage.init file.Directory
        let model, cmd =
            { model with
                DirectoryModel = dirModel }
                |> onSwitchToImage file
        model,
        Cmd.batch [
            Cmd.map MkDirectoryMessage dirCmd
            cmd
        ]

    /// Updates the given model based on the given message.
    let update dpiScale message model =
        match message with
            | MkDirectoryMessage dirMsg ->
                onDirectoryMessage dirMsg model
            | MkImageMessage imgMsg ->
                onImageMessage dpiScale imgMsg model
            | SwitchToImage file ->
                onSwitchToImage file model
            | SwitchToDirectory ->
                onSwitchToDirectory model
            | ImageSelected file ->
                onImageSelected file model
