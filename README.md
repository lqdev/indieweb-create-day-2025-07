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

This project follows a systematic development workflow documented in [Copilot Instructions](.github/copilot-instructions.md).

- **Features & Bugs**: See [projects/backlog.md](projects/backlog.md)
- **Active Work**: Check [projects/active/](projects/active/)
- **Development Process**: Follow the workflow in [.github/copilot-instructions.md](.github/copilot-instructions.md)
- **Project History**: View [changelog.md](changelog.md)

## Examples

The system generates clean, responsive HTML from markdown with media extensions. Check the `_public/` directory after running the generator to see examples of:

- Image galleries with proper aspect ratios
- Video embeds with responsive design  
- Audio players with custom controls
- Mixed media posts combining multiple formats

## License

[Add license information]