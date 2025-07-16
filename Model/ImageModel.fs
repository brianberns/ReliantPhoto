namespace Reliant.Photo

open System
open System.Collections.Generic
open System.IO

open Avalonia
open Avalonia.Media.Imaging

open Aether
open Aether.Operators

/// Initialized container.
type InitializedContainer =
    {
        /// Container size.
        ContainerSize : Size
    }

/// A browsed image file.
type BrowsedImage =
    {
        /// Initialized container.
        Initialized : InitializedContainer

        /// Image file.
        File : FileInfo

        /// Can browse to previous image?
        HasPreviousImage : bool

        /// Can browse to next image?
        HasNextImage : bool
    }

    /// Initialized container lens.
    static member Initialized_ : Lens<_, _> =
        _.Initialized,
        fun inited browsed ->
            { browsed with Initialized = inited }

/// A loaded image.
type LoadedImage =
    {
        /// Browsed image.
        Browsed : BrowsedImage

        /// Loaded bitmap.
        Bitmap : Bitmap

        /// Zoom scale. This will be 1.0 for an image displayed
        /// at 1:1 size.
        ZoomScale : float

        /// Point at which zoom request originates, if any.
        ZoomOriginOpt : Option<Point>
    }

    /// Browsed image lens.
    static member Browsed_ : Lens<_, _> =
        _.Browsed,
        fun browsed loaded ->
            { loaded with Browsed = browsed }

    /// Initialized container lens.
    static member Initialized_ =
        LoadedImage.Browsed_ >-> BrowsedImage.Initialized_

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
        /// Browsed browsed file.
        Browsed : BrowsedImage

        /// Error message.
        Message : string
    }

    /// Browsed image lens.
    static member Browsed_ : Lens<_, _> =
        _.Browsed,
        fun browsed errored ->
            { errored with Browsed = browsed }

    /// Initialized container lens.
    static member Initialized_ : Lens<_, _> =
        LoadError.Browsed_>-> BrowsedImage.Initialized_

type ImageModel =

    /// Uninitialized.
    | Uninitialized

    /// Initialized container.
    | Initialized of InitializedContainer

    /// Browsed file.
    | Browsed of BrowsedImage

    /// Loaded image.
    | Loaded of LoadedImage

    /// File could not be browsed.
    | BrowseError of BrowseError

    /// Image could not be loaded.
    | LoadError of LoadError

    /// Initialized container prism.
    static member TryInitialized_ : Prism<_, _> =

        (function
            | Initialized inited -> Some inited
            | Browsed browsed ->
                Some (browsed ^. BrowsedImage.Initialized_)
            | Loaded loaded ->
                Some (loaded ^. LoadedImage.Initialized_)
            | LoadError errored ->
                Some (errored ^. LoadError.Initialized_)
            | Uninitialized
            | BrowseError _ -> None),

        (fun inited -> function
            | Initialized _ -> Initialized inited
            | Browsed browsed ->
                browsed
                    |> inited ^= BrowsedImage.Initialized_
                    |> Browsed
            | Loaded loaded ->
                loaded
                    |> inited ^= LoadedImage.Initialized_
                    |> Loaded
            | LoadError errored ->
                errored
                    |> inited ^= LoadError.Initialized_
                    |> LoadError
            | Uninitialized
            | BrowseError _ as model -> model)

    /// Initialized container lens.
    static member Initialized_ : Lens<_, _> =

        (fun model ->
            match model ^. ImageModel.TryInitialized_ with
                | Some inited -> inited
                | None -> failwith "Invalid state"),

        (fun inited model ->
            model
                |> inited ^= ImageModel.TryInitialized_)

    /// Browsed image prism.
    static member TryBrowsed_ : Prism<_, _> =

        (function
            | Browsed browsed -> Some browsed
            | Loaded loaded ->
                Some (loaded ^. LoadedImage.Browsed_)
            | LoadError errored ->
                Some (errored ^. LoadError.Browsed_)
            | Uninitialized
            | Initialized _
            | BrowseError _ -> None),

        (fun browsed -> function
            | Browsed _ -> Browsed browsed
            | Loaded loaded ->
                loaded
                    |> browsed ^= LoadedImage.Browsed_
                    |> Loaded
            | LoadError errored ->
                errored
                    |> browsed ^= LoadError.Browsed_
                    |> LoadError
            | Uninitialized
            | Initialized _
            | BrowseError _ as model -> model)

    /// Browsed image lens.
    static member Browsed_ : Lens<_, _> =

        (fun model ->
            match model ^. ImageModel.TryBrowsed_ with
                | Some browsed -> browsed
                | None -> failwith "Invalid state"),

        (fun browsed model ->
            model
                |> browsed ^= ImageModel.TryBrowsed_)

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
    let browse inited incr (fromFile : FileInfo) =

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
                        Initialized = inited
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

    /// Initial model.
    let init () = Uninitialized
