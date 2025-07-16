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

    /// Creates a command to load an image from the given file.
    let private loadImageCommand file =
        Cmd.OfAsync.perform
            (fun () -> async { return file })   // give image view a chance to initialize first
            ()
            (LoadImage >> MkImageMessage)

    /// Browses to the given directory or image.
    let init entity =
        let model = Model.init entity
        let cmd =
            Cmd.batch [

                Cmd.ofMsg (MkDirectoryMessage LoadDirectory)

                match entity with
                    | Choice1Of2 _ -> ()
                    | Choice2Of2 file ->
                        loadImageCommand file
            ]
        model, cmd

    /// Handles a directory-mode message.
    let private onDirectoryMessage dirMsg model =
        let dirModel, dirCmd =
            DirectoryMessage.update dirMsg model.DirectoryModel
        { model with DirectoryModel = dirModel },
        Cmd.map MkDirectoryMessage dirCmd

    /// Handles an image-mode message.
    let private onImageMessage dpiScale imgMsg model =
        let imgModel, imgCmd =
            ImageMessage.update dpiScale imgMsg model.ImageModel
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
                init (Choice2Of2 file)
