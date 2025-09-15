namespace Reliant.Photo

open System.Reflection

module Resource =

    /// Gets a resource by name.
    let get name =
        Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream(
                $"ReliantPhotoImporter.Assets.{name : string}")
