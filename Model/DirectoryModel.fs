namespace Reliant.Photo

open System.IO

/// Directory model.
type DirectoryModel =
    {
        Directory : DirectoryInfo
    }

module DirectoryModel =

    /// Initializes model for the given directory.
    let init dir =
        {
            Directory = dir
        }
