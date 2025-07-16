namespace Reliant.Photo

open System.IO

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

module Model =

    /// Initializes model for the given directory.
    let init directory =
        {
            DirectoryModel = DirectoryModel.init directory
            ImageModel = ImageModel.init ()
            Mode = Mode.Directory
        }
