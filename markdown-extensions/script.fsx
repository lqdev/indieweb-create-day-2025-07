#r "nuget: Markdig, 0.41.3"
#r "nuget: Giraffe.ViewEngine, 2.0.0-alpha-1"
#r "nuget: YamlDotNet, 16.3.0"

open System
open Markdig
open Markdig.Syntax
open Markdig.Parsers

module Domain = 

    type MediaType =
        | Image of string
        | Video of string
        | Audio of string

    with
        override this.ToString() =
            match this with
            | Image s -> s
            | Video s -> s
            | Audio s -> s
        static member ToMediaType (mediaType: string) =
            match mediaType with
            | "image" -> Image mediaType
            | "video" -> Video mediaType
            | "audio" -> Audio mediaType
            | _ -> failwith "Unknown media type"

    type AspectRatio = 
        | Square of string
        | Wide of string
        | Tall of string
        | Custom of string
    with 
        override this.ToString() =
            match this with
            | Square s -> s
            | Wide s -> s
            | Tall s -> s
            | Custom s -> s
        static member ToAspectRatio (aspect: string) =
            match aspect with
            | "1:1" -> Square "1:1"
            | "3:2" -> Wide "3:2"
            | "9:16" -> Tall "9:16"
            | _ -> Custom aspect

    type Media = {
        MediaType: MediaType
        Uri: string
        AltText: string
        Caption: string option
        AspectRatio: AspectRatio
    }


open Domain

// Custom block for media gallery - moved outside Domain since it's Markdig-specific
type MediaBlock(parser: BlockParser) =
    inherit ContainerBlock(parser)
    member val MediaItems: Domain.Media list = [] with get, set
    member val RawContent: string = "" with get, set

module MarkdownParser = 
    
    open YamlDotNet.Serialization
    open YamlDotNet.Serialization.NamingConventions

    // DTO for YAML deserialization
    [<CLIMutable>]
    type MediaItemDto = {
        media_type: string
        uri: string
        caption: string
        alt_text: string
        aspect: string
    }
    
    // Parse the YAML-like content inside the media block
    let parseMediaItems (content: string) =
        try
            // Fix indentation for YAML parsing
            let lines = content.Split([|'\n'|], StringSplitOptions.None)
            let fixedLines = 
                lines
                |> Array.filter (fun line -> not (String.IsNullOrWhiteSpace(line)))
                |> Array.map (fun line ->
                    let trimmed = line.Trim()
                    if trimmed.StartsWith("- ") then
                        trimmed  // Keep list items at the beginning
                    elif trimmed.Contains(":") && not (trimmed.StartsWith("- ")) then
                        "  " + trimmed  // Indent properties with 2 spaces
                    else
                        trimmed
                )
            
            let cleanContent = String.concat "\n" fixedLines

            let deserializer = 
                DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build()
            
            let yamlItems = deserializer.Deserialize<MediaItemDto list>(cleanContent)
            
            yamlItems
            |> List.map (fun dto ->
                let mediaType = 
                    match dto.media_type with
                    | null | "" -> MediaType.ToMediaType "image"
                    | mt -> MediaType.ToMediaType mt
                
                let uri = 
                    match dto.uri with
                    | null -> ""
                    | u -> u
                
                let altText = 
                    match dto.alt_text with
                    | null -> ""
                    | alt -> alt
                
                let caption = 
                    match dto.caption with
                    | null | "" -> None
                    | c when String.IsNullOrWhiteSpace(c) -> None
                    | c -> Some c
                
                let aspectRatio = 
                    match dto.aspect with
                    | null | "" -> AspectRatio.ToAspectRatio "1:1"
                    | a -> AspectRatio.ToAspectRatio a
                
                { 
                    MediaType = mediaType
                    Uri = uri
                    AltText = altText
                    Caption = caption
                    AspectRatio = aspectRatio
                })
        with
        | ex -> 
            // Fallback to empty list if YAML parsing fails
            // In production, you might want to log this error
            printfn "Failed to parse YAML: %s" ex.Message
            printfn "Content: %s" content    
            []

    // Block parser class
    type MediaBlockParser() =
        inherit BlockParser()
        
        override this.TryOpen(processor) =
            if processor.Line.ToString().TrimStart().StartsWith(":::media") then
                let mediaBlock = MediaBlock(this)  // Pass parser to constructor
                processor.NewBlocks.Push(mediaBlock)
                BlockState.ContinueDiscard
            else
                BlockState.None
                
        override _.TryContinue(processor, block) =
            let line = processor.Line
            let lineText = line.ToString().TrimStart()
            
            if lineText = ":::media" then
                // End of media block
                let mediaBlock = block :?> MediaBlock
                mediaBlock.MediaItems <- parseMediaItems mediaBlock.RawContent
                processor.Close(block)
                BlockState.BreakDiscard
            elif lineText.StartsWith(":::") then
                // Different fence type, close this block
                processor.Close(block)
                BlockState.Break
            else
                // Continue collecting content - preserve original indentation
                let mediaBlock = block :?> MediaBlock
                let originalLine = processor.Line.ToString()
                mediaBlock.RawContent <- mediaBlock.RawContent + originalLine + "\n"
                BlockState.Continue
                
        override _.Close(processor, block) =
            let mediaBlock = block :?> MediaBlock
            if mediaBlock.MediaItems.IsEmpty then
                mediaBlock.MediaItems <- parseMediaItems mediaBlock.RawContent
            true

module MediaRenderer =
    
    open Giraffe.ViewEngine
    open Markdig.Renderers
    open Markdig.Renderers.Html

    let private formatCssAspectRatio (aspect: AspectRatio) =

        let ratioParts (ratio:string) =
            let parts = ratio.Split(":")
            if parts.Length = 2 then
                $"{parts.[0].Trim()} / {parts.[1].Trim()}"
            else
                "auto"

        match aspect with
        | Square x -> ratioParts (x.ToString())
        | Wide x -> ratioParts (x.ToString())
        | Tall x -> ratioParts (x.ToString())
        | Custom ratio -> ratioParts (ratio.ToString())

    let private renderMediaItem (item: Domain.Media) =
        let mediaElement = 
            match item.MediaType with
            | Image _ ->
                img [ _src item.Uri; _alt item.AltText ]
            | Video _ ->
                video [ _controls ] [
                    source [ _src item.Uri ]
                ]
            | Audio _ ->
                audio [ _controls ] [
                    source [ _src item.Uri ]
                ]
        
        let captionElement = 
            match item.Caption with
            | Some caption -> [ p [ _class "media-caption" ] [ str caption ] ]
            | None -> []
        
        div [ 
            _class "media-item"
            _style $"aspect-ratio: {formatCssAspectRatio item.AspectRatio}"
        ] (mediaElement :: captionElement)
    
    let private renderMediaGallery (mediaItems: Domain.Media list) =
        div [ _class "media-gallery" ] (
            mediaItems |> List.map renderMediaItem
        )
    
    // HTML renderer class
    type MediaRenderer() =
        inherit HtmlObjectRenderer<MediaBlock>()
        
        override _.Write(renderer: HtmlRenderer, mediaBlock: MediaBlock) =
            let viewEngine = renderMediaGallery mediaBlock.MediaItems
            let html = RenderView.AsString.htmlNode viewEngine
            renderer.Write(html) |> ignore

module MediaExtension =
    
    open MarkdownParser
    open MediaRenderer

    // Extension class
    type MediaExtension() =
        interface IMarkdownExtension with
            member _.Setup(pipeline) =
                let parser = MediaBlockParser()
                pipeline.BlockParsers.Insert(0, parser)
                
            member _.Setup(pipeline, renderer) =
                let htmlRenderer = MediaRenderer()
                renderer.ObjectRenderers.Add(htmlRenderer)


open System.IO
open MediaExtension

let pipeline = 
    MarkdownPipelineBuilder()
        .Use<MediaExtension>()
        .Build()

let md = File.ReadAllText(Path.Combine("_src","image.md"))

let html = Markdown.ToHtml(md, pipeline)

printfn "Generated HTML:"
printfn "%s" html

let outputPath = Path.Combine("_public", "image.html")
File.WriteAllText(outputPath, html)