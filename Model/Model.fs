namespace Reliant.Photo

open System.IO

/// Model.
type Model =
    {
        DirectoryModel : DirectoryModel
        ImageModelOpt : Option<ImageModel>
    }

module Model =

    let create dirModel imageModelOpt =
        {
            DirectoryModel = dirModel
            ImageModelOpt = imageModelOpt
        }

    let init = function
        | Choice1Of2 dir ->
            create
                (DirectoryModel.init dir)
                None
        | Choice2Of2 (file : FileInfo) ->
            create
                (DirectoryModel.init file.Directory)
                (ImageModel.init file |> Some)
