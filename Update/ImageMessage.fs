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

    /// Load error occurred.
    | HandleLoadError of string

    /// Browse to previous image in directory, if possible.
    | PreviousImage

    /// Browse to next image in directory, if possible.
    | NextImage

    /// Pointer wheel position has changed.
    | WheelZoom of int (*sign*) * Point (*pointer position*)

    /// Pointer pan has started.
    | PanStart of Point

    /// Pointer pan has moved.
    | PanMove of Point

    /// Pointer pan has ended.
    | PanEnd

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
    let private updateLayout loaded =

            // keep zoom scale constant?
        let zoomScaleOpt =
            if loaded.ZoomScaleLock then
                Some loaded.ZoomScale
            else None

            // get layout for new container size
        let offset, zoomScale =
            ImageLayout.getImageLayout
                (loaded ^. LoadedImage.ContainerSize_)
                loaded.BitmapSize
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
    let private onContainerSized containerSize model =
        let inited =
            { ContainerSize = containerSize }
        let model =
            match model with

                    // creation: set container size
                | Uninitialized -> Initialized inited

                    // resize: update container size and layout
                | Loaded loaded ->
                    loaded
                        |> inited ^= LoadedImage.Initialized_
                        |> updateLayout
                        |> Loaded

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

    /// Sets image's bitmap.
    let private onImageLoaded
        (dpiScale : float) (bitmap : Bitmap) model =
        let model =
            match model with
                | Browsed browsed ->

                        // get default layout
                    let bitmapSize =
                        bitmap.PixelSize.ToSize(dpiScale)
                    let offset, zoomScale =
                        let containerSize =
                            (browsed ^. BrowsedImage.ContainerSize_)
                        ImageLayout.getImageLayout
                            containerSize bitmapSize None None

                    Loaded {
                        Browsed = browsed
                        Bitmap = bitmap
                        BitmapSize = bitmapSize
                        Offset = offset
                        ZoomScale = zoomScale
                        ZoomScaleLock = false
                        PanOpt = None
                    }
                | _ -> failwith "Invalid state"
        model, Cmd.none

    /// Browses to a file, if possible.
    let private onBrowse incr model =
        let inited = model ^. ImageModel.Initialized_
        browse inited incr model.File

    /// Updates zoom scale and origin.
    let private onWheelZoom sign pointerPos = function

        | Loaded loaded ->

                // increment/decrement zoom scale
            let zoomScale, zoomScaleLock =
                ImageLayout.incrementZoomScale sign loaded

                // update image offset
            let offset =
                ImageLayout.updateImageOffset
                    pointerPos zoomScale loaded

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

    /// Starts panning.
    let private onPanStart pointerPos = function
        | Loaded loaded ->
            let pan =
                {
                    ImageOffset = loaded.Offset
                    PointerPos = pointerPos
                }
            let model =
                Loaded {
                    loaded with PanOpt = Some pan }
            model, Cmd.none
        | _ -> failwith "Invalid state"

    /// Moves the image during a pan.
    let private moveImage pointerPos pan loaded =
        let offset =
            pan.ImageOffset
                + (pointerPos - pan.PointerPos)
        { loaded with Offset = offset }

    /// Continues panning.
    let private onPanMove pointerPos = function
        | Loaded loaded as model ->
            match loaded.PanOpt with
                | Some pan ->
                    let model =
                        loaded
                            |> moveImage pointerPos pan
                            |> Loaded
                    model, Cmd.none
                | None -> model, Cmd.none
        | _ -> failwith "Invalid state"

    /// Ends panning.
    let private onPanEnd = function
        | Loaded loaded ->
            let model =
                Loaded {
                    loaded with PanOpt = None }
            model, Cmd.none
        | _ -> failwith "Invalid state"

    /// Updates the given model based on the given message.
    let update dpiScale message model =
        match message with

                // set/update container size
            | ContainerSized containerSize ->
                 onContainerSized containerSize model

                // start loading an image
            | LoadImage file ->
                onLoadImage file model

                // finish loading an image
            | ImageLoaded bitmap ->
                onImageLoaded dpiScale bitmap model

                // handle load error
            | HandleLoadError error ->
                onHandleLoadError error model

                // browse to previous image
            | PreviousImage  ->
                onBrowse -1 model

                // browse to next image
            | NextImage  ->
                onBrowse 1 model

                // update zoom
            | WheelZoom (sign, pointerPos) ->
                onWheelZoom sign pointerPos model

                // start pan
            | PanStart pointerPos ->
                onPanStart pointerPos model

                // continue pan
            | PanMove pointerPos ->
                onPanMove pointerPos model

                // finish pan
            | PanEnd ->
                onPanEnd model
