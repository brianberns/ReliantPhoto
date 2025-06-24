namespace Reliant.Photo

open Avalonia.FuncUI.Types

module View =

    /// Creates a view of the given model.
    let view model dispatch =
        match model with
            | MkDirectoryModel dirModel ->
                DirectoryView.view
                    dirModel
                    (MkDirectoryMessage >> dispatch)
                    :> IView
            | MkImageModel imgModel ->
                ImageView.view
                    imgModel
                    (MkImageMessage >> dispatch)
