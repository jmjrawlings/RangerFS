// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r "paket: groupref FakeBuild //"

#load "./.fake/build.fsx/intellisense.fsx"

open System.IO
open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.IO.Globbing
open Fake.DotNet.Testing
open Fake.Tools
open Fake.Api

// The name of the project
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "RangerFS"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "Intervals for F#"

// Longer description of the project
// (used as a description for NuGet package; line breaks are automatically cleaned up)
let description = "A library for generic interval arithmetic"

// List of author names (for NuGet package)
let author = "Justin Rawlings"

// Tags for your project (for NuGet package)
let tags = "Interval, FSharp, DotNetCore, DotNet"

// File system information
let solutionFile  = "RangerFS.sln"

// Default target configuration
let configuration = "Release"

// Pattern specifying assemblies to be tested using Expecto
let testAssemblies = "tests/**/bin" </> configuration </> "**" </> "*Tests.exe"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted
let gitOwner = "jmjrawlings"
let gitHome = sprintf "%s/%s" "https://github.com/" gitOwner

// The name of the project on GitHub
let gitName = "RangerFS"

// The url for the raw files hosted
let gitRaw = Environment.environVarOrDefault "gitRaw" "https://raw.githubusercontent.com/jmjrawlings"

let website = "/RangerFS"

// --------------------------------------------------------------------------------------
// The rest of the file includes standard build steps
// --------------------------------------------------------------------------------------

// Read additional information from the release notes document
let release = ReleaseNotes.load "RELEASE_NOTES.md"

// Helper active pattern for project types
let (|Fsproj|Csproj|) (projFileName:string) =
    match projFileName with
    | f when f.EndsWith("fsproj") -> Fsproj
    | f when f.EndsWith("csproj") -> Csproj
    | _                           -> failwith (sprintf "Project file %s not supported. Unknown project type." projFileName)

// Generate assembly info files with the right version & up-to-date information
Target.create "AssemblyInfo" <| fun _ ->
    let getAssemblyInfoAttributes projectName =
        [ AssemblyInfo.Title (projectName)
          AssemblyInfo.Product project
          AssemblyInfo.Description summary
          AssemblyInfo.Version release.AssemblyVersion
          AssemblyInfo.FileVersion release.AssemblyVersion
          AssemblyInfo.Configuration configuration ]

    let getProjectDetails projectPath =
        let projectName = Path.GetFileNameWithoutExtension(projectPath)
        ( projectPath,
          projectName,
          Path.GetDirectoryName(projectPath),
          (getAssemblyInfoAttributes projectName)
        )

    !! "src/**/*.??proj"
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (projFileName, _, folderName, attributes) ->
        match projFileName with
        | Fsproj -> AssemblyInfoFile.createFSharp (folderName </> "AssemblyInfo.fs") attributes
        | Csproj -> AssemblyInfoFile.createCSharp ((folderName </> "Properties") </> "AssemblyInfo.cs") attributes
        )


// Copies binaries from default VS location to expected bin folder
// But keeps a subdirectory structure for each project in the
// src folder to support multiple project outputs
Target.create "CopyBinaries" <| fun _ ->
    !! "src/**/*.??proj"
    -- "src/**/*.shproj"
    |>  Seq.map (fun f -> ((Path.getDirectory f) </> "bin" </> configuration, "bin" </> (Path.GetFileNameWithoutExtension f)))
    |>  Seq.iter (fun (fromDir, toDir) -> Shell.copyDir toDir fromDir (fun _ -> true))


// --------------------------------------------------------------------------------------
// Clean build results

let buildConfiguration = 
    configuration
    |> Environment.environVarOrDefault "configuration" 
    |> DotNet.BuildConfiguration.Custom

Target.create "Clean" <| fun _ ->
    Shell.cleanDirs ["bin"; "temp"]

Target.create "CleanDocs" <| fun _ ->
    Shell.cleanDirs ["docs"]

// --------------------------------------------------------------------------------------
// Build library & test project

Target.create "Build" <| fun _ ->

    let options =
        DotNet.Options.Create()
        |> DotNet.Options.withVerbosity (Some DotNet.Verbosity.Quiet)
        |> DotNet.Options.withCustomParams (Some "-property:Optimize=True -property:DebugSymbols=True")

    let setParams (defaults: DotNet.BuildOptions) =
        { defaults with
            Configuration = buildConfiguration
            Common = options }

    solutionFile
    |> DotNet.build setParams
    

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner

Target.create "RunTests" <| fun _ ->

    let setParams f =
        match Environment.isWindows with
        | true ->
            fun p ->
                { p with
                    FileName = f}
        | false ->
            fun p ->
                { p with
                    FileName = "mono"
                    Arguments = f }
                    
    !! testAssemblies
    |> Seq.map (fun f ->
        Process.execSimple (setParams f) System.TimeSpan.MaxValue
    )
    |>Seq.reduce (+)
    |> (fun i -> if i > 0 then failwith "")
    

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target.create "NuGet" <| fun _ ->

    let args (p: Paket.PaketPackParams) =
        { p with
            OutputPath = "bin"
            Version = release.NugetVersion
            ReleaseNotes = String.toLines release.Notes}

    Paket.pack args
    
Target.create "PublishNuget" <| fun _ ->

    let args (p: Paket.PaketPushParams) =
        { p with
            PublishUrl = "https://www.nuget.org"
            WorkingDir = "bin" }

    Paket.push args            


// --------------------------------------------------------------------------------------
// Generate the documentation

// Paths with template/source/output locations
let bin        = __SOURCE_DIRECTORY__ @@ "bin"
let content    = __SOURCE_DIRECTORY__ @@ "docsrc/content"
let output     = __SOURCE_DIRECTORY__ @@ "docs"
let files      = __SOURCE_DIRECTORY__ @@ "docsrc/files"
let templates  = __SOURCE_DIRECTORY__ @@ "docsrc/tools/templates"
let formatting = __SOURCE_DIRECTORY__ @@ "packages/formatting/FSharp.Formatting"
let docTemplate = "docpage.cshtml"

let github_release_user = Environment.environVarOrDefault "github_release_user" gitOwner
let githubLink = sprintf "https://github.com/%s/%s" github_release_user gitName

// Specify more information about your project
let info =
  [ "project-name",    "RangerFS"
    "project-author",  "Justin Rawlings"
    "project-summary", "Intervals for .NET"
    "project-github",  githubLink
    "project-nuget",   "http://nuget.org/packages/RangerFS" ]

let root = website

let referenceBinaries = []

let layoutRootsAll = new System.Collections.Generic.Dictionary<string, string list>()
layoutRootsAll.Add("en",[   templates;
                            formatting @@ "templates"
                            formatting @@ "templates/reference" ])


let createReferenceDocs () =                            

    Directory.ensure (output @@ "reference")

    let binaries () =
        let manuallyAdded =
            referenceBinaries
            |> List.map (fun b -> bin @@ b)

        let conventionBased =
            DirectoryInfo.getSubDirectories <| DirectoryInfo bin
            |> Array.collect (fun d ->
                let name, dInfo =
                    let net45Bin =
                        DirectoryInfo.getSubDirectories d |> Array.filter(fun x -> x.FullName.ToLower().Contains("net45"))
                    let net47Bin =
                        DirectoryInfo.getSubDirectories d |> Array.filter(fun x -> x.FullName.ToLower().Contains("net47"))
                    if net45Bin.Length > 0 then
                        d.Name, net45Bin.[0]
                    else
                        d.Name, net47Bin.[0]

                dInfo.GetFiles()
                |> Array.filter (fun x ->
                    x.Name.ToLower() = (sprintf "%s.dll" name).ToLower())
                |> Array.map (fun x -> x.FullName)
                )
            |> List.ofArray

        conventionBased @ manuallyAdded

    binaries()
    |> FSFormatting.createDocsForDlls (fun args ->
        { args with
            OutputDirectory = output @@ "reference"
            LayoutRoots =  layoutRootsAll.["en"]
            ProjectParameters =  ("root", root)::info
            SourceRepository = githubLink @@ "tree/master" }
           )


let copyFiles () =
    Shell.copyRecursive files output true
    |> Trace.logItems "Copying file: "
    Directory.ensure (output @@ "content")
    Shell.copyRecursive (formatting @@ "styles") (output @@ "content") true
    |> Trace.logItems "Copying styles and scripts: "


Target.create "ReferenceDocs" <| fun _ ->
    createReferenceDocs ()


let createDocs () =
    File.delete "docsrc/content/release-notes.md"
    Shell.copyFile "docsrc/content/" "RELEASE_NOTES.md"
    Shell.rename 
        "docsrc/content/release-notes.md"
        "docsrc/content/RELEASE_NOTES.md"

    File.delete "docsrc/content/license.md"
    Shell.copyFile "docsrc/content/" "LICENSE.txt"
    Shell.rename 
        "docsrc/content/license.md"
        "docsrc/content/LICENSE.txt"

    templates
    |> DirectoryInfo.ofPath
    |> DirectoryInfo.getSubDirectories
    |> Seq.iter (fun dir ->
        let name = dir.Name
        if name.Length = 2 || name.Length = 3 then
            layoutRootsAll.Add(
                    name, [templates  @@ name
                           formatting @@ "templates"
                           formatting @@ "templates/reference" ]))                          
    copyFiles ()

    for dir in  [ content; ] do

        let langSpecificPath(lang, path:string) =
            path.Split([|'/'; '\\'|], System.StringSplitOptions.RemoveEmptyEntries)
            |> Array.exists(fun i -> i = lang)

        let layoutRoots =
            let key = layoutRootsAll.Keys |> Seq.tryFind (fun i -> langSpecificPath(i, dir))
            match key with
            | Some lang -> layoutRootsAll.[lang]
            | None -> layoutRootsAll.["en"]

        let arguments : FSFormatting.LiterateArguments =
            { FSFormatting.defaultLiterateArguments with
                Source = content
                OutputDirectory = output
                LayoutRoots = layoutRoots
                ProjectParameters  = ("root", root)::info
                Template = docTemplate }

        // Have to hack in the -fseEvaluator param to get snippets to work properly
        let layoutroots =
            if arguments.LayoutRoots.IsEmpty then []
            else [ "--layoutRoots" ] @ arguments.LayoutRoots
        let source = arguments.Source
        let template = arguments.Template
        let outputDir = arguments.OutputDirectory

        let command = 
            arguments.ProjectParameters
            |> Seq.collect (fun (k, v) -> [ k; v ])
            |> Seq.append 
                   (["literate"; "--processdirectory" ] 
                    @ layoutroots 
                    @ [ "--inputdirectory"; source; 
                        "--templatefile"; template; 
                        "--outputDirectory"; outputDir; 
                        "--fsieval"; "true";
                        "--replacements" ])
            |> Seq.map (fun s -> 
                   if s.StartsWith "\"" then s
                   else sprintf "\"%s\"" s)
            |> String.separated " "

        let toolPath =
            Tools.findToolInSubPath 
                "fsformatting.exe"
                (Directory.GetCurrentDirectory() @@ "tools" @@ "FSharp.Formatting.CommandTool" @@ "tools")
    
        if 0 <> Process.execSimple ((fun info ->
            { info with
                FileName = toolPath
                Arguments = command }) >> Process.withFramework) System.TimeSpan.MaxValue
        then 
            failwithf "FSharp.Formatting %s failed." command

Target.create "Docs" <| fun _ ->
    createDocs ()

Target.create "WatchDocs" <| fun _ ->    

    use watcher = new FileSystemWatcher(DirectoryInfo("docsrc/content").FullName,"*.fsx")

    watcher.EnableRaisingEvents <- true

    let handler (e: FileSystemEventArgs) =
        (e.ChangeType, e.Name)
        ||> sprintf "Change %O of %s"
        |> Trace.traceImportant
        |> ignore
        
        createDocs()

    watcher.Changed.Add handler
    watcher.Created.Add handler
    watcher.Renamed.Add handler
    watcher.Deleted.Add handler

    Trace.traceImportant "Waiting for help edits. Press any key to stop."

    System.Console.ReadKey() |> ignore

    watcher.EnableRaisingEvents <- false
    watcher.Dispose()


// --------------------------------------------------------------------------------------
// Release Scripts
Target.create "Release" <| fun _ ->
    Git.Staging.stageAll ""
    Git.Commit.exec "" (sprintf "Bump version to %s" release.NugetVersion)
    Git.Branches.push ""

    Git.Branches.tag "" release.NugetVersion
    Git.Branches.pushTag "" "origin" release.NugetVersion


Target.create "BuildPackage" ignore
Target.create "GenerateDocs" ignore
Target.create "All" ignore

"Clean"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "CopyBinaries"
  ==> "RunTests"
  ==> "GenerateDocs"
//  ==> "NuGet"
  ==> "All"

"RunTests" ?=> "CleanDocs"

"CleanDocs"
  ==>"Docs"
  ==> "ReferenceDocs"
  ==> "GenerateDocs"

"Clean"
  ==> "Release"

"BuildPackage"
//  ==> "PublishNuget"
  ==> "Release"

Target.runOrDefault "All"
