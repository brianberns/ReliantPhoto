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

/// An image result for a specific file.
type FileImageResult = FileInfo * Result<Bitmap, string (*error message*)>

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

    /// Tries to load thumbnails of images the given directory.
    let tryLoadDirectory height (dir : DirectoryInfo) =
        dir.EnumerateFiles("*", EnumerationOptions())   // ignore hidden and system files
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
    let tryBrowse (fromFile : FileInfo) incr =

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
        option {
            let! fromIdx =
                let idx =
                    Array.BinarySearch(
                        files, fromFile, fileComparer)
                if idx >= 0 then Some idx
                else None
            let toIdx = fromIdx + incr
            if toIdx >= 0 && toIdx < files.Length then
                let hasPreviousImage = toIdx > 0
                let hasNextImage = toIdx < files.Length - 1
                return hasPreviousImage, hasNextImage
        }
