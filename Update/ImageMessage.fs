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

    /// Creates a command to load an image from the given
    /// file. This is an asynchronous command in order to
    /// allow the image view to create a container before
    /// loading its first image.
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

    /// Default zoom scale for the given bitmap in the given container.
    let private getDefaultZoomScale
        (dpiScale : float)
        (containerSize : Size)
        (bitmap : Bitmap) =
        let ratio = (containerSize * dpiScale) / bitmap.Size
        Array.min [| ratio.X; ratio.Y; 1.0 |]

    /// Sets or updates container size.
    let private onContainerSized dpiScale containerSize model =
        let inited =
            { ContainerSize = containerSize }
        let model =
            match model with

                    // set container size
                | Uninitialized -> Initialized inited

                    // update container size and corresponding zoom scale
                | Loaded loaded ->
                    let zoomScale =
                        getDefaultZoomScale
                            dpiScale containerSize loaded.Bitmap
                    Loaded {
                        loaded with
                            ZoomScale = zoomScale }
                        |> inited ^= ImageModel.Initialized_

                    // just update container size
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
                    let zoomScale =
                        let containerSize =
                            browsed.Initialized.ContainerSize
                        getDefaultZoomScale
                            dpiScale containerSize bitmap
                    Loaded {
                        Browsed = browsed
                        Bitmap = bitmap
                        ZoomScale = zoomScale
                        ZoomOrigin = RelativePoint(0.5, 0.5, RelativeUnit.Relative)
                    }
                | _ -> failwith "Invalid state"
        model, Cmd.none

    /// Browses to a file, if possible.
    let private onBrowse incr model =
        let inited = model ^. ImageModel.Initialized_
        browse inited incr model.File

    /// Acceptable rounding error.
    let private epsilon = 0.001

    /// Updates zoom scale based on user input.
    let private updateZoomScale zoomSign zoomScaleFloor loaded =
        assert(abs zoomSign = 1)

        let factor = 1.1
        let zoomScale = loaded.ZoomScale

        let newScale =
            if zoomSign >= 0 then zoomScale * factor
            else
                let newScale = zoomScale / factor
                if zoomScaleFloor - newScale > epsilon then
                    zoomScale   // don't jump suddenly
                else newScale

            // snap to 1.0?
        if newScale > 1.0 && zoomScale < 1.0
            || newScale < 1.0 && zoomScale > 1.0 then
            1.0
        else newScale

    /// Updates zoom origin based on user input.
    (*
    let private updateZoomOrigin (pointerPos : Point) displayed =
        let imageSize = displayed.ImageSize
        let originX = pointerPos.X / imageSize.Width
        let originY = pointerPos.Y / imageSize.Height
        RelativePoint(originX, originY, RelativeUnit.Relative)
    *)

    /// Updates zoom scale and origin.
    let private onWheelZoom dpiScale sign pointerPos = function

        | Loaded loaded ->

                // update zoom scale and origin
            let zoomScale =
                let zoomScaleFloor =
                    let containerSize =
                        loaded.Browsed.Initialized.ContainerSize
                    getDefaultZoomScale
                        dpiScale containerSize loaded.Bitmap
                updateZoomScale sign zoomScaleFloor loaded
            let zoomOrigin = RelativePoint(0.5, 0.5, RelativeUnit.Relative) // updateZoomOrigin pointerPos displayed

                // update model
            let model =
                Loaded {
                    loaded with
                        ZoomScale = zoomScale
                        ZoomOrigin = zoomOrigin
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
