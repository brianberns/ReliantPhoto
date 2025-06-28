namespace Reliant.Photo

open System.IO

/// Directory model.
type DirectoryModel =
    {
        /// Current directory.
        Directory : DirectoryInfo

        /// Loaded image results.
        ImageLoadPairs : (FileInfo * ImageResult)[]

        /// Directory is in the process of loading?
        IsLoading : bool
    }

module DirectoryModel =

    /// Initializes model for the given directory.
    let init dir =
        {
            Directory = dir
            ImageLoadPairs = Array.empty
            IsLoading = false
        }

    /// Tries to load the contents of the given directory.
    let tryLoadDirectory targetHeight (dir : DirectoryInfo) =
        dir.EnumerateFiles()
            |> Seq.map (fun file ->
                async {
                    let! result =
                        ImageFile.tryLoadImage
                            (Some targetHeight)
                            file
                    return file, result
                })
