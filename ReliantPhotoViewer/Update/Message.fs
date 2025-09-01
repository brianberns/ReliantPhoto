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
    let private initDirectory dir imgModelOpt =
        let dirModel, dirCmd = DirectoryMessage.init dir
        DirectoryMode (dirModel, imgModelOpt),
        Cmd.map MkDirectoryMessage dirCmd

    /// Initializes image model.
    let private initImage file dirModelOpt =
        let imgModel, imgCmd = ImageMessage.init file
        ImageMode (dirModelOpt, imgModel),
        Cmd.map MkImageMessage imgCmd

    /// Initializes model.
    let init = function
        | Choice1Of2 dir -> initDirectory dir None
        | Choice2Of2 file -> initImage file None

    /// Handles a directory message.
    let private onDirectoryMessage dirMsg model =

        let handle dirModel mkModel =
            let dirModel, dirCmd =
                DirectoryMessage.update dirMsg dirModel
            mkModel dirModel,
            Cmd.map MkDirectoryMessage dirCmd

        match model with
            | DirectoryMode (dirModel, imgModelOpt) ->
                handle dirModel (fun dirModel ->
                    DirectoryMode (dirModel, imgModelOpt))
            | ImageMode (Some dirModel, imgModel) ->   // e.g. add/delete file
                handle dirModel (fun dirModel ->
                    ImageMode (Some dirModel, imgModel))
            | ImageMode (None, _) ->
                model, Cmd.none

    /// Handles an image message.
    let private onImageMessage dpiScale imgMsg = function
        | ImageMode (dirModelOpt, imgModel) ->
            let imgModel, imgCmd =
                ImageMessage.update dpiScale imgMsg imgModel
            ImageMode (dirModelOpt, imgModel),
            Cmd.map MkImageMessage imgCmd
        | _ -> failwith "Invalid state"

    /// Reuses the given directory model, if possible.
    let private tryReuseDirectoryModel
        (file : FileInfo) dirModel =
        if FileSystemInfo.same
            dirModel.Directory file.Directory then
            Some dirModel
        else None

    /// Loads the given image.
    let private loadImage (file : FileInfo) dirModelOpt imgModel =
        ImageMode (dirModelOpt, imgModel),
        [
            Cmd.ofMsg ImageMessage.UnloadImage   // avoid flashing previous image
            ImageMessage.loadImageCommand file
        ]
            |> Cmd.batch
            |> Cmd.map MkImageMessage

    /// Loads the given image.
    let private onLoadImage file = function

            // create image model
        | DirectoryMode (dirModel, None) ->
            let dirModelOpt =
                tryReuseDirectoryModel file dirModel
            initImage file dirModelOpt

            // reuse image model
        | DirectoryMode (dirModel, Some imgModel) ->
            let dirModelOpt =
                tryReuseDirectoryModel file dirModel
            loadImage file dirModelOpt imgModel

            // reuse image model
        | ImageMode (dirModelOpt, imgModel) ->
            let dirModelOpt =
                dirModelOpt
                    |> Option.bind (
                        tryReuseDirectoryModel file)
            loadImage file dirModelOpt imgModel

    /// Switches to directory mode.
    let private onSwitchToDirectory = function
        | ImageMode (Some dirModel, imgModel) ->
            assert(FileSystemInfo.same
                dirModel.Directory
                imgModel.File.Directory)
            DirectoryMode (dirModel, Some imgModel),
            Cmd.none
        | ImageMode (None, imgModel) ->
            initDirectory
                imgModel.File.Directory
                (Some imgModel)
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
