namespace Reliant.Photo

/// Model mode.
[<RequireQualifiedAccess>]
type Mode = Directory | Image

/// Top-level model.
type Model =
    {
        /// Directory model.
        DirectoryModel : DirectoryModel

        /// Image model.
        ImageModel : ImageModel

        /// Current mode.
        Mode : Mode
    }
