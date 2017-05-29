#I @"./packages/builder/FAKE/tools"

#r "FakeLib.dll"

open System.IO
open Fake

Target "Trace" <| fun _ ->
    trace "Trace"

RunTargetOrDefault "Trace"
