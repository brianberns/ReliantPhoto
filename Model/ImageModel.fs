namespace Reliant.Photo

open System
open System.Collections.Generic
open System.IO

open Avalonia
open Avalonia.Media.Imaging

type BrowsedImage =
    {
        /// Image file.
        File : FileInfo

        /// Can browse to previous image?
        HasPreviousImage : bool

        /// Can browse to next image?
        HasNextImage : bool
    }

type LoadedImage =
    {
        /// Browsed image.
        Browsed : BrowsedImage

        /// Loaded bitmap.
        Bitmap : Bitmap
    }

    member this.File = this.Browsed.File
    member this.HasPreviousImage = this.Browsed.HasPreviousImage
    member this.HasNextImage = this.Browsed.HasNextImage

type DisplayedImage =
    {
        /// Loaded image.
        Loaded : LoadedImage

        /// Displayed image size. This may be different from
        /// the underlying bitmap size due to scaling.
        ImageSize : Size
    }

    member this.File = this.Loaded.File
    member this.HasPreviousImage = this.Loaded.HasPreviousImage
    member this.HasNextImage = this.Loaded.HasNextImage
    member this.Browsed = this.Loaded.Browsed
    member this.Bitmap = this.Loaded.Bitmap

module DisplayedImage =

    /// Gets the scale of displayed image size relative to the
    /// underlying bitmap.
    let getImageScale displayed =
        let vector =
            displayed.ImageSize / displayed.Bitmap.Size
        assert(abs (vector.X - vector.Y) < 0.001)
        vector.X

type ZoomedImage =
    {
        /// Displayed image.
        Displayed : DisplayedImage

        /// Fixed zoom scale.
        Scale : float

        /// Point at which to center zoom.
        Origin : RelativePoint
    }

    member this.File = this.Displayed.File
    member this.HasPreviousImage = this.Displayed.HasPreviousImage
    member this.HasNextImage = this.Displayed.HasNextImage
    member this.Browsed = this.Displayed.Browsed
    member this.Loaded = this.Displayed.Loaded
    member this.Bitmap = this.Loaded.Bitmap
    member this.ImageSize = this.Displayed.ImageSize

type ImageError =
    {
        /// Image file that couldn't be browsed/loaded.
        File : FileInfo

        /// Error message.
        Message : string
    }

type ImageModel =

    /// File has been browsed, but not yet loaded.
    | Browsed of BrowsedImage

    /// Bitmap has been loaded, but not yet displayed.
    | Loaded of LoadedImage

    /// Image has been displayed, but has variable scale.
    | Displayed of DisplayedImage

    /// Image has been zoomed to a specific scale.
    | Zoomed of ZoomedImage

    /// File could not be browsed/loaded.
    | Errored of ImageError

    member this.File =
        match this with
            | Browsed browsed -> browsed.File
            | Loaded loaded -> loaded.File
            | Displayed displayed -> displayed.File
            | Zoomed zoomed -> zoomed.File
            | Errored errored -> errored.File

    member this.HasPreviousImage =
        match this with
            | Browsed browsed -> browsed.HasPreviousImage
            | Loaded loaded -> loaded.HasPreviousImage
            | Displayed displayed -> displayed.HasPreviousImage
            | Zoomed zoomed -> zoomed.HasPreviousImage
            | Errored _ -> failwith "Invalid state"

    member this.HasNextImage =
        match this with
            | Browsed browsed -> browsed.HasNextImage
            | Loaded loaded -> loaded.HasNextImage
            | Displayed displayed -> displayed.HasNextImage
            | Zoomed zoomed -> zoomed.HasNextImage
            | Errored _ -> failwith "Invalid state"

    member this.BrowsedImage =
        match this with
            | Browsed browsed -> browsed
            | Loaded loaded -> loaded.Browsed
            | Displayed displayed -> displayed.Browsed
            | Zoomed zoomed -> zoomed.Browsed
            | Errored _ -> failwith "Invalid state"

    member this.LoadedImage =
        match this with
            | Loaded loaded -> loaded
            | Displayed displayed -> displayed.Loaded
            | Zoomed zoomed -> zoomed.Loaded
            | Browsed _
            | Errored _ -> failwith "Invalid state"

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

    /// Browses to a file, if possible.
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
                    return Browsed {
                        File = files[toIdx]
                        HasPreviousImage = toIdx > 0
                        HasNextImage = toIdx < files.Length - 1
                    }
            }

            // could not browse to file?
        modelOpt
            |> Option.defaultValue
                (Errored {
                    File = fromFile
                    Message = "Could not browse file"
                })

    /// Browses to the given file.
    let init file =
        browse 0 file
