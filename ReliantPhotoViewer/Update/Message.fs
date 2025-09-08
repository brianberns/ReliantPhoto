namespace Reliant.Photo

open System.IO
open Elmish

/// Messages that can change the model.
type Message =

    /// A directory-mode message.
    | MkDirectoryMessage of DirectoryMessage

    /// An image-mode message.
    | MkImageMessage of ImageMessage

    /// Load the given directory.
    | LoadDirectory of DirectoryInfo

    /// Load the given image.
    | LoadImage of FileInfo

    /// Switch to directory mode.
    | SwitchToDirectory

module Message =

    /// Initializes directory model.
    let private initDirectory dir imgModel =
        let dirModel, dirCmd = DirectoryMessage.init dir
        DirectoryMode (dirModel, imgModel),
        Cmd.map MkDirectoryMessage dirCmd

    /// Initializes image model.
    let private initImage dpiScale file dirModelOpt =
        let imgModel, imgCmd =
            ImageMessage.init dpiScale file
        ImageMode (dirModelOpt, imgModel),
        Cmd.map MkImageMessage imgCmd

    /// Initializes model.
    let init dpiScale = function
        | Choice1Of2 dir ->
            let imgModel = ImageModel.init dpiScale
            initDirectory dir imgModel
        | Choice2Of2 file ->
            initImage dpiScale file None

    /// Handles a directory message.
    let private onDirectoryMessage dirMsg model =

        let handle dirModel mkModel =
            let dirModel, dirCmd =
                DirectoryMessage.update dirMsg dirModel
            mkModel dirModel,
            Cmd.map MkDirectoryMessage dirCmd

        match model with
            | DirectoryMode (dirModel, imgModel) ->
                handle dirModel (fun dirModel ->
                    DirectoryMode (dirModel, imgModel))
            | ImageMode (Some dirModel, imgModel) ->   // e.g. add/delete file
                handle dirModel (fun dirModel ->
                    ImageMode (Some dirModel, imgModel))
            | ImageMode (None, _) ->
                model, Cmd.none

    /// Handles an image message.
    let private onImageMessage imgMsg = function
        | ImageMode (dirModelOpt, imgModel) ->
            let imgModel, imgCmd =
                ImageMessage.update imgMsg imgModel
            ImageMode (dirModelOpt, imgModel),
            Cmd.map MkImageMessage imgCmd
        | _ -> failwith "Invalid state"

    /// Loads the given directory.
    let private onLoadDirectory dir = function
        | DirectoryMode (_, imgModel) ->
            initDirectory dir imgModel
        | _ -> failwith "Invalid state"

    /// Loads the given image.
    let private loadImage file dirModelOpt imgModel =
        ImageMode (dirModelOpt, imgModel),
        [
            Cmd.ofMsg ImageMessage.UnloadImage   // avoid flashing previous image
            ImageMessage.loadImageCommand file
        ]
            |> Cmd.batch
            |> Cmd.map MkImageMessage

    /// Loads the given image.
    let private onLoadImage file = function

        | ImageMode (_, imgModel) ->
            loadImage file None imgModel

            // switch to image mode
        | DirectoryMode (dirModel, imgModel) ->
            assert(
                DirectoryInfo.same
                    file.Directory dirModel.Directory)
            let dirModelOpt = Some dirModel
            match imgModelOpt with
                | Some imgModel ->
                    loadImage file dirModelOpt imgModel
                | None ->
                    initImage file dirModelOpt

    /// Switches to directory mode.
    let private onSwitchToDirectory = function
        | ImageMode (Some dirModel, imgModel) ->
            assert(DirectoryInfo.same
                dirModel.Directory
                imgModel.File.Directory)
            DirectoryMode (dirModel, imgModel),
            Cmd.none
        | ImageMode (None, imgModel) ->
            initDirectory
                imgModel.File.Directory
                imgModel
        | _ -> failwith "Invalid state"

    /// Updates the given model based on the given message.
    let update message model =
        match message with
            | MkDirectoryMessage dirMsg ->
                onDirectoryMessage dirMsg model
            | MkImageMessage imgMsg ->
                onImageMessage imgMsg model
            | LoadDirectory dir ->
                onLoadDirectory dir model
            | LoadImage file ->
                onLoadImage file model
            | SwitchToDirectory ->
                onSwitchToDirectory model
