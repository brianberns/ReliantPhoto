namespace Reliant.Photo

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

/// A file situated in a directory.
type SituatedFile =
    {
        /// Initialized container.
        Initialized : InitializedContainer

        /// File.
        File : FileInfo

        /// Previous file, if any.
        PreviousFileOpt : Option<FileInfo>

        /// Next file, if any.
        NextFileOpt : Option<FileInfo>
    }

    /// Initialized container lens.
    static member Initialized_ : Lens<_, _> =
        _.Initialized,
        fun inited loaded ->
            { loaded with Initialized = inited }

    /// Container size lens.
    static member ContainerSize_ =
        SituatedFile.Initialized_
            >-> InitializedContainer.ContainerSize_

module SituatedFile =

    /// Creates a situated file with no previous/next
    /// file.
    let create file inited =
        {
            Initialized = inited
            File = file
            PreviousFileOpt = None
            NextFileOpt = None
        }

    /// Updates a situated file.
    let update previousFileOpt nextFileOpt situated =
        {
            situated with
                PreviousFileOpt = previousFileOpt
                NextFileOpt = nextFileOpt
        }

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
        /// Image file.
        Situated : SituatedFile

        /// Loaded bitmap.
        Bitmap : Bitmap

        /// Bitmap size, adjusted for system DPI scale.
        BitmapSize : Size

        /// Pan location, when panning.
        PanOpt : Option<Pan>
    }

    /// Situated file lens.
    static member Situated_ : Lens<_, _> =
        _.Situated,
        fun situated loaded ->
            { loaded with Situated = situated }

    /// Initialized container lens.
    static member Initialized_ =
        LoadedImage.Situated_
            >-> SituatedFile.Initialized_

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

/// An image file that could not be loaded.
type LoadError =
    {
        /// Image file that couldn't be loaded.
        Situated : SituatedFile

        /// Error message.
        Message : string
    }

    /// Situated file lens.
    static member Situated_ : Lens<_, _> =
        _.Situated,
        fun situated errored ->
            { errored with Situated = situated }

    /// Initialized container lens.
    static member Initialized_ =
        LoadError.Situated_
            >-> SituatedFile.Initialized_

type ImageModel =

    /// Uninitialized.
    | Uninitialized

    /// Initialized container.
    | Initialized of InitializedContainer

    /// Situated file.
    | Situated of SituatedFile

    /// Loaded image.
    | Loaded of LoadedImage

    /// Image could not be loaded.
    | LoadError of LoadError

    /// Initialized container prism.
    static member TryInitialized_ : Prism<_, _> =

        (function
            | Initialized inited -> Some inited
            | Situated situated ->
                Some (situated ^.SituatedFile.Initialized_)
            | Loaded loaded ->
                Some (loaded ^. LoadedImage.Initialized_)
            | LoadError errored ->
                Some (errored ^. LoadError.Initialized_)
            | Uninitialized -> None),

        (fun inited -> function
            | Initialized _ -> Initialized inited
            | Situated situated ->
                situated
                    |> inited ^= SituatedFile.Initialized_
                    |> Situated
            | Loaded loaded ->
                loaded
                    |> inited ^= LoadedImage.Initialized_
                    |> Loaded
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

    /// Situated file prism.
    static member TrySituated_ : Prism<_, _> =

        (function
            | Loaded loaded -> Some loaded.Situated
            | LoadError errored -> Some errored.Situated
            | _ -> None),

        (fun situated -> function
            | Loaded loaded ->
                Loaded {
                    loaded with
                        Situated = situated }
            | LoadError errored ->
                LoadError {
                    errored with
                        Situated = situated }
            | model -> model)

    /// Situated file lens.
    static member Situated_ : Lens<_, _> =

        (fun model ->
            match model ^. ImageModel.TrySituated_ with
                | Some situated -> situated
                | None -> failwith "Invalid state"),

        (fun situated model ->
            model
                |> situated ^= ImageModel.TrySituated_)

    /// Image file.
    member this.File =
        (this ^. ImageModel.Situated_).File

module ImageModel =

    /// Initial model.
    let init () = Uninitialized

[<AutoOpen>]
module ImageModelExt =

    /// Initialized container active pattern.
    let (|Initialized_|_|) model =
        model ^. ImageModel.TryInitialized_

    /// Situated file active pattern.
    let (|Situated_|_|) model =
        model ^. ImageModel.TrySituated_
