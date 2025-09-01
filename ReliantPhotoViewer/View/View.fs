namespace Reliant.Photo

open Avalonia.FuncUI.Types

module View =

    /// Creates a view of the given model.
    let view model dispatch =
        match model with
            | DirectoryMode (dirModel, _) ->
                DirectoryView.view dirModel dispatch
                    :> IView
            | ImageMode (_, imgModel) ->
                ImageView.view imgModel dispatch
