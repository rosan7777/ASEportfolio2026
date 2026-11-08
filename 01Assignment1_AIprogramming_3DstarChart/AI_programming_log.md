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


---
## Entry 6 – 2026-11-05, 23:44 – feature1-create-star-chart
### Prompt
> Create a new class CatalogueNames in the StarChart3D namespace with a method that returns the
> display name for a Star, using the order of preference from my domain expert: proper name, then
> Bayer + constellation (Greek symbol, e.g. "τ Cet"; numbers like "Kap-1" become a superscript,
> e.g. "κ¹ Scl"), then Flamsteed + constellation (e.g. "52 Cet"), then Gliese as it is, then
> "HD " + number, then "HIP " + number, then "HYG " + id. Use short constellation abbreviations,
> not full names. Leave HR out. If a Bayer value is not a Greek abbreviation (my data has "p"),
> show it as it is. Don't change Star.cs or StarCollection.cs. Add XML comments.

### Response
Claude first ran the same rules on my stars.csv to show what correct output looks like:
Sol, Proxima Centauri, τ Cet (id 8087, Tau Ceti), κ¹ Cet (id 15419), p Eri (id 7736).
It found that 5 pairs of binary stars get the same name, for example "61 Cyg" for both
Gl 820A and Gl 820B, because Bayer and Flamsteed come before Gliese and only Gliese has the A/B.
It did not fix this because the order was my decision.

It then wrote CatalogueNames.cs (see this commit): a static class with GetDisplayName(Star),
a Greek letter table, and a superscript helper. Differences from the domain expert's sketch:
it uses my real property names, and non-digit characters in a superscript are kept instead of
crashing.

### Critique
The code builds and the app runs. The AI's first version from my domain expert would not
have compiled, because it used the wrong property names, but this one uses mine. The
superscript code is also safer, because a non-digit can no longer crash it.

The big problem is that 5 pairs of binary stars get the same name, like 61 Cyg A and B both
showing "61 Cyg". This comes from the order of preference I chose, not from a bug in the
code. I will fix it in a separate commit. I have not tested the names in the app yet, so I
will do that next.

### Commit
Add CatalogueNames: generate catalogue names for stars without proper names


---
## Entry 7 – 2026-11-05, 23:56 – feature1-create-star-chart
### Prompt
> In MainWindow.xaml.cs, after the stars load, use CatalogueNames to write the display names of
> these test stars to the Output window, looked up by id: 0 (Sol), 8087 (Tau Ceti, should be
> "τ Cet"), 15419 (should be "κ¹ Cet"), 7736 (should be "p Eri"), 103879 and 103883 (both
> "61 Cyg"). Also write how many stars have a proper name and how many got a generated name.
> Don't change any other class.

### Response
Claude added one line to MainWindow_Loaded and a new method WriteCatalogueNameTest() (see this
commit). It looks each test star up by id, writes its generated name next to the expected name
with OK or WRONG, and counts proper vs generated names. It said it added the expected/OK/WRONG
part itself, which I did not ask for, and that the two "61 Cyg" lines passing actually proves
the duplicate-name bug.

Result when I ran it:
```
Catalogue name test:
  id 0: Sol (expected Sol) OK
  id 8087: τ Cet (expected τ Cet) OK
  id 15419: κ¹ Cet (expected κ¹ Cet) OK
  id 7736: p Eri (expected p Eri) OK
  id 103879: 61 Cyg (expected 61 Cyg) OK
  id 103883: 61 Cyg (expected 61 Cyg) OK
  89 stars have a proper name, 1911 got a generated name
```

### Critique
I ran the app and every test line says OK, and the counts (89 proper, 1911 generated) add up
to 2000. So the naming works, including Tau Ceti, the superscript and the Latin "p".

The AI added the expected value and OK/WRONG even though I did not ask. I kept it because it
makes this a real test. But the test also shows a problem: two different stars are both called
"61 Cyg". The code is doing what my rules say, so the rules need fixing, which is my next commit.
This test is only in the Output window, not a proper unit test.
### Commit
Add catalogue name test output for known stars

---
## Entry 8 – 2026-11-06, 00:50 – feature1-create-star-chart
### Prompt
Context: my test in Entry 7 showed two different stars both named "61 Cyg". Claude tested two
fixes on my data. Fix 1: always add the A/B/C letter from the Gliese name to Bayer and Flamsteed
names (44 names change, all duplicates removed). Fix 2: add the letter only when two stars share a
name (only 10 change, but CatalogueNames would need the whole collection). I replied:

> do whichever is simple

### Response
Claude used Fix 1 (see this commit):
- CatalogueNames.cs: new method GetComponentLetter() takes the last letter of the Gliese name if
  it comes straight after a digit ("Gl 820B" gives " B", "Gl 71" gives nothing). It is added to
  Bayer and Flamsteed names.
- MainWindow.xaml.cs: test expected values changed to "p Eri A", "61 Cyg A", "61 Cyg B", and a
  new line counts names used by more than one star.

Result when I ran it:
```
  id 7736: p Eri A (expected p Eri A) OK
  id 103879: 61 Cyg A (expected 61 Cyg A) OK
  id 103883: 61 Cyg B (expected 61 Cyg B) OK
  Names used by more than one star: 0
```

### Critique
The fix works: my test now shows 61 Cyg A and 61 Cyg B, and no name is used by more than one
star. I chose the simple fix. The downside is that 34 stars that did not have a duplicate also
get a letter now, like 70 Oph A. That is still correct because it is part of a binary system.

The new line that counts duplicate names makes the test stronger, because it checks all 2000
stars and not just the 6 I picked. I did not check the A/B naming with my astronomy expert,
so it is based on the programming AI's word.

### Commit
Fix duplicate names for binary stars by adding the component letter (61 Cyg A / B)



---
## Entry 9 – 2026-11-06, 01:48 – feature2-visualising-star-chart
### Prompt
Context: Feature 2 must be "3D Visualising the Star Chart". Claude compared WPF's built-in
Viewport3D with the HelixToolkit library and recommended HelixToolkit (built-in mouse zoom, pan
and rotate, ready-made spheres and 3D text). I checked the spec: it has no rule against libraries,
and the module leader's example also uses HelixToolkit. I said:

> lets start. As i dont have much time so lets start right now

### Response
Claude checked NuGet. HelixToolkit.Wpf 3.1.2 is newest but v3 is a big rewrite that Claude is
less sure of, so it recommended v2: HelixToolkit.Wpf 2.27.3, installed with
`dotnet add ... package HelixToolkit.Wpf --version 2.27.3`.

The build succeeded but showed warning NU1701: the package "was restored using
.NETFramework,Version=v4.6.1 ... v4.8.1 instead of the project target framework
net10.0-windows7.0. This package may not be fully compatible with your project."

Claude then found HelixToolkit.Core.Wpf 2.27.3: the same v2 library, built for .NET Core 3.1 and
compatible with .NET 10. I removed HelixToolkit.Wpf and added HelixToolkit.Core.Wpf. The build
then said "compatible with all the specified frameworks", 0 warnings, 0 errors.

### Critique
The AI's first package choice was wrong. HelixToolkit.Wpf only has .NET Framework versions, so
on my .NET 10 project it gave warning NU1701. The build still succeeded, so I could have missed
it, but it might have caused strange errors later. I did not ignore the warning. The AI found
HelixToolkit.Core.Wpf, the same library built for modern .NET, and now there are 0 warnings.

I chose version 2 instead of the newest version 3, because v3 is a big rewrite and the AI is
less sure of its code, so it could make up code that does not exist. Version 2 is in maintenance
mode, which is fine for this project.

### Commit
Add HelixToolkit.Core.Wpf 2.27.3 for 3D rendering



---
## Entry 10 – 2026-11-06, 02:06 – feature2-visualising-star-chart
### Prompt
> In MainWindow.xaml, add a HelixToolkit HelixViewport3D called StarViewport that fills the
> window. Give it a black background, a perspective camera looking towards the Sun at (0,0,0),
> and default lighting. Make the window bigger (1200 by 800) and keep the existing title code
> working. Don't draw any stars yet; that's the next step.

### Response
Claude replaced MainWindow.xaml (see this commit):
- added xmlns:h="http://helix-toolkit.org/wpf" and a HelixViewport3D named StarViewport,
  Background Black, with the coordinate system arrows and view cube shown
- PerspectiveCamera at (0,0,30) looking at (0,0,0), because the furthest star is about
  20.5 parsecs away, so all stars will fit; near plane 0.01, far plane 1000
- DefaultLights; window 1200 x 800; MainWindow.xaml.cs not changed
It said it was not 100% sure the Core package uses the same xmlns address, and that the build
would confirm it.

### Critique
It works: the window is black and 1200 x 800, the title still shows 2000 stars loaded, and I
can rotate the view with the right mouse button. The AI was honest that it was not sure about
the xmlns address for the Core package, so I checked it by building and running.

The coordinate arrows are useful now because there is nothing else to see, but I might remove
them later. The camera distance of 30 is based on my data, but if I changed stars.csv it would
need changing too, so a fixed number is a weak point.

### Commit
Add black 3D HelixViewport3D with camera looking at the Sun




---
## Entry 11 – 2026-11-06, 02:15 – feature2-visualising-star-chart
### Prompt
> Create a new class StarRenderer in the StarChart3D namespace that draws every star in a
> StarCollection into the HelixViewport3D as a small sphere at its X, Y, Z position (in parsecs).
> Put all the spheres in one mesh so 2000 stars draw quickly. Make all stars white for now;
> colours by spectral class are the next step. In MainWindow.xaml.cs, call StarRenderer after the
> stars load. Don't change Star.cs, StarCollection.cs or CatalogueNames.cs.

### Response
Claude wrote StarRenderer.cs (see this commit): it adds one ModelVisual3D to the viewport, and
Draw() uses HelixToolkit's MeshBuilder.AddSphere for every star (radius 0.1 parsecs, low detail)
and ToMesh to make one mesh. It used a glowing (emissive) white material on top of black, which
I did not ask for, so stars look evenly bright. In MainWindow.xaml.cs it added a starRenderer
field, creates it after InitializeComponent, and calls Draw after the stars load. It also fixed
the indentation in my test method. It said MeshBuilder, AddSphere and ToMesh exist in v2 but
the build is the real test.

Result: 2000 white dots in a ball shape around the Sun, which I can rotate.

### Critique
It works: I can see all the stars as white dots, and they make a ball shape because I chose the
2000 nearest stars. The drawing code is in its own class, StarRenderer, not in MainWindow, which
is better design. The Helix names the AI used were real, because the build had no errors.

One mesh is fast, but it means all stars are one object, so I cannot click or colour one star
on its own. For colours I will need one mesh per colour. The Sun looks the same as every other
star, so it is hard to see where the centre is. The AI also added the glowing material without
me asking, but I kept it because it looks better.

### Commit
Draw all 2000 stars as small white spheres in one mesh (StarRenderer)


---
## Entry 12 – 2026-11-06, 02:46 – feature2-visualising-star-chart
### Prompt
Context: Claude first checked my spect column and found messy values (D, d, sd, lowercase m/k/g,
pec, S and 150 blanks), so I asked my domain expert (domain log Entry 2). I sent Claude the
expert's full answer.

### Response
Claude checked the expert's rules against stars.csv: every star gets a class. It found that the
expert's text and code disagree for white dwarfs (DC5 "white" in the text, F in the code, B by the
expert's own formula 50400/n), so it used the formula with the temperature table for all white
dwarfs. It chose grey for blank types instead of the ci/absmag fallback, so Star.cs and
StarCollection.cs did not need changing (only 67 of the 150 blanks have ci).

Code (see this commit):
- new SpectralColours.cs: chart-style colours per class, grey for unknown, GetSpectralClass()
  strips old prefixes (d, sd, esd, usd, c, g) with a regex, handles white dwarfs by temperature,
  S → M, lowercase m/k/g → M/K/G
- StarRenderer.cs: groups stars by colour, one mesh per colour, glowing material in that colour
- MainWindow.xaml.cs: new WriteSpectralColourTest() writes the count per class to Output

Result when I ran it:
```
Spectral colour test:
  unknown (grey): 151 stars
  B: 17 stars
  A: 52 stars
  F: 133 stars
  G: 186 stars
  K: 417 stars
  M: 1044 stars
  id 0 Sol: spect 'G2V' -> class G
  id 32263 Sirius: spect 'A0m...' -> class A
  id 87665 Barnard's Star: spect 'sdM4' -> class M
  id 118441 Gl 244B: spect 'DA2' -> class B
```

### Critique
The stars are now coloured and the counts add up to 2000. Most stars are red M dwarfs, which
makes sense for the nearest stars. The AI caught that my domain expert was inconsistent about
white dwarfs and fixed it by using one rule.

The AI chose grey for unknown stars instead of the expert's ci fallback, to avoid changing
Star.cs again. That is simpler but less accurate, so 151 stars have no real colour. Grouping by
colour means 7 meshes instead of 1, which is still fast. I still cannot click on one star,
because each colour is one big mesh.
### Commit
Colour stars by spectral class using domain expert rules (SpectralColours)




---
## Entry 13 – 2026-11-06, 03:00 – feature2-visualising-star-chart
### Prompt
> Add star name labels to the 3D view. Every star should show its display name from
> CatalogueNames, as small text just next to its sphere, that always faces the camera. Draw all
> the labels as one group so 2000 labels stay fast. Put this in StarRenderer, not MainWindow.
> Don't change the colours or the other classes.

### Response
Claude updated StarRenderer.cs (see this commit):
- the old sphere code moved unchanged into DrawSpheres(); new DrawLabels() makes one
  BillboardTextItem per star with its CatalogueNames display name, 0.15 parsecs above the star
- all labels go into one HelixToolkit BillboardTextGroupVisual3D (light grey, transparent
  background, font size 10), added to the viewport once
- no other class was changed
It warned that it was not 100% sure of every Helix property name (HorizontalAlignment,
VerticalAlignment, Items), that a transparent background in WPF 3D can show as black boxes,
and that 2000 labels would be crowded until the filter is added.

### Critique

 It built without errors and the star names show next to the stars and keep facing the
camera when I rotate, so the Helix names the AI used were real this time.

With all 2000 names on screen it is very crowded and hard to read, especially in the
middle where the stars are close together. That is why the next step is the filter for
stars without a proper name. Putting all labels in one group keeps it fast, but it also
means I cannot hide or change one label on its own.

### Commit
Add star name labels with one BillboardTextGroupVisual3D (StarRenderer)


---
## Entry 14 – 2026-11-06, 03:05 – feature2-visualising-star-chart
### Prompt
> Add a filter to StarRenderer so Draw can show only the stars that have a proper name (not a
> generated catalogue name). Add a parameter to Draw for this. When the filter is on, hide both
> the sphere and the label of stars without a proper name. Draw should return how many stars it
> drew. In MainWindow, turn the filter on for now so I can test it; the on/off switch is the next
> step. Don't change the other classes.

### Response
Claude changed StarRenderer.cs (see this commit): Draw(stars, onlyProperNames) now filters the
stars with a new HasProperName() helper, passes the same filtered list to DrawSpheres and
DrawLabels, and returns how many stars were drawn. In MainWindow.xaml.cs, Draw is called with
onlyProperNames: true and the title shows "N stars loaded, M shown".

### Critique
It works: only the named stars show now, like Sirius, Procyon, Vega and Altair, and the names
are readable, with Sol in the middle.

The middle is still crowded, because the nearest stars are close to the Sun, so names like Sol,
Proxima Centauri and Wolf 359 overlap. Some labels have a small bar at the start or look cut
off at the end, which I will try to fix later. I also found that some "proper names" in HYG are
really old Gould numbers like "268 G. Cet", so the filter counts them as named stars.

### Commit
Add filter to draw only stars with a proper name (StarRenderer.Draw)



---
## Entry 15 – 2026-11-06, 03:09 – feature2-visualising-star-chart
### Prompt
> Add a checkbox in the top-left corner of the window, on top of the 3D view, labelled "Only show
> stars with a proper name". When it is ticked, redraw with the filter on; when it is unticked,
> show all stars. Start with it unticked. Update the title with how many stars are shown each time
> it changes. Make the checkbox text readable on the black background. Don't change StarRenderer
> or the other classes.

### Response
Claude changed (see this commit):
- MainWindow.xaml: a Border in the top-left with a dark see-through background (#B0202020) holding
  a white CheckBox "Only show stars with a proper name", unticked at start, Focusable="False"
- MainWindow.xaml.cs: new RedrawStars() reads the checkbox, calls StarRenderer.Draw and sets the
  title; new ProperNameFilterCheckBox_Changed() calls RedrawStars() when it is ticked or unticked
It said it made the checkbox not focusable so the space key will not tick it once keyboard
controls are added, but that this means it can only be switched with the mouse.

Result: unticked = "2000 stars loaded, 2000 shown", ticked = "2000 stars loaded, 89 shown".

### Critique
The filter is now switchable, which the demo form asks for. Ticking shows 89 stars and unticking
shows all 2000, and the title updates every time, so it is easy to prove in the demo. The checkbox
is readable because of the dark panel behind it.

With all 2000 stars the labels are far too crowded to read, so a later commit could turn labels on
and off. The AI made the checkbox not focusable, which helps the keyboard controls I add next, but
it means you cannot use the keyboard to tick it, which is worse for accessibility.

### Commit
Add switchable filter checkbox for stars with a proper name



---
## Entry 16 – 2026-11-06, 03:14 – feature2-visualising-star-chart
### Prompt
> Set up the mouse controls of the HelixViewport3D in MainWindow.xaml: the mouse wheel should zoom
> towards where the mouse cursor points, right mouse drag should rotate, and middle mouse drag (or
> Shift + right drag) should pan. Turn off inertia so the view stops when I let go. Show the
> camera's position on screen so I can see the movement working. Explain which settings you used.
> Don't change the C# files.

### Response
Claude added 5 HelixViewport3D settings in MainWindow.xaml (see this commit), with a comment
listing the controls:
- ZoomAroundMouseDownPoint="True" – zoom towards the point under the mouse
- RotateGesture="RightClick" – right drag rotates
- PanGesture="Shift+RightClick" – Shift + right drag pans
- IsInertiaEnabled="False" – the view stops when the mouse is let go
- ShowCameraInfo="True" – camera position and look direction shown bottom-left
It did not set middle-drag panning because it said that is already Helix's default, but that
this was from its knowledge and not tested. It said right-drag rotation already worked before.

Result: the camera info moved from Position 0,0,30 to -2.229,-0.560,65.240 after zooming out
and panning.

### Critique
It built first time, so the Helix property names were correct. The camera info box proves the
mouse moves the camera: the position changed from 0,0,30 to about -2.2,-0.6,65.2 after I zoomed
out and panned. Zooming towards the cursor worked. Middle-drag also panned, so the AI's claim about
the Helix default was right.

The AI was honest that it had not tested the middle button. Right-drag already worked before, so
that setting only makes it clear in the code. The camera info box is useful for testing but takes
space on screen, so I may turn it off later.

### Commit
Set up mouse zoom, rotate and pan, turn off inertia, show camera info



---
## Entry 17 – 2026-11-07, 21:05 – feature2-visualising-star-chart
### Prompt
> Add keyboard controls for the 3D camera, in a new class KeyboardCameraController (not in
> MainWindow): + and − zoom in and out towards the point the camera looks at, the arrow keys pan
> up/down/left/right, W A S D rotate around the point the camera looks at, and Home resets the
> camera to where it started. Steps should be relative to the distance, so they work both near
> and far. In MainWindow, send key presses to this class. Don't change StarRenderer or the other
> classes.

### Response
Claude wrote KeyboardCameraController.cs (see this commit):
- HandleKey(Key) returns true if it used the key: + / - (main keyboard and number pad) zoom,
  arrows pan, W A S D rotate, Home resets
- Zoom moves 10% of the distance to the target and stops at 0.2 and 500 parsecs; pan moves 5% of
  the distance; rotate turns 5 degrees around the target with RotateTransform3D
- the start camera is remembered in the constructor for Home
In MainWindow.xaml.cs it added the controller field, and a PreviewKeyDown handler that passes
keys to the controller and marks them handled, so they reach it before HelixToolkit. It said it
could not test this itself, and that Home only goes back to the very first camera.

Result: all keys work: zoom, pan, rotate and Home back to 0,0,30.

### Critique
The keyboard controls work, so Feature 2 now covers zoom, pan and scan with the mouse and the
keyboard, which the spec asks for. The code is in its own class, not MainWindow, and the steps
are a fraction of the distance, so it feels the same near and far. Home is useful when I get lost.

The AI could not test it, so I checked every key myself. Using PreviewKeyDown means my keys win
over HelixToolkit's own keys. If Feature 3 moves the camera to a star, Home will still go back to
the very first view, so I may need to change that later. The step sizes (10%, 5%, 5 degrees) are
fixed numbers I picked without testing other values.

### Commit
Add keyboard camera controls: zoom, pan, rotate and reset (KeyboardCameraController)


---
## Entry 18 – 2026-11-07, 23:35 – feature3-3d_movement
### Prompt
Context: Feature 3 needs "distance (light years), bearing (degrees) and path (degrees)". I asked
my domain expert how to calculate them (domain log Entry 3) and sent Claude the expert's full answer.

### Response
Claude first checked the expert's formulas against my stars.csv: bearing and path match the
expert's example (Sol to Rigil Kentaurus 219.9°, -60.8°), but the distance in my data is 4.32 ly,
not the 4.37 ly the expert quoted. It also found that Rigil Kentaurus and Toliman are less than
0.01 ly apart.

It wrote Navigation.cs (see this commit):
- record Route(DistanceLightYears, BearingDegrees?, PathDegrees?) with ToDisplayString(), showing
  "< 0.01 ly" for tiny distances and "—" for missing angles
- static Navigation.GetRoute(from, to): dx, dy, dz, bearing = atan2(dy, dx) in 0–360,
  path = atan2(dz, horizontal) in -90 to +90, null angles for the same star or straight up/down
- MainWindow.xaml.cs: new WriteRouteTest() writes routes from Sol to the Output window
It first said Sol -> Sol would print "0.00 ly", then corrected itself to "< 0.01 ly".

Result when I ran it:
```
Route test:
  Sol -> Rigil Kentaurus: 4.32 ly, bearing 219.9°, path -60.8°
  Sol -> Proxima Centauri: 4.23 ly, bearing 217.4°, path -62.7°
  Sol -> Sirius: 8.60 ly, bearing 101.3°, path -16.7°
  Sol -> Barnard's Star: 5.95 ly, bearing 269.5°, path 4.7°
  Sol -> Toliman: 4.32 ly, bearing 219.9°, path -60.8°
  Sol -> Sol: < 0.01 ly, bearing —, path —
```

### Critique
The route maths works and matches my domain expert's example for bearing and path. The AI also
checked the distance against my real data and found the expert's 4.37 ly was not from my file.

One small flaw: for the same star it shows "< 0.01 ly", when "0 ly" would be more honest. The AI
also made a mistake in its first explanation (it said "0.00 ly") and then corrected itself. The
bearing and path use the equatorial axes, so "up" means towards the north celestial pole, which I
need to explain in the demo because it is not "up" on my screen.

### Commit
Add Navigation: distance in light years, bearing and path between stars