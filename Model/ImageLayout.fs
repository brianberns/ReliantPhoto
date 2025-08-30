namespace Reliant.Photo

open Avalonia
open Aether.Operators

/// Functions relating to the location and size of an image
/// within its container.
module ImageLayout =

    /// Gets the size of the given bitmap when displayed at the
    /// system DPI and given zoom scale.
    let getImageSize (bitmapSize : Size) (zoomScale : float) =
        bitmapSize * zoomScale   // assume bitmap size already accounts for system DPI

    /// Computes image offset based on layout rules.
    let getImageOffset
        containerSize bitmapSize proposedOffsetOpt zoomScale =

            // compute (positive or negative) gap between image and container
        let marginSize =
            containerSize - getImageSize bitmapSize zoomScale

        match proposedOffsetOpt with

                // positive margin: center image in that dimension
                // negative margin: clamp image edges to container edges, if necessary
            | Some (offset : Point) ->
                let offsetX =
                    if marginSize.Width > 0.0 then
                        marginSize.Width / 2.0
                    else
                        max marginSize.Width (min 0.0 offset.X)
                let offsetY =
                    if marginSize.Height > 0.0 then
                        marginSize.Height / 2.0
                    else
                        max marginSize.Height (min 0.0 offset.Y)
                Point(offsetX, offsetY)

                // center image by default
            | None ->
                Point(marginSize.Width, marginSize.Height) / 2.0

    /// Gets the default zoom scale for the given bitmap in the
    /// given container.
    let private getDefaultZoomScale
        (containerSize : Size) (bitmapSize : Size) =
        let ratio = containerSize / bitmapSize
        min ratio.X ratio.Y |> min 1.0

    /// Gets an acceptable zoom scale for the given bitmap in the
    /// given container.
    let private getZoomScale
        containerSize bitmapSize proposedZoomScaleOpt =

            // get zoom scale limits
        let zoomScaleFloor =
            getDefaultZoomScale containerSize bitmapSize
        let zoomScaleCeiling = 8.0

            // enforce limits
        match proposedZoomScaleOpt with
            | Some proposedZoomScale ->
                proposedZoomScale
                    |> max zoomScaleFloor
                    |> min zoomScaleCeiling
            | _ -> zoomScaleFloor   // zoom all the way out by default

    /// Computes image offset and zoom scale based on layout rules.
    let getImageLayout
        containerSize bitmapSize proposedOffsetOpt proposedZoomScaleOpt =

            // get an acceptable zoom scale
        let zoomScale =
            getZoomScale
                containerSize bitmapSize proposedZoomScaleOpt

            // get image offset for that zoom scale
        let offset =
            getImageOffset
                containerSize bitmapSize proposedOffsetOpt zoomScale

        offset, zoomScale

    /// Zooms in or out one step.
    let incrementZoomScale zoomSign loaded =
        assert(abs zoomSign = 1)

            // compute possible new zoom scale
        let zoomScale = loaded ^. LoadedImage.ZoomScale_
        let factor = 1.1
        let newScale, zoomScaleLock =

                // proposed zoom
            let proposedScale =
                if zoomSign >= 0 then zoomScale * factor   // zoom in: enlarge image
                else zoomScale / factor                    // zoom out: shrink image

                // apply layout rules to proposed zoom scale
            let newScale =
                let containerSize =
                    loaded ^. LoadedImage.ContainerSize_
                getZoomScale
                    containerSize loaded.BitmapSize
                    (Some proposedScale)

                // unlock zoom when floor reached
            let zoomScaleLock =
                if zoomSign >= 0 then true
                else
                    assert(newScale >= proposedScale)
                    newScale = proposedScale

            newScale, zoomScaleLock

            // snap to 1.0 instead?
        if newScale > 1.0 && zoomScale < 1.0
            || newScale < 1.0 && zoomScale > 1.0 then
            Zoom.actualSize
        else
            Zoom.create newScale zoomScaleLock

    /// Adjusts image offset based on a new zoom scale.
    let adjustImageOffset
        (pointerPosOpt : Option<Point>) newZoomScale loaded =

            // try to keep the point under the cursor stationary
        let newOffsetOpt =
            pointerPosOpt
                |> Option.map (fun pointerPos ->
                    let zoomScale =
                        loaded ^. LoadedImage.ZoomScale_
                    pointerPos
                        - (pointerPos - loaded.Offset)
                            * (newZoomScale / zoomScale))

        getImageOffset
            (loaded ^. LoadedImage.ContainerSize_)
            loaded.BitmapSize
            newOffsetOpt
            newZoomScale
