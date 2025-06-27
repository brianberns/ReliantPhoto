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

module AsyncSeq =

    open FSharp.Control

    let chunkBySize (chunkSize : int) (source: AsyncSeq<'a>) =
        asyncSeq {
            use enumerator = source.GetEnumerator()
            let mutable isFinished = false
            while not isFinished do
                let chunk = ResizeArray<'a>(chunkSize)
                while chunk.Count < chunkSize && not isFinished do
                    match! enumerator.MoveNext() with
                        | Some item -> chunk.Add(item)
                        | None -> isFinished <- true
                if chunk.Count > 0 then
                    yield chunk.ToArray()
        }
