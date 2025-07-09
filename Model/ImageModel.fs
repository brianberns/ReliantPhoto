namespace Reliant.Photo

open System
open System.Collections.Generic
open System.IO

open Avalonia
open Avalonia.Media.Imaging

/// Image model.
type ImageModel =
    {
        /// Current or upcoming image file. This is set before
        /// the image itself is loaded.
        File : FileInfo

        /// Image is in the process of loading?
        IsLoading : bool

        /// Current underlying bitmap, or error message.
        Result : Result<Bitmap, string>

        /// User can browse to previous image?
        HasPreviousImage : bool

        /// User can browse to next image?
        HasNextImage : bool

        /// Displayed image size, if known. This may be different
        /// from the underlying bitmap size due to scaling.
        ImageSizeOpt : Option<Size>
       
        /// Zoom scale, if set.
        ZoomScaleOpt : Option<float>

        /// Point at which to center zoom.
        ZoomOrigin: RelativePoint
    }

module ImageModel =

    /// Compares files by name.
    let private compareFiles (fileA : FileInfo) (fileB : FileInfo) =
        assert(fileA.DirectoryName = fileB.DirectoryName)
        String.Compare(
            fileA.Name,
            fileB.Name,
            StringComparison.CurrentCultureIgnoreCase)

    /// Compares files by name.
    let private fileComparer =
        Comparer.Create(compareFiles)

    /// An uninitialized model.
    let private empty =
        {
            File = null
            IsLoading = false
            Result = Error ""
            HasPreviousImage = false
            HasNextImage = false
            ImageSizeOpt = None
            ZoomScaleOpt = None
            ZoomOrigin =
                RelativePoint(0.5, 0.5, RelativeUnit.Relative)   // image center
        }

    /// Browses to an image, if possible.
    let browse incr (fromFile : FileInfo) =

            // get all candidate files for browsing
        let files =
            fromFile.Directory.GetFiles()
                |> Seq.where (fun file ->
                    file.Attributes
                        &&& (FileAttributes.Hidden
                            ||| FileAttributes.System)
                        = FileAttributes.None)
                |> Seq.sortWith compareFiles
                |> Seq.toArray

            // find file we're browsing to, if possible
        let modelOpt =
            option {
                let! fromIdx =
                    let idx =
                        Array.BinarySearch(
                            files, fromFile, fileComparer)
                    if idx >= 0 then Some idx
                    else None
                let toIdx = fromIdx + incr
                if toIdx >= 0 && toIdx < files.Length then
                    return {
                        empty with
                            File = files[toIdx]
                            HasPreviousImage = toIdx > 0
                            HasNextImage = toIdx < files.Length - 1
                    }
            }

            // create default model if necessary
        modelOpt
            |> Option.defaultValue
                { empty with File = fromFile }

    /// Browses to the given file.
    let init file =
        browse 0 file

    /// Gets the scale of the given image size relative to the
    /// given underlying bitmap.
    let getImageScale (bitmap : Bitmap) imageSize =
        let zoomScale =
            imageSize / bitmap.Size
        assert(abs (zoomScale.X - zoomScale.Y) < 0.001)
        zoomScale.X

    /// Gets the given model's zoom scale, be it fixed or
    /// variable.
    let getZoomScale model =
        match model.ZoomScaleOpt, model.Result, model.ImageSizeOpt with

                // fixed zoom scale
            | Some zoomScale, _, _ -> Some zoomScale

                // variable zoom scale 
            | None, Ok bitmap, Some imageSize ->
                Some (getImageScale bitmap imageSize)

                // e.g. no zoom scale for invalid image
            | None, _, _ -> None
