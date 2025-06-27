namespace Reliant.Photo

open System.IO
open FSharp.Control

/// Directory model.
type DirectoryModel =
    {
        /// Current directory.
        Directory : DirectoryInfo

        /// Images in directory are in the process of loading?
        IsLoading : bool

        /// Loaded image results.
        ImageLoadPairs : (FileInfo * ImageResult)[]
    }

module DirectoryModel =

    /// Initializes model for the given directory.
    let init dir =
        {
            Directory = dir
            IsLoading = false
            ImageLoadPairs = Array.empty
        }

    /// Tries to load the contents of the given directory.
    let tryLoadDirectory targetHeight (dir : DirectoryInfo) =
        dir.EnumerateFiles()
            |> AsyncSeq.ofSeq
            |> AsyncSeq.mapAsync (fun file ->
                async {
                    let! result =
                        ImageFile.tryLoadImage
                            (Some targetHeight)
                            file
                    return file, result
                })
