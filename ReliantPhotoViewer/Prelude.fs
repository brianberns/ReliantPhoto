namespace Reliant.Photo

/// Option computation expression builder.
type OptionBuilder() =
    member _.Bind(opt, f) = Option.bind f opt
    member _.Return(x) = Some x
    member _.ReturnFrom(opt : Option<_>) = opt
    member _.Zero() = None

[<AutoOpen>]
module OptionBuilder =

    /// Option computation expression builder.
    let option = OptionBuilder()

module Resource =

    open System.Reflection

    /// Gets a resource by name.
    let get name =
        Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream($"ReliantPhotoViewer.{name : string}")
