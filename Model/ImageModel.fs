namespace Reliant.Photo

open System
open System.Collections.Generic
open System.IO

open Avalonia.Media.Imaging

/// Image model.
type ImageModel =
    {
        /// Current or upcoming image file. This is set before
        /// the image itself is loaded.
        File : FileInfo

        /// Current loaded image, if any. This will be the old
        /// image when starting to browse to a new one.
        Result : Result<Bitmap, string>

        /// User can browse to previous image?
        HasPreviousImage : bool

        /// User can browse to next image?
        HasNextImage : bool
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

    /// Browses to an image in the current directory, if
    /// possible.
    let browseImage incr model =

            // get all candidate files for browsing
        let files =
            model.File.Directory.GetFiles()
                |> Seq.where (fun file ->
                    file.Attributes.HasFlag(FileAttributes.Hidden)
                        |> not)
                |> Seq.sortWith compareFiles
                |> Seq.toArray

            // find index of file we're browsing to, if possible
        let toIdxOpt =
            option {
                let! fromIdx =
                    let idx =
                        Array.BinarySearch(
                            files, model.File, fileComparer)
                    if idx >= 0 then Some idx
                    else None
                let toIdx = fromIdx + incr
                if toIdx >= 0 && toIdx < files.Length then
                    return toIdx
            }

            // update model accordingly
        match toIdxOpt with
            | Some toIdx ->
                { model with
                    File = files[toIdx]
                    HasPreviousImage = toIdx > 0
                    HasNextImage = toIdx < files.Length - 1 }
            | None -> model

    /// Browses to the given file.
    let init file =
        browseImage 0 {
            File = file
            Result = Error ""
            HasPreviousImage = false
            HasNextImage = false
        }
