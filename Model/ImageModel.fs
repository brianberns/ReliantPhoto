namespace Reliant.Photo

open System
open System.Collections.Generic
open System.IO

open Avalonia
open Avalonia.Media.Imaging

/// A browsed image file.
type BrowsedImage =
    {
        /// Image file.
        File : FileInfo

        /// Can browse to previous image?
        HasPreviousImage : bool

        /// Can browse to next image?
        HasNextImage : bool
    }

/// A bitmap loaded from an image file.
type LoadedImage =
    {
        /// Browsed image file.
        Browsed : BrowsedImage

        /// Loaded bitmap.
        Bitmap : Bitmap
    }

/// A displayed image from a loaded image file.
type DisplayedImage =
    {
        /// Loaded image.
        Loaded : LoadedImage

        /// Displayed image size. This may be different from
        /// the underlying bitmap size due to scaling.
        ImageSize : Size
    }

    member this.Browsed = this.Loaded.Browsed

module DisplayedImage =

    /// Gets the scale of displayed image size relative to the
    /// underlying bitmap.
    let getImageScale displayed =
        let vector =
            displayed.ImageSize / displayed.Loaded.Bitmap.Size
        assert(abs (vector.X - vector.Y) < 0.001)
        vector.X

/// An image with a fixed zoom scale and origin.
type ZoomedImage =
    {
        /// Displayed image.
        Displayed : DisplayedImage

        /// Fixed zoom scale.
        Scale : float

        /// Point at which to center zoom.
        Origin : RelativePoint
    }

    member this.Browsed = this.Displayed.Browsed
    member this.Loaded = this.Displayed.Loaded

/// An image file that could not be browsed.
type BrowseError =
    {
        /// Image file that couldn't be browsed.
        File : FileInfo

        /// Error message.
        Message : string
    }

/// An image file that could not be loaded.
type LoadError =
    {
        /// Browsed image file.
        Browsed : BrowsedImage

        /// Error message.
        Message : string
    }

    member this.File = this.Browsed.File
    member this.HasPreviousImage = this.Browsed.HasPreviousImage
    member this.HasNextImage = this.Browsed.HasNextImage

type ImageModel =

    /// File has been browsed and is ready to be loaded.
    | Browsed of BrowsedImage

    /// Bitmap has been loaded and is ready to be displayed.
    | Loaded of LoadedImage

    /// Image has been displayed and has variable zoom scale.
    | Displayed of DisplayedImage

    /// Image has been zoomed to a specific scale.
    | Zoomed of ZoomedImage

    /// File could not be browsed.
    | BrowseError of BrowseError

    /// Image could not be loaded.
    | LoadError of LoadError

    member this.BrowsedImage =
        match this with
            | Browsed browsed -> browsed
            | Loaded loaded -> loaded.Browsed
            | Displayed displayed -> displayed.Browsed
            | Zoomed zoomed -> zoomed.Browsed
            | LoadError errored -> errored.Browsed
            | BrowseError _ -> failwith "Invalid state"

    member this.LoadedImage =
        match this with
            | Loaded loaded -> loaded
            | Displayed displayed -> displayed.Loaded
            | Zoomed zoomed -> zoomed.Loaded
            | Browsed _
            | BrowseError _
            | LoadError _ -> failwith "Invalid state"

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
                (BrowseError {
                    File = fromFile
                    Message = "Could not browse file"
                })

    /// Browses to the given file.
    let init file =
        browse 0 file
