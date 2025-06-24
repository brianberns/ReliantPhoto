namespace Reliant.Photo

open System.IO

/// Top-level model.
type Model =

    /// Directory model.
    | MkDirectoryModel of DirectoryModel

    /// Image model.
    | MkImageModel of ImageModel

module Model =

    let init (entity : FileSystemInfo) =
        match entity with
            | :? DirectoryInfo as dir ->
                DirectoryModel.init dir
                    |> MkDirectoryModel
            | :? FileInfo as file ->
                ImageModel.init file
                    |> MkImageModel
            | _ -> failwith "Unexpected file system entity"
