# Fb2Kindle

[![Stars](https://img.shields.io/github/stars/sergiye/fb2kindle?style=flat-square)](https://github.com/sergiye/fb2kindle/stargazers)
[![Fork](https://img.shields.io/github/forks/sergiye/fb2kindle?style=flat-square)](https://github.com/sergiye/fb2kindle/fork)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/sergiye/fb2kindle?style=plastic)
![GitHub all releases](https://img.shields.io/github/downloads/sergiye/fb2kindle/total?style=plastic)
![GitHub last commit](https://img.shields.io/github/last-commit/sergiye/fb2kindle?style=plastic)

*Fb2Kindle is a portable, open-source fb2 books converter into Amazon Kindle formats (mobi & epub).*

----

## Download Latest Version

Fb2Kindle provides an easy to use solution to convert, format with customizable css, combine multiple files into one and send your e-books to the Amazon Kindle devices.

The latest stable version can be downloaded from the [releases](https://github.com/sergiye/fb2kindle/releases) page, or get the newer one directly from:
[Latest Version](https://github.com/sergiye/fb2kindle/releases/latest)

Features include:

  * Support for `.fb2` files as input and `.mobi`/`.epub` files as output
  * Joining several `.fb2` books from folder to one output file (for book series)
  * Send generated book to email (see arguments)
  * Multiple commandline arguments for customizing behavior

# Build

**The recommended way to get the program is BUILD from source**
- Install git, Visual Studio (2022 or higher)
- `git clone https://github.com/sergiye/fb2kindle.git`
- build

----

### How To Use

To use:
  * Download the latest version from releases
  * There is no installation required, just start executable file from anywhere on your computer
  * Drag and drop any `.fb2` file to an app icon in file explorer to convert single file (with default options)


### Start and possible command-line arguments

  Fb2Kindle.exe [options]

  * `<path>`: input `.fb2` file path or files mask (ex: `*.fb2`) or path to `.fb2` files
  * `-epub`: create file in epub format
  * `-css` <styles.css>: styles used in destination book (example can be found here: [Fb2Kindle/Fb2Kindle.css](https://github.com/sergiye/fb2kindle/raw/master/Fb2Kindle/Fb2Kindle.css))
  * `-a`: process all `.fb2` books in app folder
  * `-r`: process files in subfolders (work with -a key)
  * `-j`: join files from each folder to the single book
  * `-o`: hide detailed output
  * `-w`: wait for key press on finish
  * `-mailto`: send document to email (kindle send-by-email delivery, see `-save` option to configure SMTP server)
  * `-save`: save parameters (listed below) to be used at the next start (`Fb2Kindle.json` file)


  * `-d`: delete source file after successful conversion
  * `-u` or `-update`: update application to the latest version. You can combine it with the `-save` option to enable auto-update on every run
  * `-s`: add sequence and number to the document title
  * `-c` (same as `-c1`) or `-c2`: use compression (slow)
  * `-ni`: no images
  * `-dc`: DropCaps mode
  * `-g`: grayscale images
  * `-jpeg`: save images in jpeg
  * `-ntoc`: no table of content
  * `-nch`: no chapters

### Examples:

    Fb2Kindle.exe somebook.fb2
    Fb2Kindle.exe "c:\booksFolder\*.fb2"
    Fb2Kindle.exe -a -r -j -d -w
    Fb2Kindle.exe "c:\bookSeries\*.fb2" -j -epub -mailto user@kindle.com -update -save

also you can check cmd script examples in archive here [other/scripts.7z](https://github.com/sergiye/fb2kindle/raw/master/other/scripts.7z)

----

## License

This program is free software: you can redistribute it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License  along with this program.  If not, see http://www.gnu.org/licenses/.
