TileSetUtility
==============

TileSetUtility is a simple program used to automatically generate tile sets and map files for use in 2d side-scrolling games.

![Tile map screenshot](/Documentation/tile_set_utility_screenshot.png)

Features
--------
* Supports export to [Tiled](http://www.mapeditor.org/) (.tmx file format)
* Supports Base64 and zlib compression for small file size
* Parses Adobe Photoshop (.psd) files, including transparency and layers to generate parallax scrolling
* Searches source map image to eliminate duplicate tiles and exports to compressed sprite sheet

Screenshots
-----------
### Sample sprite sheet output
![Tile set screenshot](/Documentation/tile_set_screenshot.png)
### Sample tile map Photoshop file input
![Tile map screenshot](/Documentation/tile_map_screenshot.png)
### Sample generated [map file](/Documentation/tiled_map.tmx)
![Tiled screenshot](/Documentation/tiled_screenshot.png)
