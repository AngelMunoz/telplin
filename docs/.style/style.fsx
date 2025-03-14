﻿#r "nuget: JavaScriptEngineSwitcher.V8.Native.win-x64"
#r "nuget: JavaScriptEngineSwitcher.V8.Native.linux-x64"
#r "nuget: JavaScriptEngineSwitcher.V8.Native.osx-x64"
#r "nuget: JavaScriptEngineSwitcher.V8, 3.20.2"
#r "nuget: DartSassHost, 1.0.0-preview8"
#r "nuget: FSharp.Control.Reactive, 5.*"

open System
open System.IO
open DartSassHost
open DartSassHost.Helpers
open JavaScriptEngineSwitcher.V8
open FSharp.Control.Reactive

let sassCompiler = new SassCompiler (new V8JsEngineFactory ())
let (</>) a b = Path.Combine (a, b)
// let inputFileToolpage = __SOURCE_DIRECTORY__ </> "online-tool.sass"
let inputFileTemplate = __SOURCE_DIRECTORY__ </> "template.sass"
let inputFolder = __SOURCE_DIRECTORY__
// let outputToolPage = __SOURCE_DIRECTORY__ </> ".." </> ".tool" </> "online-tool.css"
let outputTemplate = __SOURCE_DIRECTORY__ </> "../assets/" </> "template.css"

let compileSass () =
    try
        // let homepage =
        //     sassCompiler.CompileFile (inputFileToolpage, ?outputPath = Some outputToolPage)

        let template =
            sassCompiler.CompileFile (inputFileTemplate, ?outputPath = Some outputTemplate)

        // File.WriteAllText (outputToolPage, homepage.CompiledContent)
        // printfn "Compiled %s at %A" outputToolPage DateTime.Now

        File.WriteAllText (outputTemplate, template.CompiledContent)
        printfn "Compiled %s at %A" outputTemplate DateTime.Now

    with
    | :? SassCompilerLoadException as sclex ->
        printfn
            "During loading of Sass compiler an error occurred. See details:\n%s"
            (SassErrorHelpers.GenerateErrorDetails sclex)
    | :? SassCompilationException as sce ->
        printfn
            "During compilation of SCSS code an error occurred. See details:\n%s"
            (SassErrorHelpers.GenerateErrorDetails sce)
    | :? SassException as e ->
        printfn
            "During working of Sass compiler an unknown error occurred. See details:\n%s"
            (SassErrorHelpers.GenerateErrorDetails e)
    | ex -> printfn "Unexpected exception during Sass compilation: %A" ex

let isWatch =
    match Seq.tryLast fsi.CommandLineArgs with
    | Some "--watch" -> true
    | _ -> false

if isWatch then
    let fsw = new FileSystemWatcher (inputFolder)
    fsw.IncludeSubdirectories <- true
    fsw.Filters.Add "*.sass"
    fsw.NotifyFilter <- NotifyFilters.FileName ||| NotifyFilters.Size
    fsw.EnableRaisingEvents <- true

    let mapEvent ev = Observable.map (fun _ -> ()) ev

    let subscriber =
        [|
            mapEvent fsw.Renamed
            mapEvent fsw.Changed
            mapEvent fsw.Deleted
            mapEvent fsw.Created
        |]
        |> Observable.mergeArray
        |> Observable.startWith [| () |]
        |> Observable.throttle (TimeSpan.FromMilliseconds 200.)
        |> Observable.subscribe compileSass

    let _ = Console.ReadLine ()
    subscriber.Dispose ()

    printfn "Goodbye"
    exit 0
else
    compileSass ()
    exit 0
