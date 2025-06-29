namespace Reliant.Photo

open System.IO

/// An image result for a specific file.
type FileImageResult = FileInfo * ImageResult

/// Directory model.
type DirectoryModel =
    {
        /// Current directory.
        Directory : DirectoryInfo

        /// Loaded image results.
        FileImageResults : FileImageResult[]

        /// Directory is in the process of loading?
        IsLoading : bool
    }

module DirectoryModel =

    /// Initializes model for the given directory.
    let init dir =
        {
            Directory = dir
            FileImageResults = Array.empty
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
                    return ((file, result) : FileImageResult)
                })
