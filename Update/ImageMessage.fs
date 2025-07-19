namespace Reliant.Photo

open System.IO

open Elmish

open Avalonia
open Avalonia.Media.Imaging

open Aether.Operators

/// Messages that can update the image model.
type ImageMessage =

    /// Size of the image container has been set or updated.
    | ContainerSized of Size

    /// Load image from file, if possible.
    | LoadImage of FileInfo

    /// Image has been loaded.
    | ImageLoaded of Bitmap

    /// Browse to previous image in directory, if possible.
    | PreviousImage

    /// Browse to next image in directory, if possible.
    | NextImage

    /// Pointer wheel position has changed.
    | WheelZoom of int (*sign*) * Point (*pointer position*)

    /// Load error occurred.
    | HandleLoadError of string

module Cmd =

    /// Creates a command that handles an async result.
    let ofAsyncResult task arg ofSuccess ofError =
        Cmd.OfAsync.perform
            task arg
            (function
                | Ok success -> ofSuccess success
                | Error error -> ofError error)

module ImageMessage =

    /// Creates a command to load an image from the given file.
    /// This is an asynchronous command to allow the image
    /// view to create a container before loading its first
    /// image.
    let loadImageCommand file =
        Cmd.OfAsync.perform
            async.Return
            file
            LoadImage

    /// Initializes model to start loading the given file, if
    /// specified.
    let init fileOpt =
        let model = ImageModel.init ()
        let cmd =
            fileOpt
                |> Option.map loadImageCommand
                |> Option.defaultValue Cmd.none
        model, cmd

    /// Updates layout due to container resize.
    let private updateLayout dpiScale containerSize loaded =

            // keep zoom scale constant?
        let zoomScaleOpt =
            if loaded.ZoomScaleLock then
                Some loaded.ZoomScale
            else None

            // get layout for new container size
        let offset, zoomScale =
            ImageLayout.getImageLayout
                dpiScale
                containerSize
                loaded.Bitmap
                (Some loaded.Offset)
                zoomScaleOpt

        {
            loaded with
                Offset = offset
                ZoomScale = zoomScale
        }

    /// Sets or updates container size. This occurs when the
    /// container is first created (before it contains an
    /// image), and any time the container is resized by the
    /// user.
    let private onContainerSized dpiScale containerSize model =
        let inited =
            { ContainerSize = containerSize }
        let model =
            match model with

                    // creation: set container size
                | Uninitialized -> Initialized inited

                    // resize: update layout and container size
                | Loaded loaded ->
                    loaded
                        |> updateLayout dpiScale containerSize
                        |> Loaded
                        |> inited ^= ImageModel.Initialized_

                    // resize: just update container size
                | _ ->
                    model
                        |> inited ^= ImageModel.Initialized_
        model, Cmd.none

    /// Browses to and starts loading a file, if possible.
    let private browse inited incr fromFile =
        let model = ImageModel.browse inited incr fromFile
        let cmd =
            match model with
                | Browsed browsed ->
                    Cmd.ofAsyncResult
                        (ImageFile.tryLoadImage None)
                        browsed.File
                        ImageLoaded
                        HandleLoadError
                | BrowseError _ -> Cmd.none
                | _ -> failwith "Invalid state"
        model, cmd

    /// Starts loading an image from the given file.
    let private onLoadImage file model =
        let inited = model ^. ImageModel.Initialized_
        browse inited 0 file

    /// Sets image's bitmap.
    let private onImageLoaded dpiScale bitmap model =
        let model =
            match model with
                | Browsed browsed ->

                        // get default layout
                    let offset, zoomScale =
                        let containerSize =
                            (browsed ^. BrowsedImage.ContainerSize_)
                        ImageLayout.getImageLayout
                            dpiScale containerSize bitmap None None

                    Loaded {
                        Browsed = browsed
                        Bitmap = bitmap
                        Offset = offset
                        ZoomScale = zoomScale
                        ZoomScaleLock = false
                    }
                | _ -> failwith "Invalid state"
        model, Cmd.none

    /// Browses to a file, if possible.
    let private onBrowse incr model =
        let inited = model ^. ImageModel.Initialized_
        browse inited incr model.File

    /// Updates zoom scale and origin.
    let private onWheelZoom
        dpiScale sign pointerPos = function

        | Loaded loaded ->

                // increment/decrement zoom scale
            let zoomScale, zoomScaleLock =
                ImageLayout.incrementZoomScale
                    dpiScale sign loaded

                // update image offset
            let offset =
                ImageLayout.updateImageOffset
                    dpiScale pointerPos zoomScale loaded

                // update model
            let model =
                Loaded {
                    loaded with
                        Offset = offset
                        ZoomScale = zoomScale
                        ZoomScaleLock = zoomScaleLock
                }
            model, Cmd.none

        | _ -> failwith "Invalid state"

    /// Handles a load error.
    let private onHandleLoadError error = function
        | Browsed browsed ->
            let model =
                LoadError {
                    Browsed = browsed
                    Message = error
                }
            model, Cmd.none
        | _ -> failwith "Invalid state"

    /// Updates the given model based on the given message.
    let update dpiScale message model =
        match message with

                // set/update container size
            | ContainerSized containerSize ->
                 onContainerSized dpiScale containerSize model

                // start loading an image
            | LoadImage file ->
                onLoadImage file model

                // finish loading an image
            | ImageLoaded bitmap ->
                onImageLoaded dpiScale bitmap model

                // browse to previous image
            | PreviousImage  ->
                onBrowse -1 model

                // browse to next image
            | NextImage  ->
                onBrowse 1 model

                // update zoom
            | WheelZoom (sign, pointerPos) ->
                onWheelZoom dpiScale sign pointerPos model

                // handle load error
            | HandleLoadError error ->
                onHandleLoadError error model
