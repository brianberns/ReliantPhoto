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

    /// Zoom image to actual size (1:1).
    | ZoomToActualSize of Option<Point> (*pointer position*)

    /// Zooms image to fit container.
    | ZoomToFit

    /// Pointer pan has started.
    | PanStart of Point

    /// Pointer pan has moved.
    | PanMove of Point

    /// Pointer pan has ended.
    | PanEnd

    /// File has been situated in its directory.
    | Situated of Situation

    /// Turn full-screen mode on/off.
    | FullScreen of bool

    /// Delete the current file.
    | DeleteFile

    /// DPI scale has changed.
    | DpiChanged of double

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

    /// Updates layout due to container resize.
    let private resize containerSize loaded =

            // keep zoom scale?
        let zoomScaleOpt =
            loaded
                ^. LoadedImage.Sized_
                |> SizedContainer.tryGetLockedZoomScale

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
                | Initialized initial ->
                    Sized (
                        SizedContainer.create
                            initial containerSize)

                    // resize: update container size and layout
                | Loaded loaded ->
                    Loaded (resize containerSize loaded)

                    // resize: just update container size
                | _ ->
                    model
                        |> containerSize ^= ImageModel.ContainerSize_
        model, Cmd.none

    /// Unloads the current image, if any.
    let private unloadImage toModel model =
        let model =
            match model with
                | Sized_ sized ->
                    sized.ContainerSize   // keep only the container size
                        |> SizedContainer.create sized.Initial
                        |> toModel
                | _ -> model
        model, Cmd.none

    /// Unloads the current image, if any.
    let private onUnloadImage model =
        unloadImage Sized model

    /// Computes size of the given bitmap, adjusted for DPI.
    let private getBitmapSize
        (dpiScale : float) (bitmap : Bitmap) =
        bitmap.PixelSize.ToSize(dpiScale)

    /// Applies default layout rules to the given bitmap.
    let private layoutImage file (bitmap : Bitmap) sized =

            // get bitmap size
        let bitmapSize =
            let dpiScale = sized ^. SizedContainer.DpiScale_
            getBitmapSize dpiScale bitmap

            // keep zoom scale and offset?
        let zoomScaleOpt =
            SizedContainer.tryGetLockedZoomScale sized
        let offsetOpt =
            if zoomScaleOpt.IsSome then sized.OffsetOpt
            else None

            // layout image
        let offset, zoomScale =
            ImageLayout.getImageLayout
                sized.ContainerSize
                bitmapSize
                offsetOpt
                zoomScaleOpt

            // zoom scale lock succeeded?
        let zoomScaleLock = (Some zoomScale = zoomScaleOpt)

            // update offset/zoom
        let sized =
            {
                sized with
                    OffsetOpt = Some offset
                    Zoom =
                        Zoom.create zoomScale zoomScaleLock
            }

        {
            Situated = SituatedFile.create file sized
            Bitmap = bitmap
            BitmapSize = bitmapSize
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
                let detail = ImageFile.situate file
                let! prevResultOpt =
                    tryLoadImage detail.PreviousFileOpt
                let! nextResultOpt =
                    tryLoadImage detail.NextFileOpt
                Situation.create
                    detail.FileLengthOpt
                    detail.ExifMetadataOpt
                    prevResultOpt
                    nextResultOpt
                        |> Situated
                        |> dispatch
            } |> Async.Start)

    /// Handles a loaded image.
    let private onImageLoaded file bitmap model =
        let model =
            model ^. ImageModel.Sized_
                |> layoutImage file bitmap
                |> Loaded
        model, situate file

    /// Handles a load error.
    let private onHandleLoadError file message model =
        let model =
            let situated =
                model ^. ImageModel.Sized_
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

                // zoom in/out
            let zoom =
                ImageLayout.incrementZoomScale sign loaded
            let loaded = zoomTo zoom pointerPos loaded

            Loaded loaded, Cmd.none

        | _ -> failwith "Invalid state"

    /// Zoom to actual size.
    let private onZoomToActualSize pointerPosOpt = function
        | Loaded loaded ->

                // find container center?
            let pointerPos =
                pointerPosOpt
                    |> Option.defaultWith (fun () ->
                        let size =
                            (loaded ^. LoadedImage.ContainerSize_) / 2.0
                        Point(size.Width, size.Height))

                // zoom to actual size
            let loaded =
                zoomTo Zoom.actualSize pointerPos loaded

            Loaded loaded, Cmd.none

        | _ -> failwith "Invalid state"

    /// Zoom to fit container.
    let private onZoomToFit = function
        | Loaded loaded ->

                // get default layout
            let offset, zoomScale =
                let containerSize =
                    loaded ^. LoadedImage.ContainerSize_
                ImageLayout.getImageLayout
                    containerSize
                    loaded.BitmapSize
                    None None
            let zoom = Zoom.create zoomScale false

                    // update offset/zoom
            let loaded =
                loaded
                    |> offset ^= LoadedImage.Offset_
                    |> zoom ^= LoadedImage.Zoom_

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
    let private onSituated situation model =
        let model =
            let situated =
                { (model ^. ImageModel.Situated_) with
                    SituationOpt = Some situation }
            model
                |> situated ^= ImageModel.Situated_
        model, Cmd.none

    /// Turns full-screen mode on or off.
    let private onFullScreen flag model =
        let model =
            let sized =
                { (model ^. ImageModel.Sized_) with
                    FullScreen = flag }
            model
                |> sized ^= ImageModel.Sized_
        model, Cmd.none

    /// Empties the current image.
    let private emptyImage directory model =
        model
            |> unloadImage (fun sized ->
                Empty {
                    Sized = sized
                    Directory = directory })

    /// Deletes the current image's file.
    let private onDeleteFile model =
        let situated = model ^. ImageModel.Situated_
        match situated.SituationOpt with
            | Some situation ->

                try
                        // delete file
                    situated.File.Delete()

                        // browse to another image?
                    match situation.PreviousResultOpt,
                        situation.NextResultOpt with
                        | _, Some (file, result)
                        | Some (file, result), _ ->
                            let msg = ofResult file result
                            model, Cmd.ofMsg msg
                        | None, None ->   // no files left in directory
                            emptyImage situated.File.Directory model

                with exn ->
                    LoadError {
                        Situated = situated
                        Message = exn.Message
                    }, Cmd.none

            | None -> failwith "Invalid state"

    /// Updates DPI scale.
    let private onDpiChanged dpiScale model =

            // update DPI scale
        let model =
            model
                |> dpiScale ^= ImageModel.DpiScale_

            // update bitmap size?
        let model =
            match model with
                | Loaded loaded ->
                    let bitmapSize =
                        getBitmapSize dpiScale loaded.Bitmap
                    Loaded {
                        loaded with
                            BitmapSize = bitmapSize }
                | _ -> model

        model, Cmd.none

    /// Updates the given model based on the given message.
    let update message model =
        match message with

                // set/update container size
            | ContainerSized containerSize ->
                 onContainerSized containerSize model

                // finish loading an image
            | ImageLoaded (file, bitmap) ->
                onImageLoaded file bitmap model

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
            | ZoomToActualSize pointerPosOpt ->
                onZoomToActualSize pointerPosOpt model

                // zoom to fit container
            | ZoomToFit ->
                onZoomToFit model

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
            | Situated situation ->
                onSituated situation model

                // full-screen mode
            | FullScreen flag ->
                onFullScreen flag model

                // delete file
            | DeleteFile ->
                onDeleteFile model

                // update DPI scale
            | DpiChanged dpiScale ->
                onDpiChanged dpiScale model
