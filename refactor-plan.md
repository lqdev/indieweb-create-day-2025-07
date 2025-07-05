# Refactoring Plan for script.fsx

## Current Issues Identified

### 1. **Markdown Processing Outside MarkdownParser Module**
- `extractTextContent` function manually removes media blocks using string manipulation
- `extractMediaFromMarkdown` duplicates markdown parsing logic outside the parser module
- `generatePostHtml` performs HTML conversion using raw `Markdown.ToHtml()`
- YAML front-matter processing could be more robust

### 2. **Separation of Concerns**
- Business logic mixed with presentation logic
- File I/O operations mixed with content processing
- HTML generation scattered across multiple functions

### 3. **Code Duplication**
- Four nearly identical `generate*Post()` functions
- Repeated file reading and writing patterns
- Similar HTML structure generation in multiple places

### 4. **Type Safety and Error Handling**
- Limited error handling for file operations
- String-based media type handling could be more type-safe
- No validation for required fields

## Proposed Refactoring Structure

### Phase 1: Consolidate Markdown Processing

#### 1.1 Enhance MarkdownParser Module
- [ ] Move all markdown-related parsing here
- [ ] Add ParsedDocument type
- [ ] Create centralized parseDocument function
- [ ] Implement extractTextContentFromAst function
- [ ] Implement extractMediaFromAst function
```fsharp
module MarkdownParser =
    // Move all markdown-related parsing here
    
    type ParsedDocument = {
        Metadata: PostMetadata option
        TextContent: string
        MediaItems: Media list
        RawMarkdown: string
    }
    
    // Centralized parsing function that handles everything
    let parseDocument (content: string) : ParsedDocument
    
    // Remove media blocks using Markdig AST instead of string manipulation
    let extractTextContentFromAst (doc: MarkdownDocument) : string
    
    // Extract media using proper AST traversal
    let extractMediaFromAst (doc: MarkdownDocument) : Media list
```

#### 1.2 Create Proper AST-based Text Extraction
Replace string-based `extractTextContent` with AST traversal:
- [ ] Use Markdig's syntax tree to properly exclude MediaBlock nodes
- [ ] Preserve markdown structure while removing media blocks
- [ ] Convert remaining AST back to HTML using proper Markdig pipeline

### Phase 2: Separate Content Processing from Presentation

#### 2.1 Create ContentProcessor Module
- [ ] Create ProcessedPost type
- [ ] Implement processPost function
- [ ] Separate business logic from presentation
```fsharp
module ContentProcessor =
    
    type ProcessedPost = {
        Document: MarkdownParser.ParsedDocument
        TextHtml: string option
        MediaGallery: XmlNode
    }
    
    let processPost (markdownContent: string) : ProcessedPost
```

#### 2.2 Enhance MediaRenderer Module
- [ ] Move all HTML layout logic here
- [ ] Create composable rendering functions
- [ ] Separate media rendering from layout rendering

### Phase 3: Create Post Generator Module

#### 3.1 Centralize Post Generation
- [ ] Create PostConfig type
- [ ] Implement generatePost function with Result type
- [ ] Implement generateAllPosts function
```fsharp
module PostGenerator =
    
    type PostConfig = {
        SourceFile: string
        OutputFile: string
        PostType: string
    }
    
    let generatePost (config: PostConfig) : Result<string, string>
    
    let generateAllPosts (configs: PostConfig list) : unit
```

#### 3.2 Add Configuration-driven Generation
- [ ] Replace hardcoded file names with configuration
- [ ] Add error handling and logging
- [ ] Support batch processing

### Phase 4: Improve Type Safety and Error Handling

#### 4.1 Result Types for Error Handling
- [ ] Define ParseError union type
- [ ] Define GenerationError union type
- [ ] Update functions to return Result types
```fsharp
type ParseError = 
    | YamlParseError of string
    | MediaParseError of string
    | FileNotFound of string

type GenerationError =
    | ParseError of ParseError
    | RenderError of string
    | FileWriteError of string
```

#### 4.2 Validation Module
- [ ] Implement validateMetadata function
- [ ] Implement validateMediaItem function
- [ ] Implement validatePost function
```fsharp
module Validation =
    let validateMetadata (metadata: PostMetadata option) : Result<PostMetadata, string>
    let validateMediaItem (item: Media) : Result<Media, string>
    let validatePost (post: ParsedDocument) : Result<ParsedDocument, string list>
```

## Implementation Priority

### High Priority (Phase 1)
1. **Move `extractTextContent` logic to MarkdownParser**
   - [ ] Replace string manipulation with AST traversal
   - [ ] Use MediaBlock detection from the parser itself
   - [ ] Ensure consistent parsing pipeline usage

2. **Consolidate markdown parsing in MarkdownParser.parseDocument**
   - [ ] Single entry point for all markdown processing
   - [ ] Return structured data containing all parsed elements
   - [ ] Remove duplicate parsing calls

3. **Fix MediaBlock AST integration**
   - [ ] Ensure MediaBlocks are properly excluded from text content AST
   - [ ] Use Markdig's visitor pattern for clean AST traversal

### Medium Priority (Phase 2)
4. **Separate ContentProcessor module**
   - [ ] Clean separation between parsing and processing
   - [ ] Testable business logic

5. **Refactor MediaRenderer for better composition**
   - [ ] Separate layout from media rendering
   - [ ] Make rendering functions more modular

### Low Priority (Phase 3-4)
6. **Create PostGenerator module**
   - [ ] Eliminate code duplication in generate functions
   - [ ] Add configuration support

7. **Add comprehensive error handling**
   - [ ] Result types throughout the pipeline
   - [ ] Proper error propagation and reporting

## Specific Code Changes Needed

### 1. Replace String-based Media Block Removal
**Current problematic code:**
```fsharp
let extractTextContent (markdownContentWithoutFrontMatter: string) =
    let lines = markdownContentWithoutFrontMatter.Split([|'\n'|])
    // Manual string parsing to remove :::media blocks
```

**Proposed solution:**
```fsharp
// In MarkdownParser module
let extractTextContentFromDocument (doc: MarkdownDocument) =
    // Create a new document excluding MediaBlock nodes
    let filteredBlocks = 
        doc.Descendants()
        |> Seq.filter (fun block -> not (block :? MediaBlock))
        |> Seq.toArray
    
    // Render the filtered AST back to HTML
    let writer = new System.IO.StringWriter()
    let renderer = new HtmlRenderer(writer)
    renderer.Render(doc) // Render only non-MediaBlock content
    writer.ToString()
```

### 2. Centralize Parsing Logic
**Move to MarkdownParser:**
```fsharp
let parseDocument (content: string) : ParsedDocument =
    let (metadata, contentWithoutFrontMatter) = parseFrontMatter content
    let doc = Markdown.Parse(contentWithoutFrontMatter, pipeline)
    
    {
        Metadata = metadata
        TextContent = extractTextContentFromDocument doc
        MediaItems = extractMediaFromDocument doc
        RawMarkdown = content
    }
```

### 3. Eliminate Generate Function Duplication
**Replace four functions with:**
```fsharp
let postConfigs = [
    { SourceFile = "_src/image.md"; OutputFile = "_public/image.html"; PostType = "image" }
    { SourceFile = "_src/video.md"; OutputFile = "_public/video.html"; PostType = "video" }
    { SourceFile = "_src/audio.md"; OutputFile = "_public/audio.html"; PostType = "audio" }
    { SourceFile = "_src/mixed.md"; OutputFile = "_public/mixed.html"; PostType = "mixed" }
]

PostGenerator.generateAllPosts postConfigs
```

## Benefits of This Refactoring

1. **Consistency**: All markdown processing goes through the same pipeline
2. **Maintainability**: Clear separation of concerns and single responsibility
3. **Testability**: Each module can be tested independently
4. **Type Safety**: Better error handling and validation
5. **Extensibility**: Easy to add new post types or processing steps
6. **Performance**: More efficient AST-based processing vs string manipulation

## Migration Strategy

- [ ] Start with Phase 1 changes to fix the core architectural issues
- [ ] Implement changes incrementally, ensuring tests pass at each step
- [ ] Keep the existing functions as wrappers initially, then remove them
- [ ] Add comprehensive tests for the new modules
- [ ] Gradually add error handling and validation

This refactoring will result in a more robust, maintainable, and extensible markdown processing system while preserving all existing functionality.
