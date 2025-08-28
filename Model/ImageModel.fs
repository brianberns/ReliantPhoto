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
        /// File.
        File : FileInfo

        /// Previous file, if any.
        PreviousFileOpt : Option<FileInfo>

        /// Next file, if any.
        NextFileOpt : Option<FileInfo>
    }

module SituatedFile =

    /// Creates a situated file.
    let private create file previousFileOpt nextFileOpt =
        {
            File = file
            PreviousFileOpt = previousFileOpt
            NextFileOpt = nextFileOpt
        }

    /// Initializes a situated file with no previous/next
    /// file.
    let initialize file =
        create file None None

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
        /// Initialized container.
        Initialized : InitializedContainer

        /// Image file.
        Situated : SituatedFile

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

/// An image file that could not be loaded.
type LoadError =
    {
        /// Initialized container.
        Initialized : InitializedContainer

        /// Image file that couldn't be loaded.
        Situated : SituatedFile

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
            | LoadError errored ->
                Some (errored ^. LoadError.Initialized_)
            | Uninitialized -> None),

        (fun inited -> function
            | Initialized _ -> Initialized inited
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

    /// Loaded image prism.
    static member TryLoaded_ : Prism<_, _> =

        (function
            | Loaded loaded -> Some loaded
            | Uninitialized
            | Initialized _
            | LoadError _ -> None),

        (fun loaded -> function
            | Loaded _ -> Loaded loaded
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

    /// Loaded image active pattern.
    let (|Loaded_|_|) model =
        model ^. ImageModel.TryLoaded_
