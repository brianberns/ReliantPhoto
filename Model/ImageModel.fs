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

        /// Current loaded image, or error message. This will
        /// be the old image when starting to browse to a new one.
        Result : Result<Bitmap, string>

        /// User can browse to previous image?
        HasPreviousImage : bool

        /// User can browse to next image?
        HasNextImage : bool

        /// Displayed image size. This is different from the bitmap
        /// size due to scaling and zooming.
        ImageSize : Size

        /// User zoom scale. This is different from scaling done by
        /// Avalonia.
        ZoomScale : float

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
            ImageSize = Size.Infinity
            ZoomScale = 1.0
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
