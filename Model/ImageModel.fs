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

        /// Image location offset, if any.
        OffsetOpt : Option<Point>

        /// Zoom scale. This will be 1.0 for an image displayed
        /// at 1:1 size.
        ZoomScale : float

        /// Lock zoom scale when resizing?
        ZoomScaleLock : bool
    }

    /// Container size lens.
    static member ContainerSize_ : Lens<_, _> =
        _.ContainerSize,
        fun containerSize inited ->
            { inited with ContainerSize = containerSize }

    /// Offset prism.
    static member Offset_ : Prism<_, _> =
        _.OffsetOpt,
        fun offset inited ->
            { inited with OffsetOpt = Some offset }

    /// Zoom scale lens.
    static member ZoomScale_ : Lens<_, _> =
        _.ZoomScale,
        fun zoomScale inited ->
            { inited with ZoomScale = zoomScale }

    /// Zoom scale lock lens.
    static member ZoomScaleLock_ : Lens<_, _> =
        _.ZoomScaleLock,
        fun zoomScaleLock inited ->
            { inited with ZoomScaleLock = zoomScaleLock }

module InitializedContainer =

    /// Creates an initialized container.
    let create containerSize =
        {
            ContainerSize = containerSize
            OffsetOpt = None
            ZoomScale = 1.0
            ZoomScaleLock = false
        }

    /// Get locked zoom scale, if any.
    let tryGetLockedZoomScale inited =
        if inited.ZoomScaleLock then
            Some inited.ZoomScale
        else None

/// Image pan.
type Pan =
    {
        /// Image offset.
        ImageOffset : Point

        /// Previous pointer position.
        PointerPos : Point
    }

/// A loaded image.
type LoadedImage =
    {
        /// Initialized container.
        Initialized : InitializedContainer

        /// Image file.
        File : FileInfo

        /// Loaded bitmap.
        Bitmap : Bitmap

        /// Bitmap size, adjusted for system DPI scale.
        BitmapSize : Size

        /// Pan location, when panning.
        PanOpt : Option<Pan>
    }

    /// Initialized container lens.
    static member Initialized_ : Lens<_, _> =
        _.Initialized,
        fun inited loaded ->
            { loaded with Initialized = inited }

    /// Container size lens.
    static member ContainerSize_ =
        LoadedImage.Initialized_
            >-> InitializedContainer.ContainerSize_

    /// Zoom scale lens.
    static member ZoomScale_ =
        LoadedImage.Initialized_
            >-> InitializedContainer.ZoomScale_

    /// Zoom scale lock lens.
    static member ZoomScaleLock_ =
        LoadedImage.Initialized_
            >-> InitializedContainer.ZoomScaleLock_

    /// Offset prism.
    static member Offset_ =
        LoadedImage.Initialized_
            >-> InitializedContainer.Offset_

    /// Loaded image offset.
    member this.Offset =
        match this ^. LoadedImage.Offset_ with
            | Some offset -> offset
            | None -> failwith "Invalid state"

/// A browsed image.
type BrowsedImage =
    {
        /// Loaded image.
        Loaded : LoadedImage

        /// Can browse to previous image?
        HasPreviousImage : bool

        /// Can browse to next image?
        HasNextImage : bool
    }

    /// Loaded image lens.
    static member Loaded_ : Lens<_, _> =
        _.Loaded,
        fun loaded browsed ->
            { browsed with Loaded = loaded }

    /// Container size lens.
    static member Initialized_ =
        BrowsedImage.Loaded_
            >-> LoadedImage.Initialized_

    /// Container size lens.
    static member ContainerSize_ =
        BrowsedImage.Loaded_
            >-> LoadedImage.ContainerSize_

    /// Zoom scale lens.
    static member ZoomScale_ =
        BrowsedImage.Loaded_
            >-> LoadedImage.ZoomScale_

    /// Zoom scale lock lens.
    static member ZoomScaleLock_ =
        BrowsedImage.Loaded_
            >-> LoadedImage.ZoomScaleLock_

    /// Offset prism.
    static member Offset_ =
        BrowsedImage.Loaded_
            >-> LoadedImage.Offset_

/// An image file that could not be loaded.
type LoadError =
    {
        /// Initialized container.
        Initialized : InitializedContainer

        /// Image file that couldn't be loaded.
        File : FileInfo

        /// Error message.
        Message : string
    }

    /// Initialized container lens.
    static member Initialized_ : Lens<_, _> =
        _.Initialized,
        fun inited errored ->
            { errored with Initialized = inited }

    /// Container size lens.
    static member ContainerSize_ =
        LoadError.Initialized_
            >-> InitializedContainer.ContainerSize_

type ImageModel =

    /// Uninitialized.
    | Uninitialized

    /// Initialized container.
    | Initialized of InitializedContainer

    /// Browsed file.
    | Browsed of BrowsedImage

    /// Loaded image.
    | Loaded of LoadedImage

    /// Image could not be loaded.
    | LoadError of LoadError

    /// Initialized container prism.
    static member TryInitialized_ : Prism<_, _> =

        (function
            | Initialized inited -> Some inited
            | Loaded loaded ->
                Some (loaded ^. LoadedImage.Initialized_)
            | Browsed browsed ->
                Some (browsed ^. BrowsedImage.Initialized_)
            | LoadError errored ->
                Some (errored ^. LoadError.Initialized_)
            | Uninitialized -> None),

        (fun inited -> function
            | Initialized _ -> Initialized inited
            | Loaded loaded ->
                loaded
                    |> inited ^= LoadedImage.Initialized_
                    |> Loaded
            | Browsed browsed ->
                browsed
                    |> inited ^= BrowsedImage.Initialized_
                    |> Browsed
            | LoadError errored ->
                errored
                    |> inited ^= LoadError.Initialized_
                    |> LoadError
            | Uninitialized -> Uninitialized)

    /// Initialized container lens.
    static member Initialized_ : Lens<_, _> =

        (fun model ->
            match model ^. ImageModel.TryInitialized_ with
                | Some inited -> inited
                | None -> failwith "Invalid state"),

        (fun inited model ->
            model
                |> inited ^= ImageModel.TryInitialized_)

    /// Container size lens.
    static member ContainerSize_ =
        ImageModel.Initialized_
            >-> InitializedContainer.ContainerSize_

    /// Loaded image prism.
    static member TryLoaded_ : Prism<_, _> =

        (function
            | Loaded loaded -> Some loaded
            | Browsed browsed ->
                Some (browsed ^. BrowsedImage.Loaded_)
            | Uninitialized
            | Initialized _
            | LoadError _ -> None),

        (fun loaded -> function
            | Loaded _ -> Loaded loaded
            | Browsed browsed ->
                browsed
                    |> loaded ^= BrowsedImage.Loaded_
                    |> Browsed
            | Uninitialized
            | Initialized _
            | LoadError _ as model -> model)

    /// Loaded image lens.
    static member Loaded_ : Lens<_, _> =

        (fun model ->
            match model ^. ImageModel.TryLoaded_ with
                | Some loaded -> loaded
                | None -> failwith "Invalid state"),

        (fun loaded model ->
            model
                |> loaded ^= ImageModel.TryLoaded_)

    /// Image file.
    member this.File =
        match this with
            | LoadError errored -> errored.File
            | _ -> (this ^. ImageModel.Loaded_).File

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
    let browse loaded incr (fromFile : FileInfo) =

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
                        Loaded = loaded
                        HasPreviousImage = toIdx > 0
                        HasNextImage = toIdx < files.Length - 1
                    }
            }

            // could not browse to file?
        modelOpt
            |> Option.defaultValue (Loaded loaded)

    /// Initial model.
    let init () = Uninitialized
