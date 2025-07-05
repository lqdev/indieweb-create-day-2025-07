# Copilot Instructions for F# Refactoring Workflow

## Overview

This document outlines the systematic refactoring workflow established for the indieweb markdown processing system. It captures the methodology used successfully in Phase 1 and Phase 2 implementations and provides a repeatable process for future phases.

**Key Achievement**: Successfully refactored from monolithic functions to clean separation of concerns (Parse → Process → Render) while preserving 100% functionality.

## Refactoring Methodology

### Core Principles

1. **Work Out Loud**: Document every step in `log.md` with verbose explanations
2. **Incremental Implementation**: Make changes in small, testable steps
3. **Preserve Functionality**: Ensure existing behavior is maintained throughout
4. **Single Phase Focus**: Complete one phase entirely before proceeding to the next
5. **Evidence-Based Planning**: Update plans based on actual implementation findings
6. **Module Dependencies**: Always consider F# module definition order and dependencies

### Workflow Steps

#### Phase Preparation

1. **Read and Analyze Current State**
   - Examine the target file (`script.fsx`) completely 
   - Use `read_file` with large chunks (100+ lines) to minimize tool calls
   - Identify specific line numbers and code patterns mentioned in refactor plan
   - Document findings in `log.md` with specific references
   - **Lesson Learned**: Large file reads are more efficient than multiple small reads

2. **Document Phase Objectives**
   - Copy relevant phase goals from `refactor-plan.md`
   - Break down into specific, measurable tasks
   - Identify potential risks or edge cases
   - Log the implementation plan step-by-step

#### Implementation Process

3. **Step-by-Step Implementation**
   - Make one logical change at a time
   - Document each change in `log.md` before making it
   - Use appropriate edit tools (`replace_string_in_file`, `insert_edit_into_file`)
   - Include 3-5 lines of context when using `replace_string_in_file`
   - **Critical**: Plan module definition order before implementing

4. **Immediate Testing**
   - Test functionality after each significant change
   - Use `run_in_terminal` to execute the script after every major edit
   - Document test results in `log.md`
   - Fix errors immediately before proceeding
   - **Best Practice**: Compile and test continuously, not just at end

5. **Error Handling and Learning**
   - When errors occur, document the problem and solution
   - Update approach based on technical constraints discovered
   - Example: Block ownership issues in Markdig AST manipulation
   - **New**: Module dependency resolution strategies

#### Phase Completion

6. **Comprehensive Testing**
   - Test all existing functionality works correctly
   - Verify all post types generate properly
   - Check output files are created correctly

7. **Impact Analysis and Documentation**
   - Document architectural improvements achieved
   - Measure code quality improvements (lines reduced, complexity, etc.)
   - Identify performance benefits
   - Note any edge cases discovered

8. **Plan Updates**
   - Update `refactor-plan.md` with checkmarks for completed items
   - Document any deviations from original plan
   - Note lessons learned that affect future phases
   - Update migration strategy if needed

## F# Specific Guidelines

### Module Organization and Dependencies ⭐ **Critical Learning**

- **Module Definition Order**: F# modules must be defined before they are referenced
- **Dependency Planning**: Map out module dependencies before implementation
  - Example: `ContentProcessor` depends on `MediaRenderer.renderMediaGallery`
  - Solution: Define ContentProcessor type first, implement functions after dependencies
- **Circular Dependency Avoidance**: Structure modules to avoid circular references
- **Best Practice**: Use type-only module definitions when functions need later implementation

### AST Manipulation Best Practices

- **Avoid Block Movement**: Don't try to move Markdig blocks between documents due to ownership constraints
- **Individual Block Rendering**: Render blocks separately instead of creating new documents
- **Pipeline Consistency**: Always use the same Markdig pipeline instance
- **AST Traversal**: Use Markdig's `Descendants<T>()` for type-safe node extraction

### Module Responsibility Patterns ⭐ **Phase 2 Learning**

- **Single Responsibility**: Each module should handle one concern (parsing, rendering, generation)
- **Type-First Design**: Define types before functions, use them to drive API design
- **Centralized Entry Points**: Create single functions that handle complete workflows
- **Data Flow Architecture**: Implement clean `Parse → Process → Render` pipelines

### Common Implementation Patterns

#### Module Structure Pattern
```fsharp
// 1. Type definitions first
module ContentProcessor =
    type ProcessedPost = { ... }

// 2. Dependencies defined
module MediaRenderer = 
    let renderMediaGallery items = ...

// 3. Functions implemented after dependencies available
module ContentProcessor =
    let processPost content = ...
```

#### Function Sizing Guidelines
- **Before**: 48-line `generatePostHtml` with mixed concerns
- **After**: 3-line function using clean pipeline
- **Rule**: If function >20 lines, consider separation of concerns

### Error Handling Evolution

- **Phase 1**: Focus on functionality, basic error handling
- **Phase 2**: Clean architecture with proper separation
- **Later Phases**: Introduce Result types and comprehensive validation
- **Incremental Improvement**: Don't over-engineer early phases

## Documentation Standards

### Log.md Structure

```markdown
# Refactoring Log - Phase X Implementation

## Starting Phase X: [Phase Name]

### Current Architecture Analysis
[Detailed analysis of existing code]

### Phase X Implementation Plan
[Step-by-step plan]

## Step N: [Step Description]
[What you're doing and why]

## Step N Complete: [Achievement Summary]
[What was accomplished, code metrics, issues fixed]

## Phase X Final Summary
[Complete analysis of achievements, metrics, architecture impact]
```

### Commit Message Standards

- Use clear, descriptive commit messages
- Reference phase and step when relevant
- Include file changes and their purpose

## Phase Transition Protocol

### Before Moving to Next Phase

1. **Complete Current Phase Documentation**
   - Finalize log entries with metrics and analysis
   - Update refactor-plan.md with completed checkboxes
   - Document any plan deviations or discoveries

2. **Architecture Readiness Check**
   - Verify foundation is solid for next phase
   - Ensure no technical debt that would complicate future work
   - Confirm all existing functionality works

3. **Explicit Phase Completion Declaration**
   - Mark phase as complete in both log.md and refactor-plan.md
   - State readiness for next phase
   - Get explicit approval before proceeding

### Never Do

- Don't proceed to next phase without explicit instruction
- Don't skip testing intermediate steps
- Don't make assumptions about user intent
- Don't combine multiple phases in single implementation

## Technical Implementation Patterns

### Refactoring Sequence

1. **Enhance Core Module** (Add types and functions)
2. **Replace Usage** (Update calling code to use new functions)
3. **Remove Old Code** (Clean up deprecated functions)
4. **Test and Validate** (Ensure everything works)

### Code Change Patterns

```fsharp
// Pattern: Centralized parsing
let parseDocument (pipeline: MarkdownPipeline) (content: string) : ParsedDocument =
    // Single entry point that returns structured data

// Pattern: AST-based processing  
let extractFromAst (doc: MarkdownDocument) : ResultType =
    // Use Markdig's built-in traversal instead of string manipulation

// Pattern: Composable functions
let processContent = parseDocument >> validateContent >> renderContent
```

### Error Handling Evolution

```fsharp
// Phase 1: Basic functionality
let processPost (content: string) : ProcessedPost

// Later phases: Result types
let processPost (content: string) : Result<ProcessedPost, ProcessingError>
```

## Quality Metrics

### Success Indicators

- **Reduced Complexity**: Fewer lines of code doing the same work
- **Eliminated Duplication**: Single source of truth for common operations
- **Improved Performance**: Fewer parse operations, more efficient algorithms
- **Better Separation**: Clear module boundaries and responsibilities

### Measurement Examples

```
Before Phase 1:
- extractTextContent: 17 lines of string manipulation
- extractMediaFromMarkdown: 10 lines duplicating parsing
- Total parsing calls per post: 3+ separate calls

After Phase 1:
- extractTextContent: 3 lines using AST-based extraction  
- extractMediaFromMarkdown: 3 lines using centralized parser
- Total parsing calls per post: 1 centralized call
```

## Learning Integration

### Capture Discoveries

- Technical constraints (like Markdig block ownership)
- Performance characteristics
- API design insights
- Testing approaches that work well

### Update Future Planning

- Revise remaining phases based on learnings
- Adjust complexity estimates
- Update migration strategies
- Refine architectural decisions

## Phase Transition Protocol

### Before Moving to Next Phase

1. **Complete Current Phase Documentation**
   - Finalize log entries with metrics and analysis
   - Update refactor-plan.md with completed checkboxes
   - Document any plan deviations or discoveries

2. **Architecture Readiness Check**
   - Verify foundation is solid for next phase
   - Ensure no technical debt that would complicate future work
   - Confirm all existing functionality works

3. **Explicit Phase Completion Declaration**
   - Mark phase as complete in both log.md and refactor-plan.md
   - State readiness for next phase
   - Get explicit approval before proceeding

### Never Do

- Don't proceed to next phase without explicit instruction
- Don't skip testing intermediate steps
- Don't make assumptions about user intent
- Don't combine multiple phases in single implementation

## Technical Implementation Patterns

### Refactoring Sequence

1. **Enhance Core Module** (Add types and functions)
2. **Replace Usage** (Update calling code to use new functions)
3. **Remove Old Code** (Clean up deprecated functions)
4. **Test and Validate** (Ensure everything works)

### Code Change Patterns

```fsharp
// Pattern: Centralized parsing
let parseDocument (pipeline: MarkdownPipeline) (content: string) : ParsedDocument =
    // Single entry point that returns structured data

// Pattern: AST-based processing  
let extractFromAst (doc: MarkdownDocument) : ResultType =
    // Use Markdig's built-in traversal instead of string manipulation

// Pattern: Composable functions
let processContent = parseDocument >> validateContent >> renderContent
```

### Error Handling Evolution

```fsharp
// Phase 1: Basic functionality
let processPost (content: string) : ProcessedPost

// Later phases: Result types
let processPost (content: string) : Result<ProcessedPost, ProcessingError>
```

## Quality Metrics

### Success Indicators

- **Reduced Complexity**: Fewer lines of code doing the same work
- **Eliminated Duplication**: Single source of truth for common operations
- **Improved Performance**: Fewer parse operations, more efficient algorithms
- **Better Separation**: Clear module boundaries and responsibilities

### Measurement Examples

```
Before Phase 1:
- extractTextContent: 17 lines of string manipulation
- extractMediaFromMarkdown: 10 lines duplicating parsing
- Total parsing calls per post: 3+ separate calls

After Phase 1:
- extractTextContent: 3 lines using AST-based extraction  
- extractMediaFromMarkdown: 3 lines using centralized parser
- Total parsing calls per post: 1 centralized call
```

## Learning Integration

### Capture Discoveries

- Technical constraints (like Markdig block ownership)
- Performance characteristics
- API design insights
- Testing approaches that work well

### Update Future Planning

- Revise remaining phases based on learnings
- Adjust complexity estimates
- Update migration strategies
- Refine architectural decisions

## Example Phase Completion Checklist

- [ ] All phase objectives implemented and tested
- [ ] No regression in existing functionality
- [ ] Code quality metrics improved
- [ ] Architecture foundation solid for next phase
- [ ] All changes documented in log.md
- [ ] Refactor-plan.md updated with completion status
- [ ] Lessons learned documented for future phases
- [ ] Explicit completion declaration made

This workflow ensures systematic, documented, and quality-focused refactoring that preserves functionality while improving architecture incrementally.
