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

    /// Image has been loaded.
    | ImageLoaded of FileInfo * Bitmap

    /// Unload current image.
    | UnloadImage

    /// Load error occurred.
    | HandleLoadError of FileInfo * string (*error message*)

    /// Pointer wheel position has changed.
    | WheelZoom of int (*sign*) * Point (*pointer position*)

    /// Zoom image to given size. (Full size is 1.0.)
    | ZoomTo of Zoom

    /// Pointer pan has started.
    | PanStart of Point

    /// Pointer pan has moved.
    | PanMove of Point

    /// Pointer pan has ended.
    | PanEnd

    /// File has been situated in its directory.
    | Situated
        of Option<FileImageResult> (*previous image*)
            * Option<FileImageResult> (*next image*)

    /// Delete the current file.
    | DeleteFile

module ImageMessage =

    /// Converts the given image result to a message.
    let ofResult file (result : ImageResult) =
        match result with
            | Ok bitmap -> ImageLoaded (file, bitmap)
            | Error msg -> HandleLoadError (file, msg)

    /// Creates a command to load an image from the given file.
    let loadImageCommand file =
        Cmd.OfAsync.perform
            ImageFile.tryLoadImage
            file
            (ofResult file)

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
                | Loaded loaded ->
                    Loaded (resize containerSize loaded)

                    // resize: just update container size
                | _ ->
                    model
                        |> containerSize ^= ImageModel.ContainerSize_
        model, Cmd.none

    /// Starts loading an image from the given file.
    let private onLoadImage file (model : ImageModel) =
        let cmd = loadImageCommand file
        model, cmd

    /// Unloads the current image, if any.
    let private onUnloadImage model =
        let model =
            match model with
                | Initialized_ inited ->
                    inited.ContainerSize   // keep only the container size
                        |> InitializedContainer.create
                        |> Initialized
                | _ -> model
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
                    Zoom =
                        Zoom.create zoomScale zoomScaleLock
            }

        {
            Situated = SituatedFile.create file inited
            Bitmap = bitmap
            BitmapSize = bitmapSize
            SavedZoomOpt = None
            PanOpt = None
        }

    /// Tries to load an image from the given file.
    let private tryLoadImage fileOpt =
        async {
            match fileOpt with
                | Some file ->
                    let! result = ImageFile.tryLoadImage file
                    return Some (file, result)
                | None -> return None
        }

    /// Situates a file for browsing.
    let private situate file =
        Cmd.ofEffect (fun dispatch ->
            async {
                let prevFileOpt, nextFileOpt
                    = ImageFile.situate file
                let! prevResultOpt = tryLoadImage prevFileOpt
                let! nextResultOpt = tryLoadImage nextFileOpt
                (prevResultOpt, nextResultOpt)
                    |> Situated
                    |> dispatch
            } |> Async.Start)

    /// Handles a loaded image.
    let private onImageLoaded dpiScale file bitmap model =
        let model =
            model ^. ImageModel.Initialized_
                |> layoutImage dpiScale file bitmap
                |> Loaded
        model, situate file

    /// Handles a load error.
    let private onHandleLoadError file message model =
        let model =
            let situated =
                model ^. ImageModel.Initialized_
                    |> SituatedFile.create file
            LoadError {
                Situated = situated
                Message = message
            }
        model, situate file

    /// Zooms the current image.
    let private zoomTo zoom pointerPos loaded =

            // adjust offset
        let offset =
            ImageLayout.adjustImageOffset
                pointerPos zoom.Scale loaded

            // update offset/zoom
        loaded
            |> offset ^= LoadedImage.Offset_
            |> zoom ^= LoadedImage.Zoom_

    /// Zooms in or out one step.
    let private onWheelZoom sign pointerPos = function
        | Loaded loaded ->
            let zoom =
                ImageLayout.incrementZoomScale sign loaded
            let loaded = zoomTo zoom pointerPos loaded
            Loaded loaded, Cmd.none
        | _ -> failwith "Invalid state"

    /// Zoom to given size.
    let private onZoomTo zoom = function
        | Loaded loaded ->

                // save current zoom scale?
            let loaded =
                let curZoom =
                    loaded ^. LoadedImage.Zoom_
                let savedZoomOpt =
                    if curZoom.Scale = 1.0 then None
                    else Some curZoom
                { loaded with
                    SavedZoomOpt = savedZoomOpt }

                // find container center
            let pointerPos =
                let size =
                    (loaded ^. LoadedImage.ContainerSize_) / 2.0
                Point(size.Width, size.Height)

                // zoom to given size
            let loaded = zoomTo zoom pointerPos loaded

            Loaded loaded, Cmd.none

        | _ -> failwith "Invalid state"

    /// Starts panning.
    let private onPanStart pointerPos = function
        | Loaded loaded ->
            let model =
                let pan =
                    {
                        ImageOffset = loaded.Offset
                        PointerPos = pointerPos
                    }
                Loaded { loaded with PanOpt = Some pan }
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
        | Loaded loaded as model ->
            match loaded.PanOpt with
                | Some pan ->
                    Loaded (panImage pointerPos pan loaded),
                    Cmd.none
                | None -> model, Cmd.none
        | _ -> failwith "Invalid state"

    /// Ends panning.
    let private onPanEnd = function
        | Loaded loaded ->
            Loaded { loaded with PanOpt = None },
            Cmd.none
        | _ -> failwith "Invalid state"

    /// A file has been situated in its directory.
    let private onSituated
        previousResultOpt nextResultOpt model =
        let model =
            let situated =
                SituatedFile.update
                    previousResultOpt
                    nextResultOpt
                    (model ^. ImageModel.Situated_)
            model
                |> situated ^= ImageModel.Situated_
        model, Cmd.none

    /// Deletes the current image.
    let private onDeleteFile model =
        let situated = model ^. ImageModel.Situated_

        try
                // delete file
            situated.File.Delete()

                // browse to another image
            let message =
                match situated.PreviousResultOpt, situated.NextResultOpt with
                    | _, Some (file, result)
                    | Some (file, result), _ ->
                        ofResult file result
                    | None, None -> failwith "No file"   // to-do: handle better?
            model, Cmd.ofMsg message

        with exn ->
            LoadError {
                Situated = situated
                Message = exn.Message
            }, Cmd.none

    /// Updates the given model based on the given message.
    let update dpiScale message model =
        match message with

                // set/update container size
            | ContainerSized containerSize ->
                 onContainerSized containerSize model

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

                // zoom to given size
            | ZoomTo zoom ->
                onZoomTo zoom model

                // start pan
            | PanStart pointerPos ->
                onPanStart pointerPos model

                // continue pan
            | PanMove pointerPos ->
                onPanMove pointerPos model

                // finish pan
            | PanEnd ->
                onPanEnd model

                // situate file in directory
            | Situated (prevResultOpt, nextResultOpt) ->
                onSituated prevResultOpt nextResultOpt model

                // delete file
            | DeleteFile ->
                onDeleteFile model
