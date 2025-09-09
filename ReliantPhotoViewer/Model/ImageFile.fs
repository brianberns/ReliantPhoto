namespace Reliant.Photo

open System
open System.IO
open System.Runtime.InteropServices
open System.Threading

open Avalonia.Media.Imaging

module FileSystemInfo =

    /// Path string comparison.
    let comparison =
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            || RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
                StringComparison.OrdinalIgnoreCase
        else StringComparison.Ordinal

module DirectoryInfo =

    /// Normalizes the given directory's path.
    let normalizedPath (dir : DirectoryInfo) =
        dir.FullName
            |> Path.TrimEndingDirectorySeparator

    /// Directory equality.
    let same dirA dirB =
        String.Equals(
            normalizedPath dirA,
            normalizedPath dirB,
            FileSystemInfo.comparison)

module FileInfo =

    /// File equality.
    let same (fileA : FileInfo) (fileB : FileInfo) =
        String.Equals(
            fileA.FullName,
            fileB.FullName,
            FileSystemInfo.comparison)

    /// Waits for the given file to be readable.
    let waitForFileRead
        (token : CancellationToken) (file : FileInfo) =

        let rec loop duration =
            async {
                try
                    use _ = file.OpenRead()
                    return ()
                with
                    | :? IOException as exn
                        when exn.HResult = 0x80070020 ->   // file in use by another process
                        if not token.IsCancellationRequested then
                            do! Async.Sleep(duration : int)
                            return! loop (2 * duration)
                    | :? IOException as exn
                        when exn.HResult = 0x80070002 ->   // file no longer exists
                        ()
            }

        loop 100

/// Image result
type ImageResult = Result<Bitmap, string (*error message*)>

/// An image result for a specific file.
type FileImageResult = FileInfo * ImageResult

/// EXIF metadata.
type ExifMetadata =
    {
        /// Date taken.
        DateTakenOpt : Option<DateTime>

        /// Camera make.
        CameraMakeOpt : Option<string>

        /// Camera model.
        CameraModelOpt : Option<string>

        /// F-stop.
        FStopOpt : Option<decimal>

        /// Exposure time.
        ExposureTimeOpt : Option<decimal>

        /// ISO speed rating.
        IsoRatingOpt : Option<decimal>

        /// Focal length.
        FocalLengthOpt : Option<decimal>

        /// Full-frame focal length equivalent.
        FocalLengthFullFrameOpt : Option<decimal>
    }

module ExifMetadata =

    open ImageSharp

    /// Creates an EXIF metadata.
    let create exifProfile =
        {
            DateTakenOpt = tryGetDateTaken exifProfile
            CameraMakeOpt = tryGetCameraMake exifProfile
            CameraModelOpt = tryGetCameraModel exifProfile
            FStopOpt = tryGetFStop exifProfile
            ExposureTimeOpt = tryGetExposureTime exifProfile
            IsoRatingOpt = tryGetIsoRating exifProfile
            FocalLengthOpt = tryGetFocalLength exifProfile
            FocalLengthFullFrameOpt =
                tryGetFocalLengthFullFrame exifProfile
        }

module ImageFile =

    /// Tries to load an image from the given file.
    let tryLoadImage file =
        async {
            try
                let image = ImageSharp.loadImage file
                return Ok image
            with exn ->
                return Error exn.Message
        }

    /// Tries to load a thumbnail image from the given file.
    let tryLoadThumbnail height file =
        async {
            try
                let thumbnail = ImageSharp.loadThumbnail height file
                return Ok thumbnail
            with exn ->
                return Error exn.Message
        }

    /// Enumerates files in the given directory.
    let private enumerateFiles (dir : DirectoryInfo) =
        dir.EnumerateFiles("*", EnumerationOptions())   // ignore hidden and system files

    /// File sort key.
    type private SortKey = DateTime (*date taken*) * string (*file name*)

    /// Creates a sort key.
    let private toSortKey
        dateTakenOpt (file : FileInfo) : SortKey =
        let dateTaken =
            dateTakenOpt
                |> Option.defaultValue DateTime.MaxValue   // sort missing dates to the end
        dateTaken, file.Name

    /// Gets the sort key of the given file.
    let private getSortKey file : SortKey =
        let dateTakenOpt =
            option {
                let! exifProfile =
                    ImageSharp.tryGetExifProfile file
                return! ImageSharp.tryGetDateTaken exifProfile
            }
        toSortKey dateTakenOpt file

    /// Tries to load thumbnails of images in the given
    /// directory.
    let tryLoadDirectory height dir =
        dir
            |> enumerateFiles
            |> Seq.sortBy getSortKey
            |> Seq.map (fun file ->
                async {
                    let! result =
                        tryLoadThumbnail height file
                    return ((file, result) : FileImageResult)
                })

    /// Gets the appropriate interpolation mode for the given
    /// image file. The goal is to display crisp edges in
    /// lossless images without introducing zoom artifacts in
    /// lossy images.
    let getInterpolationMode (file : FileInfo) =
        match file.Extension.ToLower() with
            | ".gif" | ".bmp" | ".png" | ".tif" ->
                BitmapInterpolationMode.None
            | _ -> BitmapInterpolationMode.HighQuality

    /// Situates a file within its directory.
    let situate (file : FileInfo) =

            // get file length
        let fileLengthOpt =
            try Some file.Length   // this performs I/O, so it can fail (e.g. file no longer exists)
            with _ -> None

            // get key of target file
        let exifMetadataOpt =
            ImageSharp.tryGetExifProfile file
                |> Option.map ExifMetadata.create
        let targetKey =
            let dateTakenOpt =
                Option.bind _.DateTakenOpt exifMetadataOpt
            toSortKey dateTakenOpt file

            // examine all files in target's directory
        let files = enumerateFiles file.Directory
        let prevPairOpt, nextPairOpt =
            ((None, None), files)
                ||> Seq.fold (fun (prevPairOpt, nextPairOpt) curFile ->

                        // get key of current file
                    let curKey = getSortKey curFile
                    assert(curKey <> targetKey
                        || FileInfo.same curFile file)

                        // find nearest inferior neighbor
                    let prevPairOpt =
                        if curKey < targetKey then
                            prevPairOpt
                                |> Option.filter (fun (prevKey, _) ->
                                    prevKey > curKey)
                                |> Option.orElse (
                                    Some (curKey, curFile))
                        else prevPairOpt

                        // find nearest superior neighbor
                    let nextPairOpt =
                        if curKey > targetKey then
                            nextPairOpt
                                |> Option.filter (fun (nextKey, _) ->
                                    nextKey < curKey)
                                |> Option.orElse (
                                    Some (curKey, curFile))
                        else nextPairOpt

                    prevPairOpt, nextPairOpt)

        {|
            FileLengthOpt = fileLengthOpt
            ExifMetadataOpt = exifMetadataOpt
            PreviousFileOpt = Option.map snd prevPairOpt
            NextFileOpt = Option.map snd nextPairOpt
        |}
