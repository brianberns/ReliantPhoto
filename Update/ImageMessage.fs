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
    | ImageLoaded of FileInfo * Bitmap

    /// Unload current image.
    | UnloadImage

    /// Load error occurred.
    | HandleLoadError of FileInfo * string

    /// Pointer wheel position has changed.
    | WheelZoom of int (*sign*) * Point (*pointer position*)

    /// Zoom image to actual size.
    | ZoomToActualSize

    /// Pointer pan has started.
    | PanStart of Point

    /// Pointer pan has moved.
    | PanMove of Point

    /// Pointer pan has ended.
    | PanEnd

    /// Loaded image has been situated in the current directory.
    | ImageSituated
        of Option<FileInfo> (*previous image*)
            * Option<FileInfo> (*next image*)

    /// Delete the current file.
    | DeleteFile

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

    /// Initializes model to start loading the given file.
    let init file =
        ImageModel.init (),
        loadImageCommand file

    /// Updates layout due to container resize.
    let private resize containerSize loaded =

            // keep zoom scale?
        let zoomScaleOpt =
            loaded
                ^. LoadedImage.Initialized_
                |> InitializedContainer.tryGetLockedZoomScale

            // get layout for new container size
        let offset, zoomScale =
            ImageLayout.getImageLayout
                containerSize
                loaded.BitmapSize
                (Some loaded.Offset)
                zoomScaleOpt

        loaded
            |> containerSize ^= LoadedImage.ContainerSize_
            |> offset ^= LoadedImage.Offset_
            |> zoomScale ^= LoadedImage.ZoomScale_

    /// Sets or updates container size. This occurs when the
    /// container is first created (before it contains an
    /// image), and any time the container is resized by the
    /// user.
    let private onContainerSized containerSize model =
        let model =
            match model with

                    // creation: set container size
                | Uninitialized ->
                    Initialized (
                        InitializedContainer.create containerSize)

                    // resize: update container size and layout
                | Loaded_ loaded ->
                    let loaded = resize containerSize loaded
                    model
                        |> loaded ^= ImageModel.Loaded_

                    // resize: just update container size
                | _ ->
                    model
                        |> containerSize ^= ImageModel.ContainerSize_
        model, Cmd.none

    /// Starts loading an image from the given file.
    let private onLoadImage file (model : ImageModel) =
        let cmd =
            Cmd.OfAsync.perform
                ImageFile.tryLoadImage
                file
                (function
                    | Ok bitmap -> ImageLoaded (file, bitmap)
                    | Error msg -> HandleLoadError (file, msg))
        model, cmd

    /// Unloads the current image, if any.
    let private onUnloadImage model =
        let model =
            model ^. ImageModel.TryInitialized_
                |> Option.map (fun inited ->
                    inited.ContainerSize   // keep only the container size
                        |> InitializedContainer.create
                        |> Initialized)
                |> Option.defaultValue model
        model, Cmd.none

    /// Applies default layout rules to the given bitmap.
    let private layoutImage
        (dpiScale : float) file (bitmap : Bitmap) inited =

            // get size of bitmap, adjusted for DPI scale
        let bitmapSize =
            bitmap.PixelSize.ToSize(dpiScale)

            // keep zoom scale and offset?
        let zoomScaleOpt =
            InitializedContainer.tryGetLockedZoomScale inited
        let offsetOpt =
            if zoomScaleOpt.IsSome then inited.OffsetOpt
            else None

            // layout image
        let offset, zoomScale =
            ImageLayout.getImageLayout
                inited.ContainerSize
                bitmapSize
                offsetOpt
                zoomScaleOpt

            // zoom scale lock succeeded?
        let zoomScaleLock = (Some zoomScale = zoomScaleOpt)

            // update offset/zoom
        let inited =
            {
                inited with
                    OffsetOpt = Some offset
                    ZoomScale = zoomScale
                    ZoomScaleLock = zoomScaleLock
            }

        {
            Initialized = inited
            File = file
            Bitmap = bitmap
            BitmapSize = bitmapSize
            PanOpt = None
        }

    /// Handles a loaded image.
    let private onImageLoaded dpiScale file bitmap model =

            // layout the image
        let model =
            model ^. ImageModel.Initialized_
                |> layoutImage dpiScale file bitmap
                |> Loaded

            // situate image for browsing
        let cmd =
            Cmd.ofEffect (fun dispatch ->
                async {
                    ImageFile.situate file
                        |> ImageSituated
                        |> dispatch
                } |> Async.Start)

        model, cmd

    /// Handles a load error.
    let private onHandleLoadError file message model =
        let model =
            let inited = model ^. ImageModel.Initialized_
            LoadError {
                Initialized = inited
                File = file
                Message = message
            }
        model, Cmd.none

    /// Zooms the current image.
    let private zoom
        zoomScale zoomScaleLock pointerPosOpt loaded model =

            // adjust offset
        let offset =
            ImageLayout.adjustImageOffset
                pointerPosOpt zoomScale loaded

            // update offset/zoom
        let loaded =
            loaded
                |> offset ^= LoadedImage.Offset_
                |> zoomScale ^= LoadedImage.ZoomScale_
                |> zoomScaleLock ^= LoadedImage.ZoomScaleLock_

            // update model
        let model =
            model
                |> loaded ^= ImageModel.Loaded_
        model, Cmd.none

    /// Updates zoom scale and origin.
    let private onWheelZoom sign pointerPos = function
        | Loaded_ loaded as model ->
            let zoomScale, zoomScaleLock =
                ImageLayout.incrementZoomScale sign loaded
            zoom zoomScale zoomScaleLock
                (Some pointerPos) loaded model
        | _ -> failwith "Invalid state"

    /// Zoom to actual size.
    let private onZoomToActualSize = function
        | Loaded_ loaded as model ->
            zoom 1.0 true None loaded model
        | _ -> failwith "Invalid state"

    /// Starts panning.
    let private onPanStart pointerPos = function
        | Loaded_ loaded as model ->
            let loaded =
                let pan =
                    {
                        ImageOffset = loaded.Offset
                        PointerPos = pointerPos
                    }
                { loaded with PanOpt = Some pan }
            let model =
                model
                    |> loaded ^= ImageModel.Loaded_
            model, Cmd.none
        | _ -> failwith "Invalid state"

    /// Moves the image during a pan.
    let private panImage pointerPos pan loaded =

            // track pointer
        let offset =
            pan.ImageOffset
                + (pointerPos - pan.PointerPos)

            // enforce layout rules
        let offset =
            ImageLayout.getImageOffset
                (loaded ^. LoadedImage.ContainerSize_)
                loaded.BitmapSize
                (Some offset)
                (loaded ^. LoadedImage.ZoomScale_)

        loaded
            |> offset ^= LoadedImage.Offset_

    /// Continues panning.
    let private onPanMove pointerPos = function
        | Loaded_ loaded as model ->
            match loaded.PanOpt with
                | Some pan ->
                    let loaded =
                        panImage pointerPos pan loaded
                    let model =
                        model
                            |> loaded ^= ImageModel.Loaded_
                    model, Cmd.none
                | None -> model, Cmd.none
        | _ -> failwith "Invalid state"

    /// Ends panning.
    let private onPanEnd = function
        | Loaded_ loaded as model ->
            let loaded =
                { loaded with PanOpt = None }
            let model =
                model
                    |> loaded ^= ImageModel.Loaded_
            model, Cmd.none
        | _ -> failwith "Invalid state"

    /// A loaded image has been situated in the current
    /// directory.
    let private onImageSituated
        previousFileOpt nextFileOpt model =
        let model =
            match model with
                | Loaded loaded ->
                    Browsed {
                        Loaded = loaded
                        PreviousFileOpt = previousFileOpt
                        NextFileOpt = nextFileOpt
                    }
                | _ -> failwith "Invalid state"
        model, Cmd.none

    /// Browses to another image in the current directory.
    let private onBrowse incr model =
        model, Cmd.none

    /// Deletes the current image.
    let private onDeleteFile model =
        model, Cmd.none

    (*
    /// Browses to and starts loading a file, if possible.
    let private browse inited incr fromFile =
        let model = ImageModel.browse inited incr fromFile
        let cmd =
            match model with
                | Browsed browsed ->
                    Cmd.OfAsync.perform
                        ImageFile.tryLoadImage
                        browsed.File
                        (function
                            | Ok bitmap ->
                                ImageLoaded (browsed.File, bitmap)
                            | Error msg -> HandleLoadError msg)
                | BrowseError _ -> Cmd.none
                | _ -> failwith "Invalid state"
        model, cmd

    /// Browses to a file, if possible.
    let private onBrowse incr model =
        let inited = model ^. ImageModel.Initialized_
        browse inited incr model.File

            match model with
                | Browsed browsed
                    when FileSystemInfo.same file model.File ->
                    layoutImage dpiScale file bitmap browsed
                | _ -> model   // ignore stale message
        model, Cmd.none

    /// Deletes the current image.
    let private onDeleteFile model =
        match model with
            | Browsed browsed ->

                try
                        // browse first
                    let model, cmd =
                        if browsed.HasNextImage then
                            onBrowse 1 model
                        elif browsed.HasPreviousImage then
                            onBrowse -1 model
                        else
                            BrowseError {   // to-do: handle better?
                                Initialized = browsed.Initialized
                                File = browsed.File
                                Message = "No file"
                            }, Cmd.none

                        // then delete file
                    browsed.File.Delete()

                    model, cmd

                with exn ->
                    LoadError {   // to-do: handle better?
                        Browsed = browsed
                        Message = exn.Message
                    }, Cmd.none

            | _ -> failwith "Invalid state"
            *)

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
            | ImageLoaded (file, bitmap) ->
                onImageLoaded dpiScale file bitmap model

                // unload current image
            | UnloadImage ->
                onUnloadImage model

                // handle load error
            | HandleLoadError (file, message) ->
                onHandleLoadError file message model

                // update zoom
            | WheelZoom (sign, pointerPos) ->
                onWheelZoom sign pointerPos model

                // zoom to actual size
            | ZoomToActualSize ->
                onZoomToActualSize model

                // start pan
            | PanStart pointerPos ->
                onPanStart pointerPos model

                // continue pan
            | PanMove pointerPos ->
                onPanMove pointerPos model

                // finish pan
            | PanEnd ->
                onPanEnd model

                // situate image in directory
            | ImageSituated (prevFileOpt, nextFileOpt) ->
                onImageSituated prevFileOpt nextFileOpt model

                // delete file
            | DeleteFile ->
                onDeleteFile model
