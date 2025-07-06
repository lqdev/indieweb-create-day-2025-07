# IndieWeb Content Management System

A comprehensive markdown-based content management system for creating media-rich IndieWeb posts with custom markdown extensions. Built in F# with support for images, videos, audio, and mixed media content.

Originally started as experiments for [IndieWeb Create Day (July 2025)](https://events.indieweb.org/2025/07/indieweb-create-day-3q2PTCbGioi9), this project has evolved into a fully functional content management system with sophisticated project management workflows.

## What This System Does

- **Custom Markdown Extensions**: Parse markdown files with `:::media` blocks for rich media content
- **Multi-Media Support**: Handle images, videos, audio, and mixed media posts with proper aspect ratios
- **Static Site Generation**: Convert markdown to clean, responsive HTML
- **YAML Front-matter**: Support for metadata, tags, dates, and post types
- **Project Management**: Comprehensive workflow for feature development and documentation

## Project Structure

```
├── markdown-extensions/          # Main application
│   ├── script.fsx               # F# application entry point
│   ├── start-server.ps1         # Development server
│   ├── _src/                    # Source markdown files
│   │   ├── image.md            # Image post example
│   │   ├── video.md            # Video post example
│   │   ├── audio.md            # Audio post example
│   │   └── mixed.md            # Mixed media example
│   └── _public/                 # Generated HTML output
│       ├── *.html              # Generated post pages
│       └── main.css            # Styling
├── projects/                    # Project management
│   ├── backlog.md              # Feature backlog
│   ├── active/                 # Active projects
│   ├── archive/                # Completed projects
│   └── templates/              # Project templates
├── logs/                       # Daily development logs
├── changelog.md                # Project history
└── .github/
    └── copilot-instructions.md # Development workflow
```

## Getting Started

### Prerequisites

- .NET SDK with F# support
- PowerShell (for development server)

### Running the Application

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd indieweb-create-day-2025-07
   ```

2. **Navigate to the main application**
   ```bash
   cd markdown-extensions
   ```

3. **Run the F# script to generate posts**
   ```bash
   dotnet fsi script.fsx
   ```

4. **Start the development server**
   ```powershell
   .\start-server.ps1
   ```

5. **View generated content**
   - Open browser to the local server (URL will be shown in terminal)
   - Check `_public/` directory for generated HTML files

### Creating Content

1. **Create markdown files** in `_src/` with YAML front-matter:
   ```markdown
   ---
   post_type: media
   title: My Post Title
   publish_date: "2025-07-05 11:47 -05:00"
   tags: ["tag1", "tag2"]
   ---
   
   Your content here!
   
   :::media
   - media_type: image
     uri: https://example.com/image.jpg
     caption: Image caption
     alt_text: Accessibility description
     aspect: 3:2
   :::media
   ```

2. **Run the generator** to create HTML output
3. **Check results** in `_public/` directory

## Technology Stack

- **F#**: Core application language
- **Markdig**: Markdown processing with custom extensions
- **Giraffe.ViewEngine**: HTML generation
- **YamlDotNet**: YAML front-matter parsing

## Contributing

This project follows a systematic development workflow with comprehensive project management.

### Quick Start for Contributors

1. **Review Current Work**: Check [projects/backlog.md](projects/backlog.md) for available tasks
2. **Understand Workflow**: Read [.github/copilot-instructions.md](.github/copilot-instructions.md) for development process
3. **Check Active Projects**: See [projects/active/](projects/active/) for ongoing work
4. **Review Project History**: Browse [changelog.md](changelog.md) for context

### Development Workflow

- **Backlog-Driven Development**: Select items from prioritized backlog
- **Requirements Phase**: Create detailed project plans before implementation
- **Daily Logging**: Document all work in `logs/YYYY-MM-DD-log.md`
- **Project Lifecycle**: Backlog → Active → Archive with comprehensive documentation

### Project Management Features

- **Feature Tracking**: Comprehensive backlog with categorized improvements
- **Documentation Standards**: Consistent templates and logging practices
- **Quality Assurance**: Systematic testing and validation requirements
- **Knowledge Capture**: Lessons learned preserved for future development

### Current Priorities

Check the backlog for current high-priority items including:
- Bug fixes (like the extra `:::` in mixed media output)
- Performance improvements (image optimization)
- New features (RSS feeds, tagging system)
- Documentation enhancements

### Getting Help

- **Architecture Questions**: See project logs in `logs/` directory
- **Workflow Guidance**: Detailed process in copilot-instructions.md
- **Feature Requests**: Add to backlog.md following established format

## Examples

### Input: Markdown with Media Extensions

```markdown
---
post_type: media
title: My vacation post
publish_date: "2025-07-05 11:47 -05:00"
tags: ["vacation", "summer", "travel"]
---

Such a fun trip!

:::media
- media_type: image
  uri: https://images.unsplash.com/photo-1469854523086-cc02fe5d8800
  caption: Hot day, but worth it!
  alt_text: A van driving through the desert
  aspect: 3:2
- media_type: video
  uri: https://archive.org/download/CC_1916_09_04_TheCount/CC_1916_09_04_TheCount_512kb.mp4
  caption: Classic Charlie Chaplin
  aspect: 16:9
:::media
```

### Output: Responsive HTML

The system generates clean, semantic HTML with:
- **Responsive media galleries** with proper aspect ratios
- **Accessible markup** with alt text and captions
- **Tagged content** with metadata display
- **CSS Grid layouts** for optimal viewing on all devices

### Live Examples

After running the generator, check these files in `_public/`:
- `image.html` - Image gallery with multiple aspect ratios
- `video.html` - Video embed with responsive design
- `audio.html` - Audio players with custom controls  
- `mixed.html` - Combined media types in single post

## Architecture Overview

### Core Components

- **Domain Models**: Type-safe representations of media types, aspect ratios, and post metadata
- **Markdown Parser**: Custom Markdig extensions for `:::media` block processing
- **Content Processor**: Business logic layer separating parsing from presentation
- **Post Generator**: Configuration-driven batch processing system
- **Error Handling**: Comprehensive Result types with structured validation

### Processing Pipeline

```
Markdown Files → Parse (Markdig) → Process (Business Logic) → Render (HTML) → Output Files
     ↓              ↓                    ↓                     ↓              ↓
  _src/*.md    YAML + Media         Structured Data        HTML + CSS      _public/*.html
```

### Technology Stack

- **F#**: Core application language with functional programming patterns
- **Markdig**: Markdown processing engine with custom extension support
- **Giraffe.ViewEngine**: Type-safe HTML generation library
- **YamlDotNet**: YAML front-matter parsing for post metadata
- **Result Types**: Functional error handling throughout the pipeline

## Project Status

### Current Capabilities ✅

- **Multi-Media Support**: Images, videos, audio, and mixed content
- **Responsive Output**: CSS Grid layouts with proper aspect ratios
- **Type-Safe Processing**: F# domain models with comprehensive validation
- **Project Management**: Systematic workflow with backlog and documentation
- **Error Handling**: Structured error types with meaningful messages

### Roadmap

**High Priority**
- RSS/Atom feed generation for content syndication
- Automated image optimization and compression
- Tag system for content organization

**Medium Priority**
- Sitemap generation for SEO
- Enhanced responsive design improvements
- POSSE (Post Own Site, Syndicate Elsewhere) automation

**Research & Exploration**
- Alternative output formats (PDF, EPUB)
- Performance optimizations
- Advanced IndieWeb features

### Known Issues

- Extra `:::` characters appearing in mixed media output (tracked in backlog)

For complete project history and detailed progress, see [changelog.md](changelog.md).

## License

[Add license information]