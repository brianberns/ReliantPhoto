namespace Reliant.Photo

/// Top-level model.
type Model =

    /// Directory mode.
    | DirectoryMode of (DirectoryModel * ImageModel)

    /// Image mode.
    | ImageMode of (Option<DirectoryModel> * ImageModel)

module Model =

    /// Gets the given model's directory model, if it exists.
    let tryGetDirectoryModel = function
        | DirectoryMode (dirModel, _) -> Some dirModel
        | ImageMode (dirModelOpt, _) -> dirModelOpt

    /// Gets the given model's image model.
    let getImageModel = function
        | DirectoryMode (_, imgModel)
        | ImageMode (_, imgModel) -> imgModel
