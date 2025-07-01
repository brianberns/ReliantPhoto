namespace Reliant.Photo

open System.IO

/// Top-level model.
type Model =
    {
        /// Directory model.
        DirectoryModel : DirectoryModel

        /// Image model, if in image mode.
        ImageModelOpt : Option<ImageModel>
    }

module Model =

    /// Initializes model for the given entity.
    let init (entity : FileSystemInfo) =
        match entity with
            | :? DirectoryInfo as dir ->
                {
                    DirectoryModel = DirectoryModel.init dir
                    ImageModelOpt = None
                }
            | :? FileInfo as file ->
                {
                    DirectoryModel = DirectoryModel.init file.Directory
                    ImageModelOpt = Some (ImageModel.init file)
                }
            | _ -> failwith "Unexpected file system entity"
