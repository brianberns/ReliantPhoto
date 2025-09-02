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

module Dictionary =

    /// Converts a dictionary lookup value to an option.
    let toOption (flag, value) =
        if flag then Some value
        else None
