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

    // New type for post metadata from YAML front-matter
    [<CLIMutable>]
    type PostMetadata = {
        post_type: string
        title: string
        publish_date: string
        tags: string list
    }

    type Post = {
        Metadata: PostMetadata option
        TextContent: string
        MediaItems: Media list
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
    open Markdig.Renderers
    open Markdig.Renderers.Html

    // ParsedDocument type as specified in refactor plan
    type ParsedDocument = {
        Metadata: Domain.PostMetadata option
        TextContent: string
        MediaItems: Domain.Media list
        RawMarkdown: string
    }

    // DTO for YAML deserialization
    [<CLIMutable>]
    type MediaItemDto = {
        media_type: string
        uri: string
        caption: string
        alt_text: string
        aspect: string
    }
    
    // Parse YAML front-matter
    let parseFrontMatter (content: string) : Domain.PostMetadata option * string =
        let lines = content.Split([|'\n'|], StringSplitOptions.None)
        
        if lines.Length > 0 && lines.[0].Trim() = "---" then
            let endIdx = 
                lines 
                |> Array.skip 1
                |> Array.tryFindIndex (fun line -> line.Trim() = "---")
            
            match endIdx with
            | Some idx ->
                let frontMatterLines = lines.[1..idx]
                let frontMatterYaml = String.concat "\n" frontMatterLines
                let remainingContent = 
                    lines.[(idx + 2)..]
                    |> String.concat "\n"
                
                try
                    let deserializer = 
                        DeserializerBuilder()
                            .WithNamingConvention(UnderscoredNamingConvention.Instance)
                            .Build()
                    
                    let metadata = deserializer.Deserialize<Domain.PostMetadata>(frontMatterYaml)
                    (Some metadata, remainingContent)
                with
                | ex -> 
                    printfn "Failed to parse front-matter YAML: %s" ex.Message
                    (None, content)
            | None ->
                (None, content)
        else
            (None, content)
    
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

    // AST-based text extraction to replace string manipulation
    let extractTextContentFromAst (doc: MarkdownDocument) : string =
        // Create a new writer and renderer for HTML output
        let writer = new System.IO.StringWriter()
        let renderer = new HtmlRenderer(writer)
        
        // Create a visitor that will filter out MediaBlocks
        let filteredWriter = new System.IO.StringWriter()
        let filteredRenderer = new HtmlRenderer(filteredWriter)
        
        // Iterate through blocks and render only non-MediaBlocks
        for block in doc do
            if not (block :? MediaBlock) then
                filteredRenderer.Render(block) |> ignore
        
        filteredWriter.ToString()

    // AST-based media extraction to centralize media parsing
    let extractMediaFromAst (doc: MarkdownDocument) : Domain.Media list =
        doc.Descendants<MediaBlock>()
        |> Seq.collect (fun block -> block.MediaItems)
        |> Seq.toList

    // Centralized parsing function as single entry point
    let parseDocument (pipeline: MarkdownPipeline) (content: string) : ParsedDocument =
        let (metadata, contentWithoutFrontMatter) = parseFrontMatter content
        let doc = Markdown.Parse(contentWithoutFrontMatter, pipeline)
        
        {
            Metadata = metadata
            TextContent = extractTextContentFromAst doc
            MediaItems = extractMediaFromAst doc
            RawMarkdown = content
        }

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

    let renderLayout (postContent: XmlNode option) (mediaGallery: XmlNode) (metadata: Domain.PostMetadata option) =
        html [] [
            head [] [
                link [ _rel "stylesheet"; _href "main.css" ]
            ]
            body [] [
                div [ _class "feed-container" ] [
                    article [_class "post-card"] [
                        match metadata with
                        | Some meta ->
                            div [ _class "post-header" ] [
                                h1 [ _class "post-title" ] [ str meta.title ]
                                div [ _class "post-meta" ] [
                                    time [ _class "post-date"; _datetime meta.publish_date ] [ str meta.publish_date ]
                                    div [ _class "post-tags" ] [
                                        for tag in meta.tags do
                                            span [ _class "tag" ] [ str tag ]
                                    ]
                                ]
                            ]
                        | None -> ()
                        
                        match postContent with
                        | Some content -> 
                            div [ _class "post-content" ] [
                                content
                                mediaGallery
                            ]
                        | None -> mediaGallery
                    ]
                ]
            ]
        ]

    let private renderMediaItem (item: Media) =
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
        
        // Audio elements should not have aspect ratio constraints
        let containerAttributes = 
            match item.MediaType with
            | Audio _ -> [ _class "media-item audio-item" ]
            | _ -> [ 
                _class "media-item"
                _style $"aspect-ratio: {formatCssAspectRatio item.AspectRatio}"
            ]
        
        div containerAttributes (mediaElement :: captionElement)

    let renderMediaGallery (mediaItems: Media list) =
        div [ _class "media-gallery" ] (
            mediaItems |> List.map renderMediaItem
        )

    // HTML renderer class
    type MediaRenderer() =
        inherit HtmlObjectRenderer<MediaBlock>()
        
        override _.Write(renderer: HtmlRenderer, mediaBlock: MediaBlock) =
            let mediaGallery = renderMediaGallery mediaBlock.MediaItems
            let html = RenderView.AsString.htmlNode (renderLayout None mediaGallery None)
            renderer.Write(html: string) |> ignore

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
open Giraffe.ViewEngine

let pipeline = 
    MarkdownPipelineBuilder()
        .Use<MediaExtension>()
        .Build()

// Add ContentProcessor implementation now that pipeline is available
module ContentProcessor =
    
    open Giraffe.ViewEngine
    open Domain
    
    type ProcessedPost = {
        Document: MarkdownParser.ParsedDocument
        TextHtml: string option
        MediaGallery: XmlNode
        Header: XmlNode option
        PostTitle: string option
    }
    
    let processPost (markdownContent: string) : ProcessedPost =
        // Parse the document using Phase 1's centralized parser
        let parsedDoc = MarkdownParser.parseDocument pipeline markdownContent
        
        // Process text content - convert to HTML if present
        let textHtml = 
            if String.IsNullOrWhiteSpace(parsedDoc.TextContent) then
                None
            else 
                Some parsedDoc.TextContent
        
        // Generate media gallery using existing renderer
        let mediaGallery = MediaRenderer.renderMediaGallery parsedDoc.MediaItems
        
        // Process header information
        let headerNode = 
            match parsedDoc.Metadata with
            | Some meta ->
                Some (div [ _class "post-header" ] [
                    h1 [ _class "post-title" ] [ str meta.title ]
                    div [ _class "post-meta" ] [
                        time [ _class "post-date"; _datetime meta.publish_date ] [ str meta.publish_date ]
                        div [ _class "post-tags" ] [
                            for tag in meta.tags do
                                span [ _class "tag" ] [ str tag ]
                        ]
                    ]
                ])
            | None -> None
        
        // Extract post title for easy access
        let postTitle = 
            match parsedDoc.Metadata with
            | Some meta -> Some meta.title
            | None -> None
        
        {
            Document = parsedDoc
            TextHtml = textHtml
            MediaGallery = mediaGallery
            Header = headerNode
            PostTitle = postTitle
        }

// Enhanced rendering function that takes ProcessedPost
let renderProcessedPost (processedPost: ContentProcessor.ProcessedPost) =
    // Create text content node if present
    let textContentNode = 
        match processedPost.TextHtml with
        | Some html -> Some (div [ _class "post-text" ] [ rawText html ])
        | None -> None
    
    // Create content nodes list
    let contentNodes = [
        match textContentNode with
        | Some node -> yield node
        | None -> ()
        yield processedPost.MediaGallery
    ]
    
    // Generate complete HTML page using Giraffe ViewEngine
    let fullPage = html [] [
        head [] [
            link [ _rel "stylesheet"; _href "main.css" ]
        ]
        body [] [
            div [ _class "feed-container" ] [
                article [ _class "post-card" ] [
                    match processedPost.Header with
                    | Some header -> yield header
                    | None -> ()
                    yield div [ _class "post-content" ] contentNodes
                ]
            ]
        ]
    ]
    
    RenderView.AsString.htmlDocument fullPage

let extractMediaFromMarkdown (markdownContent: string) =
    // Use new centralized parser instead of duplicating parsing logic
    let parsedDoc = MarkdownParser.parseDocument pipeline markdownContent
    parsedDoc.MediaItems

let extractTextContent (markdownContentWithoutFrontMatter: string) =
    // Use AST-based extraction instead of string manipulation
    let doc = Markdown.Parse(markdownContentWithoutFrontMatter, pipeline)
    MarkdownParser.extractTextContentFromAst doc

let generatePostHtml (markdownContent: string) =
    // Phase 2: Use Process → Render workflow
    let processedPost = ContentProcessor.processPost markdownContent
    renderProcessedPost processedPost

module PostGenerator =
    
    type PostConfig = {
        SourceFile: string
        OutputFile: string
        PostType: string
    }
    
    let generatePost (config: PostConfig) : Result<string, string> =
        try
            // Read source file
            let markdownContent = File.ReadAllText(config.SourceFile)
            
            // Generate HTML using existing pipeline
            let html = generatePostHtml markdownContent
            
            // Write output file
            File.WriteAllText(config.OutputFile, html)
            
            // Log success
            printfn "Generated %s post: %s -> %s" config.PostType config.SourceFile config.OutputFile
            printfn "Generated HTML:"
            printfn "%s" html
            
            Ok html
        with
        | ex -> Error $"Failed to generate {config.PostType} post: {ex.Message}"
    
    let generateAllPosts (configs: PostConfig list) : unit =
        configs
        |> List.iter (fun config ->
            match generatePost config with
            | Ok _ -> ()
            | Error errorMsg -> printfn "Error: %s" errorMsg
        )

// Configuration-driven post generation
let postConfigs = [
    { PostGenerator.SourceFile = Path.Combine("_src", "image.md"); PostGenerator.OutputFile = Path.Combine("_public", "image.html"); PostGenerator.PostType = "image" }
    { PostGenerator.SourceFile = Path.Combine("_src", "video.md"); PostGenerator.OutputFile = Path.Combine("_public", "video.html"); PostGenerator.PostType = "video" }
    { PostGenerator.SourceFile = Path.Combine("_src", "audio.md"); PostGenerator.OutputFile = Path.Combine("_public", "audio.html"); PostGenerator.PostType = "audio" }
    { PostGenerator.SourceFile = Path.Combine("_src", "mixed.md"); PostGenerator.OutputFile = Path.Combine("_public", "mixed.html"); PostGenerator.PostType = "mixed" }
]

// Generate all posts using the new PostGenerator module
PostGenerator.generateAllPosts postConfigs