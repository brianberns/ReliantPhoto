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

    /// Initializes model.
    let init = function
        | Choice1Of2 dir ->
            let dirModel, dirCmd = DirectoryMessage.init dir
            DirectoryMode (dirModel, None),
            Cmd.map MkDirectoryMessage dirCmd
        | Choice2Of2 file ->
            let imgModel, imgCmd = ImageMessage.init file
            ImageMode (None, imgModel),
            Cmd.map MkImageMessage imgCmd

    /// Handles a directory-mode message.
    let private onDirectoryMessage dirMsg = function
        | DirectoryMode (dirModel, imgModelOpt) ->
            let dirModel, dirCmd =
                DirectoryMessage.update dirMsg dirModel
            DirectoryMode (dirModel, imgModelOpt),
            Cmd.map MkDirectoryMessage dirCmd
        | _ -> failwith "Invalid state"

    /// Handles an image-mode message.
    let private onImageMessage dpiScale imgMsg = function
        | ImageMode (dirModelOpt, imgModel) ->
            let imgModel, imgCmd =
                ImageMessage.update dpiScale imgMsg imgModel
            ImageMode (dirModelOpt, imgModel),
            Cmd.map MkImageMessage imgCmd
        | _ -> failwith "Invalid state"

    /// Loads the given image.
    let private onLoadImage file = function
        | ImageMode _ as model ->   // to-do: refactor
            let imgCmd = ImageMessage.loadImageCommand file
            model,
            Cmd.map MkImageMessage imgCmd
        | DirectoryMode (dirModel, None) ->
            let imgModel, imgCmd = ImageMessage.init file
            ImageMode (Some dirModel, imgModel),
            Cmd.map MkImageMessage imgCmd
        | DirectoryMode (dirModel, Some imgModel) ->
            let imgCmd = ImageMessage.loadImageCommand file
            ImageMode (Some dirModel, imgModel),
            Cmd.map MkImageMessage imgCmd

    /// Switches to directory mode.
    let private onSwitchToDirectory = function
        | ImageMode (None, imgModel) ->
            let dirModel, dirCmd =
                DirectoryMessage.init imgModel.File.Directory
            DirectoryMode (dirModel, None),
            Cmd.map MkDirectoryMessage dirCmd
        | ImageMode (Some dirModel, imgModel) ->
            let dirModel, dirCmd =
                if dirModel.Directory.FullName
                    = imgModel.File.Directory.FullName then
                    dirModel, Cmd.none
                else
                    DirectoryMessage.init imgModel.File.Directory
            DirectoryMode (dirModel, Some imgModel),
            Cmd.map MkDirectoryMessage dirCmd
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
