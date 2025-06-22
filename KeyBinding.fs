namespace Reliant.Photo

open System.Windows.Input

open Avalonia.Input
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.FuncUI.Builder

[<AutoOpen>]
module KeyBinding =
    
    let create (attrs: IAttr<KeyBinding> list) : IView<KeyBinding> =
        ViewBuilder.Create<KeyBinding>(attrs)

    type KeyBinding with

        static member gesture<'t when 't :> KeyBinding> (value: KeyGesture) : IAttr<'t> =
            AttrBuilder<'t>.CreateProperty<KeyGesture>(KeyBinding.GestureProperty, value, ValueNone)
    
        static member command<'t when 't :> KeyBinding> (value: ICommand) : IAttr<'t> =
            AttrBuilder<'t>.CreateProperty<ICommand>(KeyBinding.CommandProperty, value, ValueNone)
    
        static member commandParameter<'t when 't :> KeyBinding> (value: obj) : IAttr<'t> =
            AttrBuilder<'t>.CreateProperty<obj>(KeyBinding.CommandParameterProperty, value, ValueNone)
    
    type InputElement with

        static member keyBindings<'t when 't :> InputElement> (bindings: IView<KeyBinding> list) : IAttr<'t> =
            let getter: 't -> obj = fun control -> control.KeyBindings :> obj

            AttrBuilder<'t>.CreateContentMultiple(
                "KeyBindings",
                ValueSome getter,
                ValueNone,
                bindings |> List.map (fun v -> v :> IView))
