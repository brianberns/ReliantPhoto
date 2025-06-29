namespace Reliant.Photo

open System.IO

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
