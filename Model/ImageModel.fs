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

    /// Browsed image lens.
    static member Browsed_ : Lens<_, _> =
        _.Browsed,
        fun browsed contained ->
            { contained with Browsed = browsed }

/// A bitmap loaded in a container but not yet displayed.
type LoadedImage =
    {
        /// Contained image.
        Contained : ContainedImage

        /// Loaded bitmap.
        Bitmap : Bitmap
    }

    /// Contained image lens.
    static member Contained_ : Lens<_, _> =
        _.Contained,
        fun contained loaded ->
            { loaded with Contained = contained }

    /// Browsed image lens.
    static member Browsed_ =
        LoadedImage.Contained_ >-> ContainedImage.Browsed_

/// A displayed image.
type DisplayedImage =
    {
        /// Loaded image.
        Loaded : LoadedImage

        /// Displayed image size. This may be different from
        /// the underlying bitmap size due to scaling, but is
        /// not affected by zooming.
        ImageSize : Size

        /// Scale of default layout image size relative to the
        /// underlying bitmap. This is not affected by zooming.
        ImageScale : float
    }

    /// Loaded image lens.
    static member Loaded_ : Lens<_, _> =
        _.Loaded,
        fun loaded displayed ->
            { displayed with Loaded = loaded }

    /// Contained image lens.
    static member Contained_ =
        DisplayedImage.Loaded_ >-> LoadedImage.Contained_

    /// Browsed image lens.
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

        /// Point at which zoom originates.
        ZoomOrigin : RelativePoint
    }

    /// Displayed image lens.
    static member Displayed_ : Lens<_, _> =
        _.Displayed,
        fun displayed zoomed ->
            { zoomed with Displayed = displayed }

    /// Loaded image lens.
    static member Loaded_ =
        ZoomedImage.Displayed_ >-> DisplayedImage.Loaded_

    /// Contained image lens.
    static member Contained_ =
        ZoomedImage.Loaded_ >-> LoadedImage.Contained_

    /// Browsed image lens.
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
        /// Contained image file.
        Contained : ContainedImage

        /// Error message.
        Message : string
    }

    /// Contained image lens.
    static member Contained_ : Lens<_, _> =
        _.Contained,
        fun contained errored ->
            { errored with Contained = contained }

    /// Browsed image lens.
    static member Browsed_ : Lens<_, _> =
        LoadError.Contained_ >-> ContainedImage.Browsed_

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

    /// Browsed image prism.
    static member TryBrowsed_ : Prism<_, _> =

        (function
            | Browsed browsed -> Some browsed
            | Contained contained ->
                Some (contained ^. ContainedImage.Browsed_)
            | Loaded loaded ->
                Some (loaded ^. LoadedImage.Browsed_)
            | Displayed displayed ->
                Some (displayed ^. DisplayedImage.Browsed_)
            | Zoomed zoomed ->
                Some (zoomed ^. ZoomedImage.Browsed_)
            | LoadError errored ->
                Some (errored ^. LoadError.Browsed_)
            | BrowseError _ -> None),

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
            | LoadError errored ->
                errored
                    |> browsed ^= LoadError.Browsed_
                    |> LoadError
            | BrowseError _ as model -> model)

    /// Browsed image lens.
    static member Browsed_ : Lens<_, _> =

        (fun model ->
            match model ^. ImageModel.TryBrowsed_ with
                | Some browsed -> browsed
                | None -> failwith "Invalid state"),

        (fun browsed model ->
            assert(
                model ^. ImageModel.TryBrowsed_
                    |> Option.isSome)
            browsed ^= ImageModel.TryBrowsed_
                <| model)

    /// Contained image prism.
    static member TryContained_ : Prism<_, _> =

        (function
            | Contained contained -> Some contained
            | Loaded loaded ->
                Some (loaded ^. LoadedImage.Contained_)
            | Displayed displayed ->
                Some (displayed ^. DisplayedImage.Contained_)
            | Zoomed zoomed ->
                Some (zoomed ^. ZoomedImage.Contained_)
            | LoadError errored ->
                Some (errored ^. LoadError.Contained_)
            | Browsed _
            | BrowseError _ -> None),

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
            | LoadError _ as model -> model)

    /// Contained image lens.
    static member Contained_ : Lens<_, _> =

        (fun model ->
            match model ^. ImageModel.TryContained_ with
                | Some contained -> contained
                | None -> failwith "Invalid state"),

        (fun contained model ->
            assert(
                model ^. ImageModel.TryContained_
                    |> Option.isSome)
            contained ^= ImageModel.TryContained_
                <| model)

    /// Loaded image prism.
    static member TryLoaded_ : Prism<_, _> =

        (function
            | Loaded loaded -> Some loaded
            | Displayed displayed ->
                Some (displayed ^. DisplayedImage.Loaded_)
            | Zoomed zoomed ->
                Some (zoomed ^. ZoomedImage.Loaded_)
            | Browsed _
            | Contained _
            | BrowseError _
            | LoadError _ -> None),

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
            | LoadError _ as model -> model)

    /// Loaded image lens.
    static member Loaded_ : Lens<_, _> =

        (fun model ->
            match model ^. ImageModel.TryLoaded_ with
                | Some loaded -> loaded
                | None -> failwith "Invalid state"),

        (fun loaded model ->
            assert(
                model ^. ImageModel.TryLoaded_
                    |> Option.isSome)
            loaded ^= ImageModel.TryLoaded_
                <| model)

    /// Displayed image prism.
    static member TryDisplayed_ : Prism<_, _> =

        (function
            | Displayed displayed -> Some displayed
            | Zoomed zoomed ->
                Some (zoomed ^. ZoomedImage.Displayed_)
            | Browsed _
            | Contained _
            | Loaded _
            | BrowseError _
            | LoadError _ -> None),

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
            | LoadError _ as model -> model)

    /// Displayed image lens.
    static member Displayed_ : Lens<_, _> =

        (fun model ->
            match model ^. ImageModel.TryDisplayed_ with
                | Some displayed -> displayed
                | None -> failwith "Invalid state"),

        (fun displayed model ->
            assert(
                model ^. ImageModel.TryDisplayed_
                    |> Option.isSome)
            displayed ^= ImageModel.TryDisplayed_
                <| model)

    /// Image file.
    member this.File =
        match this with
            | BrowseError errored -> errored.File
            | _ -> (this ^. ImageModel.Browsed_).File

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
