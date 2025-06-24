namespace Reliant.Photo

/// Model.
type Model =
    | MkDirectoryModel of DirectoryModel
    | MkImageModel of ImageModel

module Model =

    let init = function
        | Choice1Of2 dir ->
            DirectoryModel.init dir
                |> MkDirectoryModel
        | Choice2Of2 file ->
            ImageModel.init file
                |> MkImageModel
