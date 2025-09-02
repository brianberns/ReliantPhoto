namespace Reliant.Photo

open System
open System.Collections.Generic
open System.IO
open System.Threading

open Avalonia.Media.Imaging

module FileSystemInfo =

    /// File/directory equality.
    let same<'t when 't :> FileSystemInfo> (fsiA : 't) (fsiB : 't) =
        fsiA.FullName = fsiB.FullName

module FileInfo =

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
        toSortKey (ImageSharp.tryGetDateTaken file) file

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
    let situate file =

            // get key of target file
        let dateTakenOpt = ImageSharp.tryGetDateTaken file
        let targetKey = toSortKey dateTakenOpt file

            // examine all files in target's directory
        let files = enumerateFiles file.Directory
        let prevPairOpt, nextPairOpt =
            ((None, None), files)
                ||> Seq.fold (fun (prevPairOpt, nextPairOpt) curFile ->

                        // get key of current file
                    let curKey = getSortKey curFile
                    assert(curKey <> targetKey
                        || FileSystemInfo.same curFile file)

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
            DateTakenOpt = dateTakenOpt
            PreviousFileOpt = Option.map snd prevPairOpt
            NextFileOpt = Option.map snd nextPairOpt
        |}
