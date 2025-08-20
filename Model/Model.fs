namespace Reliant.Photo

/// Top-level model.
type Model =

    /// Directory model.
    | MkDirectoryModel of DirectoryModel

    /// Image model.
    | MkImageModel of ImageModel
