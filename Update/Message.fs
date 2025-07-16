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

    /// Creates a command to load an image from the given
    /// file. This is an asynchronous command in order to
    /// allow the image view to initialize before loading
    /// its first image.
    let private loadImageCommand file =
        Cmd.OfAsync.perform
            async.Return
            file
            (LoadImage >> MkImageMessage)

    /// Initializes model.
    let init arg =

        let dir, fileOpt =
            match arg with
                | Choice1Of2 dir -> dir, None
                | Choice2Of2 (file : FileInfo) ->
                    file.Directory, Some file

        let model = Model.init dir
        let cmd =
            Cmd.batch [

                Cmd.ofMsg (MkDirectoryMessage LoadDirectory)

                match fileOpt with
                    | Some file ->
                        loadImageCommand file
                    | None -> ()
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
        loadImageCommand file

    /// Switches to directory mode.
    let private onSwitchToDirectory model =
        { model with Mode = Mode.Directory },
        Cmd.none

    /// Opens the given file in its directory.
    let private onImageSelected (file : FileInfo) model =
        let model =
            { model with
                DirectoryModel =
                    DirectoryModel.init file.Directory }
        onSwitchToImage file model

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
