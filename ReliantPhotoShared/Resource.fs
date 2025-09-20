namespace Reliant.Photo

open System.Reflection

module Resource =

    /// Gets a resource by name.
    let get (path : string) (name : string) =
        Assembly
            .GetCallingAssembly()
            .GetManifestResourceStream($"{path}.{name}")
