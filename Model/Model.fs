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

    /// Initializes model for the given entity.
    let init = function
        | Choice1Of2 dir ->
            {
                DirectoryModel = DirectoryModel.init dir
                ImageModel = ImageModel.init ()
                Mode = Mode.Directory
            }
        | Choice2Of2 (file : FileInfo) ->
            {
                DirectoryModel = DirectoryModel.init file.Directory
                ImageModel = ImageModel.init ()
                Mode = Mode.Image
            }
