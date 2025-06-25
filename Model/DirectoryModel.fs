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
        ImageLoadPairs : (FileInfo * ImageResult)[]

        /// Currently hovered image, if any.
        HoverFileOpt : Option<FileInfo>
    }

module DirectoryModel =

    /// Initializes model for the given directory.
    let init dir =
        {
            Directory = dir
            IsLoading = false
            ImageLoadPairs = Array.empty
            HoverFileOpt = None
        }

    /// Tries to load the contents of the given directory.
    let tryLoadDirectory targetHeight (dir : DirectoryInfo) =
        dir.GetFiles()
            |> Array.map (fun file ->
                async {
                    let! result =
                        ImageFile.tryLoadImage
                            (Some targetHeight)
                            file
                    return file, result
                })
            |> Async.Parallel
