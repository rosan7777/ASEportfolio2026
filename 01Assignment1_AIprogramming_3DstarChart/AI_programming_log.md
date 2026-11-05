# AI Programming Log
AI used: Claude Opus 5.5 (claude.ai)

---
## Entry 1 – 2026-11-05, 03:48 – feature1-create-star-chart
### Prompt
Context: I asked Claude which way to cut the star data down. It recommended letting it cut the
file with a script (rather than me doing it in Excel), taking the 2000 stars nearest the Sun,
keeping all columns, the header row and stars without proper names. I agreed and sent:

> [attached: hyg_v44.csv.gz] here it is

### Response
`stars.csv` is ready: the header row plus the 2,000 stars nearest the Sun, every original line copied unchanged.

**What's in it, checked from the real file**
- Full dataset: 119,614 stars in 37 columns.
- The cut: Sol first at distance 0; the furthest star kept is at 20.5339.
- Names: 89 stars have a proper name and 1,911 don't. That's why Feature 1 needs generated catalogue names.
- Stars I'll want later: Sol, Proxima Centauri, Rigil Kentaurus, Toliman, Barnard's Star and Sirius are all in.
- Multiple star systems: 286 stars belong to one.

**Facts about the data the code must handle**
1. Columns are numbered from 0, 37 in total: id 0, gl 4, proper 6, dist 9, mag 13, absmag 14,
   spect 15, ci 16, x 17, y 18, z 19, bayer 27, flam 28, con 29, comp 30, comp_primary 31, lum 33.
2. Line 1 is a header, and its column names are in quotes ("id","hip",...). Code must skip it.
3. Some values are quoted, and empty text is written as "". No value contains a comma inside its
   quotes, so splitting on , gives exactly 37 parts on every line, but the " characters must be stripped.
4. Some fields are blank: spect for 150 stars, ci for 429, con for Sol only, proper for 1,911.
   A plain double.Parse on a blank ci crashes, so blank fields need safe parsing.
5. One name has non-English letters (Añañuca). The file is UTF-8; File.ReadAllLines reads UTF-8 by default.
6. Tau Ceti has no proper name in this version. Its row has bayer = Tau and con = Cet, which is
   exactly the case the catalogue-naming feature has to solve.
7. Distances are in parsecs, not light years. (To be confirmed with the astronomy chat.)
8. A small tie at the cut-off: 3 stars share the distance 20.5339, and only one of them made the 2,000.

The script used (Python) parsed every row with a CSV reader, checked each had 37 fields, sorted by
dist then id, and wrote the header plus the first 2000 original lines unchanged.

### Critique
I did not just trust the AI's file. I checked it myself in PowerShell and it had 2001 lines,
which is the header plus 2000 stars. I used the AI with a script instead of Excel because
Excel can change number formats when it saves a CSV.

One weak point: the cut-off at 2000 fell between 3 stars with the same distance (20.5339),
so 1 was kept and 2 were dropped for no real reason. It does not matter much, but the AI's
cut is not perfect.

The AI also told me things my code must handle: the first line is a header, empty text is
written as "", and some fields like spect and ci are blank, so a normal double.Parse would
crash. The distances are in parsecs, not light years, so I need to convert them later.

### Commit
Add stars.csv: 2000 nearest stars from HYG v4.4, copied to output on build


---
## Entry 2 – 2026-11-05, 04:51 – feature1-create-star-chart
### Prompt
Context: I chose Option A (Star.cs only holds data, StarCollection.cs reads the file). I asked Claude:

> just keep that things are required in this. only do that things that are told us to do,
> as i provide you everything for this project, so you better know these things

### Response
Claude mapped each requirement in the spec and demo form to the fields it needs:
- Feature 1, read star data: Id
- Feature 1, catalogue names for unnamed stars: Bayer, Flamsteed, Constellation, Gliese, Hipparcos, HenryDraper
- Feature 2, names and the proper-name filter: ProperName
- Feature 2, spectral class colour: SpectralType
- Feature 2/3, 3D position, bearing and path: X, Y, Z
- Feature 3, distance in light years: Distance (parsecs in the file)

It left out magnitude, colour index, luminosity and multiple-star data because the spec does not
require them. It said which catalogue IDs to use for unnamed stars must be checked with the
astronomy chat first. It could not compile the code itself (no .NET in its workspace).

Code produced: see Star.cs in this commit (13 properties with XML comments, strings default to
string.Empty, catalogue numbers stored as strings).


### Critique
The AI could not run its own code, so I built it myself in Visual Studio and it worked
with 0 errors. I told the AI to only add what the assignment needs, and it explained why
it chose each field, so I could check it against the spec.

But I found one problem. Every property uses "set", so any part of my program could change
a star's data by mistake after it is loaded. "init" would be safer. Also, Distance is in
parsecs, so I must change it to light years later for Feature 3.
### Commit
Add Star.cs data class with fields needed for Features 1-3



---
## Entry 3 – 2026-11-05 – feature1-create-star-chart
### Prompt
No AI used. This is a manual fix of a defect I found in the AI's Star.cs (see Entry 2 critique).

### Change
Changed all 13 properties in Star.cs from `{ get; set; }` to `{ get; init; }`.

### Critique
With "set", any part of the program could change a star's data after loading. With "init",
the values can only be set once, when the star is created from the file. After that they
cannot be changed. I built it again and it still compiles with 0 errors.

### Commit
Fix Star.cs: use init instead of set so star data cannot change after loading

Note: the first commit of this fix only included the log, because Star.cs was not saved.
   The Star.cs change itself is in the next commit.


---
## Entry 4 – 2026-11-05, 23:11 – feature1-create-star-chart
### Prompt
> create StarCollection.cs in the StarChart3D namespace. It should hold a List of Star objects and
> have a method Load(string filePath) that reads stars.csv. The file has a header line to skip,
> 37 columns, and values in quotes that must have the " removed. Some fields are blank, so parsing
> must not crash on them. Use the column numbers: id 0, hip 1, hd 2, gl 4, proper 6, dist 9, x 17,
> y 18, z 19, spect 15, bayer 27, flam 28, con 29. Also add a Count property. Don't generate
> catalogue names yet; that's a separate step. Add XML comments.

### Response
Claude first checked the real file: id, dist, x, y, z are never blank and are plain numbers; blank
and quoted values ("" and "10") only appear in text columns. It then wrote StarCollection.cs
(see this commit):
- column numbers stored as named constants (ColId, ColDist, ...)
- Load() clears old stars, skips the header, splits each line on ',', checks for 37 columns
- text fields: spaces and " removed, blank becomes empty string
- required numbers (id, dist, x, y, z) parsed with CultureInfo.InvariantCulture; if invalid it
  throws InvalidDataException with the line number instead of using 0
- Stars is IReadOnlyList so other classes cannot change the list; Count property added

It pointed out it changed my request: a blank required number throws an error instead of "not
crashing", because using 0 would put the star at the Sun's position. It also said Split(',') only
works because no value contains a comma, and that it could not compile the code.

### Critique
The AI did not do exactly what I asked. I said blank fields must not crash, but it made the
code throw an error if a needed number like x or dist is blank. I agree with this, because
using 0 would put the star at the Sun and that bug would be hard to see. In my file these
columns are never blank, so it is safe.

Splitting on "," is a weak point. It only works because no value in my file has a comma.
I built the project and it compiles, but I have not tested yet that it really loads 2000
stars. I will check that in the next commit.

### Commit
Add StarCollection.cs: load stars.csv into a list of Star objects



---
## Entry 5 – 2026-11-05, 23:19 – feature1-create-star-chart
### Prompt
> In MainWindow.xaml.cs, when the window opens, load stars.csv with StarCollection. Show the number
> of stars loaded in the window title. Also write the count and the first 3 stars (id, name,
> distance) to the Visual Studio Output window, so I can show the data loading in my demo. If
> loading fails, show an error message instead of crashing. Don't draw any stars yet; that's Feature 2.

### Response
Claude first checked the file: the first 3 stars should be Sol (0.0), Proxima Centauri (1.2959) and
Toliman (1.3248). It then wrote MainWindow.xaml.cs (see this commit):
- loads stars in the window's Loaded event, from the folder the .exe runs in
- title shows "Star Chart 3D - N stars loaded"
- Debug.WriteLine prints the count and first 3 stars
- catches only IOException, InvalidDataException and UnauthorizedAccessException and shows a
  MessageBox; other errors still crash so real bugs are not hidden
- removed 10 unused template using lines; wrote System.IO.Path in full because
  System.Windows.Shapes also has a Path class

### Critique
I tested it myself. The window title says 2000 stars loaded and the Output window shows Sol,
Proxima Centauri and Toliman with the right distances, so StarCollection works. This is the
test I said I would do in Entry 4.

The AI caught a hidden problem: the template had "using System.Windows.Shapes", which has its
own Path class that clashes with System.IO.Path. It only catches 3 kinds of error on purpose,
so other bugs still show up. The distance is still in parsecs, which I fix in Feature 3.

### Commit
Load stars on window open and show the count in the title and Output window