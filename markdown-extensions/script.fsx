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
open MediaExtension
open Giraffe.ViewEngine

let pipeline = 
    MarkdownPipelineBuilder()
        .Use<MediaExtension>()
        .Build()

let extractMediaFromMarkdown (markdownContent: string) =
    // Parse the markdown to extract the media content
    let doc = Markdown.Parse(markdownContent, pipeline)
    let mediaBlocks = 
        doc.Descendants<MediaBlock>()
        |> Seq.toList
    
    // Get all media items from all media blocks
    let allMediaItems = 
        mediaBlocks
        |> List.collect (fun block -> block.MediaItems)
    
    allMediaItems

let extractTextContent (markdownContentWithoutFrontMatter: string) =
    // Remove YAML frontmatter (already removed by parseFrontMatter)
    let lines = markdownContentWithoutFrontMatter.Split([|'\n'|], StringSplitOptions.None)
    
    // Remove media blocks
    let filteredLines = 
        lines
        |> Array.fold (fun (acc, inMediaBlock) line ->
            let trimmed = line.Trim()
            if trimmed = ":::media" then
                (acc, not inMediaBlock)
            elif inMediaBlock then
                (acc, inMediaBlock)
            else
                (line :: acc, inMediaBlock)
        ) ([], false)
        |> fst
        |> List.rev
        |> List.filter (fun line -> not (String.IsNullOrWhiteSpace(line)))
    
    String.concat "\n" filteredLines

let generatePostHtml (markdownContent: string) =
    // Parse front-matter first
    let (metadata, contentWithoutFrontMatter) = MarkdownParser.parseFrontMatter markdownContent
    
    let textContent = extractTextContent contentWithoutFrontMatter
    let mediaItems = extractMediaFromMarkdown contentWithoutFrontMatter
    
    // Create HTML structure manually to avoid ViewEngine complications
    let textContentHtml = 
        if String.IsNullOrWhiteSpace(textContent) then ""
        else 
            let htmlString = Markdown.ToHtml(textContent)
            $"""<div class="post-text">{htmlString}</div>"""
    
    // Generate the media gallery HTML
    let mediaGallery = MediaRenderer.renderMediaGallery mediaItems
    let mediaHtml = RenderView.AsString.htmlNode mediaGallery
    
    // Generate post header HTML from metadata
    let headerHtml = 
        match metadata with
        | Some meta ->
            let tagsHtml = 
                meta.tags
                |> List.map (fun tag -> $"""<span class="tag">{tag}</span>""")
                |> String.concat ""
            
            $"""<div class="post-header">
                <h1 class="post-title">{meta.title}</h1>
                <div class="post-meta">
                    <time class="post-date" datetime="{meta.publish_date}">{meta.publish_date}</time>
                    <div class="post-tags">{tagsHtml}</div>
                </div>
            </div>"""
        | None -> ""
    
    // Generate the complete HTML structure
    let fullHtml = $"""
<!DOCTYPE html>
<html>
<head>
    <link rel="stylesheet" href="main.css">
</head>
<body>
    <div class="feed-container">
        <article class="post-card">
            {headerHtml}
            <div class="post-content">
                {textContentHtml}
                {mediaHtml}
            </div>
        </article>
    </div>
</body>
</html>"""
    
    fullHtml

let generateImagePost () = 
    let md = File.ReadAllText(Path.Combine("_src","image.md"))
    let html = generatePostHtml md

    printfn "Generated HTML:"
    printfn "%s" html

    let outputPath = Path.Combine("_public", "image.html")
    File.WriteAllText(outputPath, html)

let generateVideoPost () = 
    let md = File.ReadAllText(Path.Combine("_src","video.md"))
    let html = generatePostHtml md

    printfn "Generated HTML:"
    printfn "%s" html

    let outputPath = Path.Combine("_public", "video.html")
    File.WriteAllText(outputPath, html)

let generateAudioPost () = 
    let md = File.ReadAllText(Path.Combine("_src","audio.md"))
    let html = generatePostHtml md

    printfn "Generated HTML:"
    printfn "%s" html

    let outputPath = Path.Combine("_public", "audio.html")
    File.WriteAllText(outputPath, html)

let generateMixedPost () = 
    let md = File.ReadAllText(Path.Combine("_src","mixed.md"))
    let html = generatePostHtml md

    printfn "Generated HTML:"
    printfn "%s" html

    let outputPath = Path.Combine("_public", "mixed.html")
    File.WriteAllText(outputPath, html)

generateImagePost ()
generateVideoPost ()
generateAudioPost ()
generateMixedPost ()