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
