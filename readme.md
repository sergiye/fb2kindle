# Fb2Kindle

[![Release](https://img.shields.io/github/v/release/sergiye/fb2kindle)](https://github.com/sergiye/fb2kindle/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/sergiye/fb2kindle/total?color=ff4f42)](https://sergiye.github.io/github-release-stats/?username=sergiye&repository=fb2kindle&page=1&per_page=100)
![Last commit](https://img.shields.io/github/last-commit/sergiye/fb2kindle?color=00AD00)

*Fb2Kindle is a free, portable Windows tool that converts FictionBook (`.fb2`) e-books into Amazon Kindle formats (`.mobi` and `.epub`).*

It converts single books or whole folders, can join a book series into one file, lets you restyle the result with your own CSS, and can send the finished book straight to your Kindle by email.

----

## What does it look like?

Fb2Kindle adds a menu to Windows Explorer, so you can convert a book or a whole folder with a right click:

[<img src="https://github.com/sergiye/fb2kindle/raw/master/preview.png" alt="preview"/>](https://github.com/sergiye/fb2kindle/raw/master/preview.png)

## Features

**Input and output**
  * Reads `.fb2` books and `.zip` archives that contain `.fb2` books.
  * Creates `.mobi` (KF8, for Kindle devices and apps) or `.epub` (for Send to Kindle and most other readers).
  * Builds a clickable table of contents from the book's chapters, plus a separate contents page (you can turn it off).
  * Adds a title page with author, title, series, annotation and publisher, and uses the book's cover as the Kindle cover.
  * Turns footnote references into links to the notes.

**Batch processing**
  * Converts one file, every file that matches a mask (for example `*.fb2`), or all books in a folder and its subfolders.
  * Joins all books in a folder into a single e-book, which is handy for a book series. Books are added in file name order, and each one gets its own section in the table of contents.
  * Can delete the source files once the conversion succeeds.

**Look and layout**
  * Uses a built-in stylesheet that you can replace with your own CSS, including embedded fonts.
  * Optional drop caps: an enlarged first letter in the first paragraph of each chapter.
  * Can add the series name and number to the book title.

**Images**
  * Shrinks large images to fit the Kindle screen (824×1200 by default) to make the file smaller.
  * Can convert images to grayscale or leave them out altogether (the cover is always kept).
  * Can shrink the images inside your original `.fb2` files to save disk space, without converting anything.

**Delivery and integration**
  * Sends the finished book to your Kindle email address (Send to Kindle) through any SMTP server, for example Gmail.
  * Adds right-click menu items to Windows Explorer for `.fb2` files and folders. No administrator rights are needed.
  * Remembers your favorite options between runs.
  * Can update itself to the latest release.

## Requirements

  * Windows 7 SP1 or later with .NET Framework 4.8 (already included in current versions of Windows 10 and 11).
  * No installation: Fb2Kindle is a single portable `.exe`. The Amazon KindleGen tool it needs for `.mobi` output is built in.

## Download

Download `Fb2Kindle.exe` from the [latest release](https://github.com/sergiye/fb2kindle/releases/latest). All versions are on the [releases](https://github.com/sergiye/fb2kindle/releases) page.

## How To Use

### 1. Quick start

1. Put `Fb2Kindle.exe` into any folder, for example `C:\Tools\Fb2Kindle`.
2. Double-click it. The app shows its help and asks two questions:
   * **Integrate with Windows Explorer?** Press `Enter` to add the right-click menu (recommended).
   * **Process all local files?** Press `Enter` to convert every `.fb2` book in the app folder and its subfolders, or any other key to exit.

After that, you will usually use the Explorer menu.

### 2. Convert from Windows Explorer

Right-click an `.fb2` file and open the **Fb2Kindle** submenu:

  * **Convert to .mobi**: creates a `.mobi` book next to the source file (large images are shrunk).
  * **Convert to .epub**: creates an `.epub` book next to the source file.
  * **Optimize source**: shrinks the images inside the `.fb2` file itself.

Right-click a **folder** to see the same items. They process every `.fb2` file in that folder and its subfolders, joining the books of each folder into one e-book. This is the easiest way to convert a series.

You can also drag and drop an `.fb2` file onto `Fb2Kindle.exe` to convert it to `.mobi` with default settings.

To add or remove the menu later, run:

    Fb2Kindle.exe -register
    Fb2Kindle.exe -unregister

### 3. Where the result goes

The new book is saved **next to the source file** with the same name, for example `Book.fb2` becomes `Book.mobi`. Existing files are never overwritten: if `Book.mobi` already exists, the new file is named `Book(v1).mobi`, then `Book(v2).mobi`, and so on. For joined books, the output is named after the first file in the folder.

### 4. Command line

    Fb2Kindle.exe <path> [options]

`<path>` can be placed anywhere among the options. It can be:

  * a single file: `C:\Books\Book.fb2` or `C:\Books\Book.zip`
  * a mask: `C:\Books\*.fb2` or `C:\Books\*.zip`
  * a folder, ending with a backslash: `C:\Books\` (same as `C:\Books\*.fb2`)

A file name or mask without a folder (for example `Book.fb2` or `*.fb2`) is looked up in the current folder. Paths with spaces must be in quotes.

#### Input and output

| Option | Description |
|---|---|
| `-epub` | Create `.epub` instead of `.mobi`. |
| `-a` | Process all `.fb2` books in the app folder (use instead of `<path>`). |
| `-r` | Also process all subfolders. |
| `-j` | Join all books of each folder into one e-book (for a book series). |
| `-css <file>` | Use your own stylesheet instead of the built-in one. If the file is not found as given, it is looked up in the app folder. |
| `-mailto <address>` | Send the book to this email address, usually your `...@kindle.com` address. See [Send to Kindle by email](#5-send-to-kindle-by-email). |

#### Book options (can be saved with `-save`)

| Option | Description |
|---|---|
| `-s` | Add the series name and number to the book title, for example `Series 3 Title`. |
| `-dc` | Drop caps: enlarge the first letter of the first paragraph in each chapter. |
| `-ntoc` | Do not add a separate table of contents page. |
| `-nch` | Do not split the book into nested chapters: all headings go into a single flat list. |
| `-c` or `-c1`, `-c2` | Compress the `.mobi` file (smaller but slower; `-c2` is the strongest). |
| `-optimize` | Shrink large images in the result to fit 824×1200. |
| `-ni` | No images (only the cover is kept). |
| `-g` | Convert images to grayscale. |
| `-jpeg` | Save images of unknown type as JPEG instead of PNG. |
| `-d` | Delete the source files after a successful conversion. |
| `-u` or `-update` | Check for a new version after the conversion and update the app. |

To turn off a saved option for one run, add `-` at the end of it: `-d-`, `-dc-`, `-c-` and so on.

#### Other options

| Option | Description |
|---|---|
| `-optimizeSource` | Shrink the images inside the source `.fb2` files to 824×1200 instead of converting them. An image is replaced only if the result is smaller. |
| `-save` | Save the book options used in this run to `Fb2Kindle.json` next to the app. They are applied automatically on every next run. |
| `-o` | Hide the detailed KindleGen output. |
| `-w` | Wait for a key press before closing the window. |

### 5. Send to Kindle by email

Fb2Kindle can email the book to your Kindle. Amazon accepts only EPUB by email, so with `-mailto` the book is always created as `.epub`.

1. In your Amazon account, open **Manage Your Content and Devices → Preferences → Personal Document Settings**. Find your Kindle email address and add the sender address to the **Approved Personal Document E-mail List**.
2. Run Fb2Kindle once with `-save` to create `Fb2Kindle.json` next to the app. Open the file in a text editor and fill in the SMTP settings:

   ```json
   "SmtpServer": "smtp.gmail.com",
   "SmtpPort": 587,
   "SmtpLogin": "your.name@gmail.com",
   "SmtpPassword": "your password",
   ```

   Gmail and many other providers require an **app password** instead of your normal password. The connection always uses SSL/TLS.
3. Send a book:

       Fb2Kindle.exe "C:\Books\Book.fb2" -mailto your.name@kindle.com

After a successful send, the book file is not kept. If sending fails, the book is saved next to the source file as usual.

The password is stored in `Fb2Kindle.json` as plain text. Use an app password rather than your main account password, and do not share the settings file.

### 6. Settings file

`Fb2Kindle.json` lives next to `Fb2Kindle.exe` and is created by `-save`. Besides the book options and SMTP settings, you can edit these values by hand:

  * `OptimizeImagesWidth` / `OptimizeImagesHeight`: the maximum image size (default `824` × `1200`).
  * `SmtpTimeout`: the time to wait for the mail server, in milliseconds (default `100000`).

### 7. Custom styles and fonts

The built-in stylesheet is [Fb2Kindle/Fb2Kindle.css](https://github.com/sergiye/fb2kindle/raw/master/Fb2Kindle/Fb2Kindle.css). To change the look of all your books, copy it next to the app as `Fb2Kindle.css` and edit it. It is then used automatically instead of the built-in one. To change the look of a single conversion, pass a stylesheet with `-css`.

To embed fonts, put them into a `fonts` folder next to the app and reference them in the CSS as `src: url("fonts/MyFont.ttf")`.

Drop caps (`-dc`) work only if the stylesheet defines the `span.dc` style.

### Examples

Convert one book to EPUB:

    Fb2Kindle.exe "C:\Books\Book.fb2" -epub

Convert every book in a folder, each into its own `.mobi` file:

    Fb2Kindle.exe "C:\Books\*.fb2"

Convert all books in the app folder and its subfolders, join each folder into one book, delete the sources and wait for a key press at the end:

    Fb2Kindle.exe -a -r -j -d -w

Join a series, send it to your Kindle, and save these settings (including auto-update) for next time:

    Fb2Kindle.exe "C:\Series\*.fb2" -j -mailto your.name@kindle.com -update -save

More ready-to-use `.cmd` scripts are in [other/scripts.7z](https://github.com/sergiye/fb2kindle/raw/master/other/scripts.7z).

----

## How can I help improve it?

Feedback is always welcome!<br/>
Try the app with your books. If something is converted incorrectly, please [open an issue](https://github.com/sergiye/fb2kindle/issues) and attach the `.fb2` file if you can. Pull requests with fixes and improvements are welcome too.

Also, don't forget to ★ star ★ the repository to help other people find it.

<!-- [![Stargazers](https://reporoster.com/stars/sergiye/fb2kindle)](https://star-history.com/#sergiye/fb2kindle&Date) -->
<!-- [![Forkers](https://reporoster.com/forks/sergiye/fb2kindle)](https://github.com/sergiye/fb2kindle/network/members) -->

## Donate!

Every [cup of coffee](https://patreon.com/SergiyE) you donate helps this app get better and shows that the project is in demand.

## License

Fb2Kindle is free to use for **personal, home and other non-commercial purposes only**.

Any commercial use is not allowed without prior written permission from the author. This includes use inside a company or organization, bundling it with or into commercial products or services, and selling it or charging for access to it. For commercial licensing, please contact the author through [GitHub issues](https://github.com/sergiye/fb2kindle/issues).

You may share unmodified copies of the program free of charge, as long as they are used under the same terms.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
