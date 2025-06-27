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

    let chunkBySize (chunkSize: int) (source: AsyncSeq<'a>) : AsyncSeq<'a[]> =
        // We create a new async sequence using the computation expression.
        asyncSeq {
            use enumerator = source.GetEnumerator()

            let mutable isFinished = false
            while not isFinished do
                // Use a mutable list to build the current chunk.
                let chunk = ResizeArray<'a>(chunkSize)

                // Keep adding to the chunk until it's full or the source stream ends.
                while chunk.Count < chunkSize && not isFinished do
                    // Asynchronously move to the next item.
                    match! enumerator.MoveNext() with
                        | Some value -> chunk.Add(value)
                        | None -> isFinished <- true

                // If the chunk has any items (for the last partial chunk), yield it.
                if chunk.Count > 0 then
                    yield chunk.ToArray()
        }
