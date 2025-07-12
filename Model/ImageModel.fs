namespace Reliant.Photo

open System
open System.Collections.Generic
open System.IO

open Avalonia
open Avalonia.Media.Imaging

open Aether
open Aether.Operators

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

/// A browsed image inside a container.
type ContainedImage =
    {
        /// Browsed image file.
        Browsed : BrowsedImage

        /// Container size.
        ContainerSize : Size
    }

    static member Browsed_ : Lens<_, _> =
        _.Browsed,
        fun browsed contained ->
            { contained with Browsed = browsed }

    static member ContainerSize_ : Lens<_, _> =
        _.ContainerSize,
        fun containerSize contained ->
            { contained with ContainerSize = containerSize }

/// A bitmap loaded in a container but not yet displayed.
type LoadedImage =
    {
        /// Contained image.
        Contained : ContainedImage

        /// Loaded bitmap.
        Bitmap : Bitmap
    }

    static member Contained_ : Lens<_, _> =
        _.Contained,
        fun contained loaded ->
            { loaded with Contained = contained }

    static member Bitmap_ : Lens<_, _> =
        _.Bitmap,
        fun bitmap loaded ->
            { loaded with Bitmap = bitmap }

    static member Browsed_ =
        LoadedImage.Contained_ >-> ContainedImage.Browsed_

/// A displayed image.
type DisplayedImage =
    {
        /// Loaded image.
        Loaded : LoadedImage

        /// Displayed image size. This may be different from
        /// the underlying bitmap size due to scaling.
        ImageSize : Size

        /// Scale of default layout image size relative to the
        /// underlying bitmap. This is not affected by zooming.
        ImageScale : float
    }

    static member Loaded_ : Lens<_, _> =
        _.Loaded,
        fun loaded displayed ->
            { displayed with Loaded = loaded }

    static member Contained_ =
        DisplayedImage.Loaded_ >-> LoadedImage.Contained_

    static member Browsed_ =
        DisplayedImage.Contained_ >-> ContainedImage.Browsed_

/// An image with a fixed zoom scale and origin.
type ZoomedImage =
    {
        /// Displayed image.
        Displayed : DisplayedImage

        /// Fixed zoom scale. This will be 1.0 for an image
        /// displayed at 1:1 size.
        ZoomScale : float

        /// Point at which to center zoom.
        ZoomOrigin : RelativePoint
    }

    static member Displayed_ : Lens<_, _> =
        _.Displayed,
        fun displayed zoomed ->
            { zoomed with Displayed = displayed }

    static member Loaded_ =
        ZoomedImage.Displayed_ >-> DisplayedImage.Loaded_

    static member Contained_ =
        ZoomedImage.Loaded_ >-> LoadedImage.Contained_

    static member Browsed_ =
        ZoomedImage.Contained_ >-> ContainedImage.Browsed_

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

    static member Browsed_ : Lens<_, _> =
        _.Browsed,
        fun browsed contained ->
            { contained with Browsed = browsed }

type ImageModel =

    /// File has been browsed and is ready to be loaded.
    | Browsed of BrowsedImage

    /// Image has been added to a container.
    | Contained of ContainedImage

    /// Bitmap has been loaded and is ready to be added to a
    /// container.
    | Loaded of LoadedImage

    /// Image has been displayed and has variable zoom scale.
    | Displayed of DisplayedImage

    /// Image has been zoomed to a specific scale.
    | Zoomed of ZoomedImage

    /// File could not be browsed.
    | BrowseError of BrowseError

    /// Image could not be loaded.
    | LoadError of LoadError

    /// Browsed image file.
    static member Browsed_ : Lens<_, _> =
        (function
            | Browsed browsed -> browsed
            | Contained contained -> contained ^. ContainedImage.Browsed_
            | Loaded loaded -> loaded ^. LoadedImage.Browsed_
            | Displayed displayed -> displayed ^. DisplayedImage.Browsed_
            | Zoomed zoomed -> zoomed ^. ZoomedImage.Browsed_
            | LoadError errored -> errored ^. LoadError.Browsed_
            | BrowseError _ -> failwith "Invalid state"),
        (fun browsed -> function
            | Browsed _ -> Browsed browsed
            | Contained contained ->
                contained
                    |> browsed ^= ContainedImage.Browsed_
                    |> Contained
            | Loaded loaded ->
                loaded
                    |> browsed ^= LoadedImage.Browsed_
                    |> Loaded
            | Displayed displayed ->
                displayed
                    |> browsed ^= DisplayedImage.Browsed_
                    |> Displayed
            | Zoomed zoomed ->
                zoomed
                    |> browsed ^= ZoomedImage.Browsed_
                    |> Zoomed
            | LoadError error ->
                error
                    |> browsed ^= LoadError.Browsed_
                    |> LoadError
            | BrowseError _ ->
                failwith "Invalid state")

    /// Contained image.
    static member Contained_ : Lens<_, _> =
        (function
            | Contained contained -> contained
            | Loaded loaded -> loaded ^. LoadedImage.Contained_
            | Displayed displayed -> displayed ^. DisplayedImage.Contained_
            | Zoomed zoomed -> zoomed ^. ZoomedImage.Contained_
            | Browsed _
            | BrowseError _
            | LoadError _ -> failwith "Invalid state"),
        (fun contained -> function
            | Contained _ -> Contained contained
            | Loaded loaded ->
                loaded
                    |> contained ^= LoadedImage.Contained_
                    |> Loaded
            | Displayed displayed ->
                displayed
                    |> contained ^= DisplayedImage.Contained_
                    |> Displayed
            | Zoomed zoomed ->
                zoomed
                    |> contained ^= ZoomedImage.Contained_
                    |> Zoomed
            | Browsed _
            | BrowseError _
            | LoadError _ -> failwith "Invalid state")

    /// Loaded image.
    static member Loaded_ : Lens<_, _> =
        (function
            | Loaded loaded -> loaded
            | Displayed displayed -> displayed ^. DisplayedImage.Loaded_
            | Zoomed zoomed -> zoomed ^. ZoomedImage.Loaded_
            | Browsed _
            | Contained _
            | BrowseError _
            | LoadError _ -> failwith "Invalid state"),
        (fun loaded -> function
            | Loaded _ -> Loaded loaded
            | Displayed displayed ->
                displayed
                    |> loaded ^= DisplayedImage.Loaded_
                    |> Displayed
            | Zoomed zoomed ->
                zoomed
                    |> loaded ^= ZoomedImage.Loaded_
                    |> Zoomed
            | Browsed _
            | Contained _
            | BrowseError _
            | LoadError _ -> failwith "Invalid state")

    /// Displayed image.
    static member Displayed_ : Lens<_, _> =
        (function
            | Displayed displayed -> displayed
            | Zoomed zoomed -> zoomed ^. ZoomedImage.Displayed_
            | Browsed _
            | Contained _
            | Loaded _
            | BrowseError _
            | LoadError _ -> failwith "Invalid state"),
        (fun displayed -> function
            | Displayed _ -> Displayed displayed
            | Zoomed zoomed ->
                zoomed
                    |> displayed ^= ZoomedImage.Displayed_
                    |> Zoomed
            | Browsed _
            | Contained _
            | Loaded _
            | BrowseError _
            | LoadError _ -> failwith "Invalid state")

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
