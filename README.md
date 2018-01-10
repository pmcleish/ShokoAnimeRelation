# Shoko Anime Relation DOT Generator

Generates a DOT file for use with graphviz which will generate a graph of the Shoko anime relations.

GraphViz can be downloaded from [here](https://graphviz.gitlab.io/_pages/Download/Download_windows.html)

To use, configure SQL Server connection string in the config file, then run the application. The DOT file content will be written
to standard output. To save to a file, simply redirect standard output to a file (e.g. `ShokoAnimeRelation.exe > relations.dot`)

Once a DOT file has been created, use graphviz to generate and SVG. For example, from a command prompt:
`D:\GraphViz\bin\sfdp -Tsvg -o relations.svg relations.dot`

The generated graph only contains anime that has one or more relations (and is rather messy).
The background colours for the nodes should match roughly what AniDB has.
The red square nodes are anime that have a relation, but have no AniDB_Anime record.
The double octagon nodes are the series that Shoko would be choosing as the main series of the AnimeGroup.
Also, each node can be clicked to open it's anidb page in a separate browser tab.


(NOTE: code developed using Jetbrains Rider 2017.3, should probably compile in VS 2017, etc.)
