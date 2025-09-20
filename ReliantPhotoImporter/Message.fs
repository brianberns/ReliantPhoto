namespace Reliant.Photo

open System.IO

type Message =
    | SetSource of DirectoryInfo

module Message =

    let update message model =
        match message with
            | SetSource dir ->
                { model with
                    Source = dir }
