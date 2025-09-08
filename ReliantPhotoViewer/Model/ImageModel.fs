namespace Reliant.Photo

open System.IO

open Avalonia
open Avalonia.Media.Imaging

open Aether
open Aether.Operators

/// Initialized model.
type Initial =
    {
        /// System DPI scale.
        DpiScale : float
    }

    /// DPI scale lens.
    static member DpiScale_ : Lens<_, _> =
        _.DpiScale,
        fun dpiScale initial ->
            { initial with DpiScale = dpiScale }

/// Zoom details.
type Zoom =
    {
        /// Zoom scale. This will be 1.0 for an image displayed
        /// at 1:1 size.
        Scale : float

        /// Lock zoom scale when resizing?
        ScaleLock : bool
    }

    /// Scale lens.
    static member Scale_ : Lens<_, _> =
        _.Scale,
        fun scale zoom ->
            { zoom with Scale = scale }

    /// Scale lock lens.
    static member ScaleLock_ : Lens<_, _> =
        _.ScaleLock,
        fun scaleLock zoom ->
            { zoom with ScaleLock = scaleLock }

module Zoom =

    /// Creates a zoom.
    let create scale scaleLock =
        {
            Scale = scale
            ScaleLock = scaleLock
        }

    /// 1:1 zoom.
    let actualSize =
        create 1.0 true

/// Sized container.
type SizedContainer =
    {
        /// Initialized model.
        Initial : Initial

        /// Container size.
        ContainerSize : Size

        /// Image location offset, if any.
        OffsetOpt : Option<Point>

        /// Zoom details.
        Zoom : Zoom

        /// Full-screen mode.
        FullScreen : bool
    }

    /// Container size lens.
    static member ContainerSize_ : Lens<_, _> =
        _.ContainerSize,
        fun containerSize sized ->
            { sized with ContainerSize = containerSize }

    /// Offset prism.
    static member Offset_ : Prism<_, _> =
        _.OffsetOpt,
        fun offset sized ->
            { sized with OffsetOpt = Some offset }

    /// Zoom lens. (Hah!)
    static member Zoom_ : Lens<_, _> =
        _.Zoom,
        fun zoom sized ->
            { sized with Zoom = zoom }

    /// Zoom scale lens.
    static member ZoomScale_ =
        SizedContainer.Zoom_
            >-> Zoom.Scale_

    /// Zoom scale lock lens.
    static member ZoomScaleLock_ =
        SizedContainer.Zoom_
            >-> Zoom.ScaleLock_

    /// Initialized model lens.
    static member Initial_ : Lens<_, _> =
        _.Initial,
        fun initial sized ->
            { sized with Initial = initial }

    /// DPI scale lens.
    static member DpiScale_ =
        SizedContainer.Initial_
            >-> Initial.DpiScale_

module SizedContainer =

    /// Dummy zoom.
    let private dummyZoom = Zoom.create 0.0 false

    /// Creates a sized container.
    let create initial containerSize =
        {
            Initial = initial
            ContainerSize = containerSize
            OffsetOpt = None
            Zoom = dummyZoom
            FullScreen = false
        }

    /// Get locked zoom scale, if any.
    let tryGetLockedZoomScale sized =
        if sized.Zoom.ScaleLock then
            Some sized.Zoom.Scale
        else None

/// Situation of a file in a directory.
type Situation =
    {
        /// File's EXIF metadata, if any.
        ExifMetadataOpt : Option<ExifMetadata>

        /// Previous image result, if any.
        PreviousResultOpt : Option<FileImageResult>

        /// Next image result, if any.
        NextResultOpt : Option<FileImageResult>
    }

module Situation =

    /// Creates a situation.
    let create
        exifMetadataOpt previousResultOpt nextResultOpt =
        {
            ExifMetadataOpt = exifMetadataOpt
            PreviousResultOpt = previousResultOpt
            NextResultOpt = nextResultOpt
        }

    /// Unknown situation.
    let unknown =
        create None None None

/// A file situated in a directory.
type SituatedFile =
    {
        /// Sized container.
        Sized : SizedContainer

        /// File.
        File : FileInfo

        /// File situation.
        Situation : Situation
    }

    /// Sized container lens.
    static member Sized_ : Lens<_, _> =
        _.Sized,
        fun sized loaded ->
            { loaded with Sized = sized }

module SituatedFile =

    /// Creates a situated file with unknown situation.
    let create file sized =
        {
            Sized = sized
            File = file
            Situation = Situation.unknown
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

        /// Saved zoom, if any.
        SavedZoomOpt : Option<Zoom>

        /// Pan location, when panning.
        PanOpt : Option<Pan>
    }

    /// Situated file lens.
    static member Situated_ : Lens<_, _> =
        _.Situated,
        fun situated loaded ->
            { loaded with Situated = situated }

    /// Sized container lens.
    static member Sized_ =
        LoadedImage.Situated_
            >-> SituatedFile.Sized_

    /// Container size lens.
    static member ContainerSize_ =
        LoadedImage.Sized_
            >-> SizedContainer.ContainerSize_

    /// Zoom lens.
    static member Zoom_ =
        LoadedImage.Sized_
            >-> SizedContainer.Zoom_

    /// Zoom scale lens.
    static member ZoomScale_ =
        LoadedImage.Sized_
            >-> SizedContainer.ZoomScale_

    /// Zoom scale lock lens.
    static member ZoomScaleLock_ =
        LoadedImage.Sized_
            >-> SizedContainer.ZoomScaleLock_

    /// Offset prism.
    static member Offset_ =
        LoadedImage.Sized_
            >-> SizedContainer.Offset_

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

    /// Sized container lens.
    static member Sized_ =
        LoadError.Situated_
            >-> SituatedFile.Sized_

type ImageModel =

    /// Initial state.
    | Initialized of Initial

    /// Sized container.
    | Sized of SizedContainer

    /// Situated file.
    | Situated of SituatedFile

    /// Loaded image.
    | Loaded of LoadedImage

    /// Image could not be loaded.
    | LoadError of LoadError

    /// Sized container prism.
    static member TrySized_ : Prism<_, _> =

        (function
            | Sized sized -> Some sized
            | Situated situated ->
                Some (situated ^.SituatedFile.Sized_)
            | Loaded loaded ->
                Some (loaded ^. LoadedImage.Sized_)
            | LoadError errored ->
                Some (errored ^. LoadError.Sized_)
            | Initialized _ -> None),

        (fun sized -> function
            | Sized _ -> Sized sized
            | Situated situated ->
                situated
                    |> sized ^= SituatedFile.Sized_
                    |> Situated
            | Loaded loaded ->
                loaded
                    |> sized ^= LoadedImage.Sized_
                    |> Loaded
            | LoadError errored ->
                errored
                    |> sized ^= LoadError.Sized_
                    |> LoadError
            | Initialized _ as model ->
                model)

    /// Sized container lens.
    static member Sized_ : Lens<_, _> =

        (fun model ->
            match model ^. ImageModel.TrySized_ with
                | Some sized -> sized
                | None -> failwith "Invalid state"),

        (fun sized model ->
            model
                |> sized ^= ImageModel.TrySized_)

    /// Container size lens.
    static member ContainerSize_ =
        ImageModel.Sized_
            >-> SizedContainer.ContainerSize_

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
    let init dpiScale =
        Initialized { DpiScale = dpiScale }

[<AutoOpen>]
module ImageModelExt =

    /// Sized container active pattern.
    let (|Sized_|_|) model =
        model ^. ImageModel.TrySized_

    /// Situated file active pattern.
    let (|Situated_|_|) model =
        model ^. ImageModel.TrySituated_
