# CryPixiv-UWP
Complete redesign of CryPixiv on UWP platform using my own Pixiv API made from scratch

This project is still in active development, so I haven't started versioning it yet.

## Installation instructions
1. Download `app.zip` from *Releases*
2. Install certificate (`.cer` file) to **Trusted Root Certification Authorities** location (under **Local machine**))
3. Install app by double clicking on the `.appxbundle` file

If install fails due to **missing dependencies**, download the `dependencies.zip` and install the ones for your CPU architecture.

After initial setup is done, you **don't need to reinstall the certificate or dependencies** on newer releases.

## Features
- Seperate searches with tabs
- Sort illustrations by score as you search
- Illustration filter using 3 levels (Safe, Questionable, NSFW)
- Tags get automatically translated
- Image information displayed for every image (Resolution, File size, Other details...)
- View **Ranking** (any category), **Bookmarks** (public/private), **Following** (public/private), **Recommended**
- Search illustrations by Artist
- Other basic functionality (Bookmarking publicy and privately, Following artists)

## Planned Features and To-Do
- Displaying search history
- Showing searching status (if reached end, if error, if limit reached, ...)
- Ability to reset any tab, not just searching tab
- Sort by score on Artist page
- Increasing searching limit (right now it's 5000) using older APIs and some tricks
- Copying images and keeping PNG transparency

## Hotkeys
- Use E/Q to navigate between illustrations
- Use arrow keys or scroll wheel to navigate between images on illustrations with multiple images

## Screenshots
### Home page
![Home page](https://cryshana.me/viewer/ikm01tlcmqx.jpg?d=true)
### Details page
![Details page](https://cryshana.me/viewer/yk4o1t4h4pq.jpg?d=true)
### Artist page
![Artist page](https://cryshana.me/viewer/yrmdzdxyqo1.jpg?d=true)
### Searching
![Searching](https://cryshana.me/viewer/oa52qhrpglp.jpg?d=true)
### Tag translation
![Tag translation](https://cryshana.me/viewer/so5aky40m54.jpg?d=true)
