namespace Reliant.Photo

open System
open System.IO

type Message =
    | SetSource of DirectoryInfo
    | SetDestination of DirectoryInfo
    | SetName of string
    | StartImport

module Message =

    let private onSetSource dir model =
        { model with
            SourceOpt = Some dir }

    let private onSetDestination dir model =
        { model with
            Destination = dir }

    let private onSetName name model =
        let nameOpt =
            if String.IsNullOrWhiteSpace name then
                None
            else Some name
        { model with
            NameOpt = nameOpt }

    let update message model =
        match message with
            | SetSource dir ->
                onSetSource dir model
            | SetDestination dir ->
                onSetDestination dir model
            | SetName name ->
                onSetName name model
            | StartImport ->
                model
