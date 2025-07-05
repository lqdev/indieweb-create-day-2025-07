# Copilot Instructions for Project Development Workflow

## Overview

This document outlines the systematic project development workflow for the indieweb content management system. It captures proven methodologies for managing projects from backlog through completion, supporting both single-focus and multi-project development approaches.

**Key Achievement**: Evolved from refactoring-specific workflow to comprehensive project management system supporting diverse development tasks while maintaining quality and documentation standards.

## Universal Development Methodology

### Core Principles

1. **Work Out Loud**: Document every step with verbose explanations in daily logs
2. **Incremental Implementation**: Make changes in small, testable steps
3. **Preserve Functionality**: Ensure existing behavior is maintained throughout
4. **Evidence-Based Planning**: Update plans based on actual implementation findings
5. **Clear Completion Criteria**: Define success metrics before starting work
6. **Context Preservation**: Maintain project continuity across sessions

### Project Lifecycle

#### Project Initiation

1. **Identify and Plan**
   - Review `projects/backlog.md` for prioritized work
   - Create requirements document using `projects/templates/requirements-template.md`
   - Collaborate to define clear problem statement, success criteria, and technical approach
   - Create detailed implementation plan in `projects/active/[project-name].md`
   - Update backlog item status: `[ ]` → `[>]` (in progress)
   - Define clear objectives and success criteria

2. **Analyze Current State**
   - Examine relevant files completely using large chunk reads
   - Identify specific patterns, dependencies, and constraints
   - Document findings in daily log with specific references
   - **Best Practice**: Large file reads are more efficient than multiple small reads

#### Active Development

3. **Daily Work Sessions**
   - Start with current state analysis in `logs/YYYY-MM-DD-log.md`
   - Document session objectives and planned approach
   - Reference active project plan for context and progress tracking

4. **Step-by-Step Implementation**
   - Make one logical change at a time
   - Document each change in daily log before making it
   - Use appropriate edit tools with sufficient context
   - **Critical**: Consider module dependencies and system constraints

5. **Continuous Testing**
   - Test functionality after each significant change
   - Document test results in daily log
   - Fix issues immediately before proceeding
   - Validate against project success criteria
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

## Project Workflow Management

### Backlog-Driven Development
- Review `projects/backlog.md` for prioritized features and improvements
- Select appropriate items based on current capacity and dependencies
- Move selected items from backlog to active projects

#### Moving Items to Active:
1. **Requirements Phase**: Create requirements document using template in `projects/templates/`
2. **Collaborative Planning**: Work together to define problem statement, success criteria, and approach
3. **Create Project Plan**: Create detailed implementation plan in `projects/active/[project-name].md`
4. **Update Backlog Status**: Mark as in progress: `[ ]` → `[>]`
5. **Begin Documentation**: Start daily logging in date-based log files

#### Priority Levels for Selection:
- **High**: Critical for basic functionality
- **Medium**: Important improvements 
- **Low**: Nice to have features
- **Research**: Exploratory work

### Multi-Project Support
- Support concurrent projects when appropriate
- Maintain context switching discipline with proper documentation
- Ensure each project has clear boundaries and deliverables
- Use project plans in `projects/active/` for focused work tracking

### Project State Management
- **Backlog**: `[ ]` - Ideas and planned work
- **Active**: `[>]` - Currently in progress with project plan
- **Complete**: `[x]` - Finished and moved to archive
- **Archive**: Projects moved to `projects/archive/` with completion summary

### Documentation Hierarchy
1. **Daily Logs** (`logs/YYYY-MM-DD-log.md`) - Implementation details and decisions
2. **Project Plans** (`projects/active/*.md`) - Project scope, objectives, and progress
3. **Backlog** (`projects/backlog.md`) - Strategic overview of planned work
4. **Changelog** (`changelog.md`) - High-level site evolution summary

### Project Completion Protocol

#### When Completing Projects:
1. **Finalize Documentation**
   - Complete final summary in daily log
   - Update project plan with completion status
   - Document lessons learned and architectural impact

2. **Archive Process**
   - Move project plan from `active/` to `archive/`
   - Update backlog: `[>]` → `[x]`
   - Add entry to `changelog.md` with summary

3. **Knowledge Capture**
   - Update copilot-instructions.md with new patterns or learnings
   - Document reusable templates or processes
   - Note improvements for future projects

#### Project Completion Workflow:
1. **Move Project Plan**: From `active/` to `archive/`
2. **Update Backlog Status**: Mark complete: `[>]` → `[x]`
3. **Update Changelog**: Add summary to `changelog.md`
4. **Document Learnings**: Update copilot-instructions.md if applicable

### Changelog Entry Template

When adding entries to `changelog.md`, use this template:

```markdown
## YYYY-MM-DD - [Project Name] [Status Icon]

**Project**: [Link to project plan]  
**Duration**: [Start] - [End]  
**Status**: [Complete/In Progress/Cancelled]

### What Changed
[High-level description of what was accomplished]

### Technical Improvements  
[Bullet points of technical achievements]

### Features Added/Removed
[List of user-facing changes]

### Architecture Impact
[How this affects the overall system]

### Documentation Created/Updated
[Links to relevant documentation]
```

## Example Project Completion Checklist

- [ ] All project objectives implemented and tested
- [ ] No regression in existing functionality
- [ ] Code quality metrics improved (when applicable)
- [ ] Architecture foundation solid for future work
- [ ] All changes documented in daily logs
- [ ] Project plan updated with completion status
- [ ] Lessons learned documented for future projects
- [ ] Backlog item marked complete and project archived
- [ ] Changelog updated with project summary
- [ ] Explicit completion declaration made

This workflow ensures systematic, documented, and quality-focused development that preserves functionality while improving architecture incrementally.
