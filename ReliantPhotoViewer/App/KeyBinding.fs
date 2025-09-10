namespace Reliant.Photo

open System
open System.Reactive.Subjects
open System.Windows.Input

open Avalonia.Input

open Elmish

module KeyBinding =

    /// A command that pushes a message to a subject.
    type private MessageCommand(
        subject : ISubject<Message>, msg : Message) =

        let canExecuteChanged = Event<EventHandler, EventArgs>()

        interface ICommand with
            member _.CanExecute(_) = true
            member _.Execute(_) = subject.OnNext(msg)
            [<CLIEvent>]
            member _.CanExecuteChanged = canExecuteChanged.Publish

    /// Creates bindings for the given key-message pairs.
    let createBindings mappings =
        let subject = new Subject<Message>()
        let keyBindings =
            mappings
                |> List.map (fun (key, msg) ->
                    KeyBinding(
                        Gesture = KeyGesture(key),
                        Command = MessageCommand(subject, msg)))
        keyBindings, subject :> IObservable<Message>
