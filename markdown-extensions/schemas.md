# Extension Schemas

The intent for these extensions is to be used inside of a post and to be able to customize rendering. They are not intended to be a replacement for front-matter YAML, but to be used alongside it. 

## Media

Media posts can contain different forms of media:

- Image - (Gallery)
- Video - (Live streams, Shorts / Stories / Reels, Playlists)
- Audio - (Podcasts, Songs, Playlists, Voice Notes?)

There has to be at least one media item in a media collection. There is no upper limit to the number of elements in a collection.

To start off, I don't think I care too much for mixed collections. However, it might be interesting to think about. For example, let's say that I want to have both images and videos from a trip or experience. There's nothing stopping me from just building a single collection with mixed media since I'm only using URLs. I can probably leave that for later though. For now, I'll focus on individual media type collections

### Properties

- **media_type**: The type of media (image, audio, video)
- **uri**: The Uri of the resource
- **caption**: A caption to display with the resource
- **alt-text**: Descriptive text for accessibility
- **aspect**: Aspect Ratio
    - Images
        - 1:1 (square) - Instagram posts, profile pics, general social media
        - 4:3 - Traditional photography, older digital cameras
        - 3:2 - Modern DSLR cameras, most phone cameras
        - 16:9 - Widescreen, modern displays, web banners
    - Videos
        - 16:9 - YouTube, most modern video content, widescreen
        - 9:16 - TikTok, Instagram Reels, Shorts (vertical)
        - 4:3 - Older video content, some livestreams
        - 1:1 - Square video posts (Instagram, some social platforms)

### Assumptions

- Resources will most likely not be hosted on my site. Especially in the context of video hosting, they most likely will be on YouTube. Maybe PeerTube. In any case, I may want to render as video or embed in those cases. Same goes for songs and things like that. Although audio, I think may be easier to host. 