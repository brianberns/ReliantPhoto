namespace Reliant.Photo

/// Top-level model.
type Model =

    /// Directory mode.
    | DirectoryMode of (DirectoryModel * Option<ImageModel>)

    /// Image mode.
    | ImageMode of (Option<DirectoryModel> * ImageModel)

module Model =

    /// Gets the given model's directory model, if it exists.
    let tryGetDirectoryModel = function
        | DirectoryMode (dirModel, _) -> Some dirModel
        | ImageMode (dirModelOpt, _) -> dirModelOpt

    /// Gets the given model's image model, if it exists.
    let tryGetImageModel = function
        | DirectoryMode (_, imgModelOpt) -> imgModelOpt
        | ImageMode (_, imgModel) -> Some imgModel
