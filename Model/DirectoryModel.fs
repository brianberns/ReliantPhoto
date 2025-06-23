namespace Reliant.Photo

open System.IO

/// Directory model.
type DirectoryModel =
    {
        Directory : DirectoryInfo
    }

module DirectoryModel =

    let init dir =
        {
            Directory = dir
        }
