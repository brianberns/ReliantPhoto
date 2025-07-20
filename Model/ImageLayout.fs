namespace Reliant.Photo

open Avalonia
open Avalonia.Media.Imaging

open Aether.Operators

/// Functions relating to the location and size of an image
/// within its container.
module ImageLayout =

    /// Gets the size of the given bitmap when displayed at the
    /// given DPI and zoom scales.
    let getImageSize
        (dpiScale : float) (bitmap : Bitmap) (zoomScale : float) =
        bitmap.PixelSize.ToSize(dpiScale) * zoomScale

    /// Gets the default zoom scale for the given bitmap in the
    /// given container.
    let private getDefaultZoomScale
        (dpiScale : float)
        (containerSize : Size)
        (bitmap : Bitmap) =
        let ratio =
            containerSize / bitmap.PixelSize.ToSize(dpiScale)
        Array.min [| ratio.X; ratio.Y; 1.0 |]

    /// Computes image offset based on layout rules.
    let private getImageOffset
        dpiScale containerSize bitmap proposedOffsetOpt zoomScale =

            // compute (positive or negative) gap between image and container
        let marginSize =
            containerSize
                - getImageSize dpiScale bitmap zoomScale

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

    /// Gets image offset and zoom scale based on layout rules.
    let getImageLayout
        dpiScale containerSize bitmap proposedOffsetOpt zoomScaleOpt =

            // scale the image to fit in the container?
        let zoomScale =
            zoomScaleOpt
                |> Option.defaultWith (fun () ->
                    getDefaultZoomScale
                        dpiScale containerSize bitmap)

            // get image offset for that zoom scale
        let offset =
            getImageOffset
                dpiScale containerSize bitmap proposedOffsetOpt zoomScale

        offset, zoomScale

    /// Acceptable rounding error.
    let private epsilon = 0.001

    /// Zooms in or out one step.
    let incrementZoomScale dpiScale zoomSign loaded =
        assert(abs zoomSign = 1)

            // compute possible new zoom scale
        let zoomScale = loaded.ZoomScale
        let factor = 1.1
        let newScale, zoomScaleLock =

                // zoom in?
            if zoomSign >= 0 then
                let newScale = zoomScale * factor
                newScale, true

                // zoom out?
            else
                    // get minimum allowable zoom scale
                let zoomScaleFloor =
                    let containerSize =
                        loaded ^. LoadedImage.ContainerSize_
                    getDefaultZoomScale
                        dpiScale containerSize loaded.Bitmap

                    // zoom out
                let newScale = zoomScale / factor

                    // enforce floor
                let newScale, zoomScaleLock =
                    if zoomScaleFloor - newScale > epsilon then
                        zoomScale, false   // don't jump suddenly
                    else newScale, true

                newScale, zoomScaleLock

            // snap to 1.0?
        if newScale > 1.0 && zoomScale < 1.0
            || newScale < 1.0 && zoomScale > 1.0 then
            1.0, true
        else
            newScale, zoomScaleLock

    /// Updates image offset based on a new zoom scale.
    let updateImageOffset
        dpiScale (pointerPos : Point) newZoomScale loaded =

            // try to keep the point under the cursor stationary
        let newOffset =
            pointerPos
                - (pointerPos - loaded.Offset)
                    * (newZoomScale / loaded.ZoomScale)

        getImageLayout
            dpiScale
            (loaded ^. LoadedImage.ContainerSize_)
            loaded.Bitmap
            (Some newOffset)
            (Some newZoomScale)
            |> fst
