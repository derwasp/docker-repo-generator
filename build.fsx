#I @"./packages/builder/FAKE/tools"

#r "FakeLib.dll"

open System
open System.IO
open Fake

    module Constants =
        let BUILDER_PREFIX = "### BUILDER:"
        let BUILDER_COMMIT_MESSAGE = "COMMIT MESSAGE:"
        let COMPONENT_DOCKER = "component.docker"

type RepoRecord =
    { branch     : string
      commitText : string
      tag        : string
      items      : string list }

type ComponentContent =
    { componentName  : string
      content        : string
      version        : int64
      items          : string list
      commitText     : string }

let readCommitText file =
    let filteredContents = 
        File.ReadAllLines file
        |> Seq.filter (fun line -> line.Contains Constants.BUILDER_PREFIX)
        |> Seq.filter (fun line -> line.Contains Constants.BUILDER_COMMIT_MESSAGE)
        |> List.ofSeq
    
    match filteredContents with
        h::t -> h.Substring(h.IndexOf(Constants.BUILDER_COMMIT_MESSAGE) + Constants.BUILDER_COMMIT_MESSAGE.Length)
                |> trim
        | _ -> failwithf "No commit message is specified for the file %s" file

let readFileContents file =
    File.ReadAllLines file
    |> Seq.filter (fun line -> not <| line.Contains Constants.BUILDER_PREFIX)
    |> String.concat Environment.NewLine

let folderToComponentContent folder =
    let folder = Path.GetFullPath folder
    let file = folder </> Constants.COMPONENT_DOCKER
    let content = readFileContents file
    let commitText = readCommitText file
    let version = Path.GetFileName folder |> int64
    let componentName = folder
                        |> Directory.GetParent
                        |> string
                        |> Path.GetFileName
    tracefn "Component %s of version %i with commit text \"%s\"" componentName version commitText

    let items = !! (folder + "/**/*.*")
                -- (folder + "/**/*" + Constants.COMPONENT_DOCKER)
                |> List.ofSeq
    
    { componentName = componentName
      items = items
      commitText = commitText
      version = version
      content = content }

    

// let getComponentList folder =
//     let baseFile = folder </> "base.docker"


Target "Trace" <| fun _ ->
    Directory.GetDirectories "components"
    |> Seq.map Directory.GetDirectories
    |> Seq.collect id
    |> Seq.map folderToComponentContent
    |> tracefn "%A" 
     
    trace "Trace"

RunTargetOrDefault "Trace"
