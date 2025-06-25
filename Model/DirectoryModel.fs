namespace Reliant.Photo

open System.IO

/// Directory model.
type DirectoryModel =
    {
        /// Current directory.
        Directory : DirectoryInfo

        /// Images in directory are in the process of loading?
        IsLoading : bool

        /// Loaded image results.
        ImageResults : (FileInfo * ImageResult)[]
    }

module DirectoryModel =

    /// Initializes model for the given directory.
    let init dir =
        {
            Directory = dir
            IsLoading = false
            ImageResults = Array.empty
        }

    let tryLoadDirectory targetHeight (dir : DirectoryInfo) =
        let files = dir.GetFiles()
        files
            |> Array.map (fun file ->
                async {
                    let! result =
                        ImageFile.tryLoadImage
                            (Some targetHeight)
                            file
                    return file, result
                })
            |> Async.Parallel
