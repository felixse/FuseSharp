# FuseSharp
A .NET port of https://github.com/krisk/fuse-swift

## What is Fuse?

Fuse is a super lightweight library which provides a simple way to do fuzzy searching.

#### Example 1

```csharp
var fuse = new Fuse();
var result = fuse.Search("od mn war", "Old Man's War");

Debug.WriteLine(result.Score);  // 0.44444444444444442
Debug.WriteLine(result.Ranges); // [(0...0), (2...6), (9...12)]
```

#### Example 2

Search for a text pattern in an array of srings.

```csharp
var books = new string[] { "The Silmarillion", "The Lock Artist", "The Lost Symbol" };
var fuse = new Fuse();

// Improve performance by creating the pattern once
var pattern = fuse.CreatePattern("Te silm");

// Search for the pattern in every book
foreach(var book in books)
{
    var result = fuse.Search(pattern, book);
    Debug.WriteLine(result.Score);
    Debug.WriteLine(result.Ranges);
}
```

#### Example 3

```csharp
class Book : IFuseable
{
    public string Title { get; set; }
    public string Author { get; set; }

    IEnumerable<FuseProperty> IFuseable.Properties => new[]
    {
        new FuseProperty(Title, 0.3),
        new FuseProperty(Author, 0.7)
    };
}

var books = new Book[]
{
    new Book { Author = "John X", Title = "Old Man's War fiction" },
    new Book { Author = "P.D. Mans", Title = "Right Ho Jeeves" }
};

var fuse = new Fuse();
var results = fuse.Search("man", books);

foreach(var result in results)
{
    Debug.WriteLine("index: " + result.index);
    Debug.WriteLine("score: " + result.score);
    Debug.WriteLine("results: " + result.results);
    Debug.WriteLine("---------------");
}

// Output:
//
// index: 1
// score: 0.015
// results: [(key: "author", score: 0.015000000000000003, ranges: [(5...7)])]
// ---------------
// index: 0
// score: 0.028
// results: [(key: "title", score: 0.027999999999999997, ranges: [(4...6)])]
```

### Options

`Fuse` takes the following options:

- `location`: Approximately where in the text is the pattern expected to be found. Defaults to `0`
- `distance`: Determines how close the match must be to the fuzzy `location` (specified above). An exact letter match which is `distance` characters away from the fuzzy location would score as a complete mismatch. A distance of `0` requires the match be at the exact `location` specified, a `distance` of `1000` would require a perfect match to be within `800` characters of the fuzzy location to be found using a 0.8 threshold. Defaults to `100`
- `threshold`: At what point does the match algorithm give up. A threshold of `0.0` requires a perfect match (of both letters and location), a threshold of `1.0` would match anything. Defaults to `0.6`
- `maxPatternLength`: The maximum valid pattern length. The longer the pattern, the more intensive the search operation will be. If the pattern exceeds the `maxPatternLength`, the `Search` operation will return `null`. Why is this important? [Read this](https://en.wikipedia.org/wiki/Word_(computer_architecture)#Word_size_choice). Defaults to `32`
- `isCaseSensitive`: Indicates whether comparisons should be case sensitive. Defaults to `false`
