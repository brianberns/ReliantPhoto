namespace Reliant.Photo

// https://stackoverflow.com/questions/62082930/best-way-to-do-trymax-and-trymin-in-f
module Seq =

    /// Tries to find the minimum item in a sequence based on a
    /// projection function.
    let tryMinBy projection (items : seq<_>) =
        use e = items.GetEnumerator ()
        if e.MoveNext () then
            let mutable minItem = e.Current
            let mutable minValue = projection minItem
            while e.MoveNext () do
                let value = projection e.Current
                if value < minValue
                then
                    minItem <- e.Current
                    minValue <- value
            Some minItem
        else None

    /// Tries to find the maximum item in a sequence based on a
    /// projection function.
    let tryMaxBy projection (items : seq<_>) =
        use e = items.GetEnumerator ()
        if e.MoveNext ()
        then
            let mutable maxItem = e.Current
            let mutable maxValue = projection maxItem
            while e.MoveNext () do
                let value = projection e.Current
                if value > maxValue then
                    maxItem <- e.Current
                    maxValue <- value
            Some maxItem
        else None

    /// Tries to find the minimum item in a sequence.
    let tryMin (items : seq<_>) =
        use e = items.GetEnumerator ()
        if e.MoveNext () then
            let mutable minItem = e.Current
            while e.MoveNext () do
                if e.Current < minItem then
                    minItem <- e.Current
            Some minItem
        else None

    /// Tries to find the maximum item in a sequence.
    let tryMax (items : seq<_>) =
        use e = items.GetEnumerator ()
        if e.MoveNext () then
            let mutable maxItem = e.Current
            while e.MoveNext () do
                if e.Current > maxItem then
                    maxItem <- e.Current
            Some maxItem
        else None
