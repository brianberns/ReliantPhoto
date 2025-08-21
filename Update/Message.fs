namespace Reliant.Photo

open System.IO
open Elmish

/// Messages that can change the model.
type Message =

    /// A directory-mode message.
    | MkDirectoryMessage of DirectoryMessage

    /// An image-mode message.
    | MkImageMessage of ImageMessage

    /// Load the given image.
    | LoadImage of FileInfo

    /// Switch to directory mode.
    | SwitchToDirectory

module Message =

    /// Initializes directory model.
    let private initDirectory dir =
        let dirModel, dirCmd = DirectoryMessage.init dir
        MkDirectoryModel dirModel,
        Cmd.map MkDirectoryMessage dirCmd

    /// Initializes image model.
    let private initImage file =
        let imgModel, imgCmd = ImageMessage.init file
        MkImageModel imgModel,
        Cmd.map MkImageMessage imgCmd

    /// Initializes model.
    let init = function
        | Choice1Of2 dir -> initDirectory dir
        | Choice2Of2 file -> initImage file

    /// Handles a directory-mode message.
    let private onDirectoryMessage dirMsg = function
        | MkDirectoryModel dirModel ->
            let dirModel, dirCmd =
                DirectoryMessage.update dirMsg dirModel
            MkDirectoryModel dirModel,
            Cmd.map MkDirectoryMessage dirCmd
        | _ -> failwith "Invalid state"

    /// Handles an image-mode message.
    let private onImageMessage dpiScale imgMsg = function
        | MkImageModel imgModel ->
            let imgModel, imgCmd =
                ImageMessage.update dpiScale imgMsg imgModel
            MkImageModel imgModel,
            Cmd.map MkImageMessage imgCmd
        | _ -> failwith "Invalid state"

    /// Loads the given image.
    let private onLoadImage file (_model : Model) =
        initImage file

    /// Switches to directory mode.
    let private onSwitchToDirectory = function
        | MkImageModel imgModel ->
            initDirectory imgModel.File.Directory
        | _ -> failwith "Invalid state"

    /// Updates the given model based on the given message.
    let update dpiScale message model =
        match message with
            | MkDirectoryMessage dirMsg ->
                onDirectoryMessage dirMsg model
            | MkImageMessage imgMsg ->
                onImageMessage dpiScale imgMsg model
            | LoadImage file ->
                onLoadImage file model
            | SwitchToDirectory ->
                onSwitchToDirectory model
