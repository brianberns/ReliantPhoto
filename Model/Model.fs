namespace Reliant.Photo

/// Top-level model.
type Model =

    /// Directory mode.
    | DirectoryMode of (DirectoryModel * Option<ImageModel>)

    /// Image mode.
    | ImageMode of (Option<DirectoryModel> * ImageModel)
