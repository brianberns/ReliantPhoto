namespace Reliant.Photo

open System.IO

/// Directory model.
type DirectoryModel =
    {
        Directory : DirectoryInfo
        ImageResults : (FileInfo * ImageResult)[]
    }

module DirectoryModel =

    /// Initializes model for the given directory.
    let init dir =
        {
            Directory = dir
            ImageResults = Array.empty
        }

    let tryLoadDirectory (dir : DirectoryInfo) =
        let files = dir.GetFiles()
        files
            |> Array.map (fun file ->
                async {
                    let! result =
                        ImageFile.tryLoadImage file
                    return file, result
                })
            |> Async.Parallel
