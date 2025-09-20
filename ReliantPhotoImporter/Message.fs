namespace Reliant.Photo

open System.IO

type Message =
    | SetSource of DirectoryInfo
    | SetDestination of DirectoryInfo

module Message =

    let update message model =
        match message with
            | SetSource dir ->
                { model with
                    Source = dir }
            | SetDestination dir ->
                { model with
                    Destination = dir }
