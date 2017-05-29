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

    
let allCombinations lst =
    let rec comb accLst elemLst =
        match elemLst with
        | h::t ->
            let next = [h]::List.map (fun el -> h::el) accLst @ accLst
            comb next t
        | _ -> accLst
    comb [] lst

let getCommitHistory (componentList : ComponentContent list) (componentNames : string list) =
    componentList
    |> List.filter (fun com -> componentNames |> List.contains com.componentName)
    |> List.sortBy (fun com -> com.version)

let getComponentList folder =
    let baseFile = folder </> "base.docker"
    
    let components = 
        folder
        |> Directory.GetDirectories
        |> List.ofArray
        |> List.map Path.GetFileName
         
    let allHistory = 
        folder
        |> Directory.GetDirectories
        |> Seq.map Directory.GetDirectories
        |> Seq.collect id
        |> Seq.map folderToComponentContent
        |> List.ofSeq
    
    components
    |> allCombinations
    |> Seq.iter 
        (fun comps -> 
            let hist = getCommitHistory allHistory comps
            
            let tracable = hist |> Seq.map (fun h->h.commitText)
            tracefn "%A" tracable
        )
    
    


Target "Trace" <| fun _ ->
    getComponentList "components" 

RunTargetOrDefault "Trace"
