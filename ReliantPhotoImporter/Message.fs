namespace Reliant.Photo

open System
open System.IO

type Message =
    | SetSource of DirectoryInfo
    | SetDestination of DirectoryInfo
    | SetName of string

module Message =

    let update message model =
        match message with
            | SetSource dir ->
                { model with
                    SourceOpt = Some dir }
            | SetDestination dir ->
                { model with
                    Destination = dir }
            | SetName name ->
                let nameOpt =
                    if String.IsNullOrWhiteSpace name then None
                    else Some name
                { model with
                    NameOpt = nameOpt }
