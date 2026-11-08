# AI Domain Expert Log (Astronomy)
AI used: Claude Opus 5.5 (claude.ai), in a separate chat used only for astronomy questions,
not the chat I use for programming

---
## Entry 1 – 2026-11-05, 23:32 – how unnamed stars are named, and distance units
### Question
I'm building a star chart from the HYG star database v4.4. Many stars have no proper name. How
are these stars named in astronomy? Please explain Bayer designations (for example Tau Ceti),
Flamsteed numbers, and catalogue numbers like Gliese (Gl), Hipparcos (HIP) and Henry Draper (HD).
In my data, the bayer column holds values like "Tau", the flam column holds a number, and the con
column holds a 3-letter abbreviation like "Cet". How do I turn these into a full star name, and in
what order of preference should I use them if a star has several?
What unit is the dist column in the HYG database in, and how do I convert it to light years?

(I gave this chat access to my full hyg_v44.csv file so it could check real rows.)

### Response
Proper names exist for only a few hundred stars. Every other star is named by one of several
systems, which you can combine into a fallback chain. I checked your hyg_v44.csv, so the examples
below use your real rows.

**How stars are named**

Bayer designations (1603). Johann Bayer labelled the bright stars in each constellation with Greek
letters, roughly from brightest to faintest. The name is the Greek letter plus the constellation
name in the Latin genitive (possessive) form. So τ in Cetus becomes Tau Ceti (τ Cet), and α in
Canis Major becomes Alpha Canis Majoris, which is Sirius. When stars share a letter, a superscript
number tells them apart, as in κ¹ and κ² Sculptoris.

Flamsteed numbers (1725). John Flamsteed's catalogue numbered stars in each constellation from west
to east by right ascension, not by brightness. The format is the number plus the genitive: 61 Cygni,
or 9 Canis Majoris (Sirius again). They cover more, fainter stars than Bayer did, but mostly stars
visible from England, so far-southern stars often have no number.

Catalogue numbers apply to stars of any brightness:
- HR (Harvard Revised / Yale Bright Star Catalogue) covers about 9,000 stars visible to the naked
  eye. Your file has an hr column.
- Gliese (Gl / GJ) lists stars within about 25 parsecs of the Sun. It's the usual name for nearby
  faint stars, such as Gl 581.
- HD (Henry Draper) lists about 225,000 stars with spectral types, such as HD 48915.
- HIP (Hipparcos) comes from the ESA satellite's catalogue of about 118,000 stars with precise
  positions and parallaxes. Most HYG rows have one.

**What your columns actually contain**

| Column | Example from your file | How to display it |
|---|---|---|
| bayer | Alp, Tau, Kap-1 | 3-letter Greek abbreviation; -1 is the superscript |
| flam | 9 | plain number |
| con | CMa, Cet, Scl | IAU abbreviation, which needs mapping to the genitive |
| gl | Gl 244A, Gl 559A | already prefixed, so use it as-is |
| hip, hd, hr | 32349, 48915 | bare numbers, so add the HIP, HD, HR prefixes |
| bf | 9Alp CMa, Kap1Scl | ready-made combined label, but cramped; better to build your own |

You need two lookup tables:
- Greek letters: Alp α, Bet β, Gam γ, Del δ, Eps ε, Zet ζ, Eta η, The θ, Iot ι, Kap κ, Lam λ,
  Mu μ, Nu ν, Xi ξ, Omi ο, Pi π, Rho ρ, Sig σ, Tau τ, Ups υ, Phi φ, Chi χ, Psi ψ, Ome ω.
- Constellation genitives, all 88: for example Cet → Ceti, CMa → Canis Majoris, Cen → Centauri,
  Cyg → Cygni, Ori → Orionis, UMa → Ursae Majoris, Scl → Sculptoris.

For a compact chart label, use the symbol and abbreviation (τ Cet, κ¹ Scl). For a tooltip or info
panel, use the full form (Tau Ceti).

**Order of preference**
1. proper, for example Sirius
2. Bayer + con, for example α CMa
3. Flamsteed + con, for example 61 Cyg
4. gl, for example Gl 559A. Use this before the big catalogues, because nearby stars are best
   known by it.
5. HR, for example HR 2491 (optional, bright stars only)
6. HD, for example HD 48915
7. HIP, for example HIP 32349
8. A last resort such as HYG {id}

If a star has both a Bayer and a Flamsteed designation, show the Bayer one; the combined 9 α CMa
form is fine for detail views. Row 0 is the Sun (proper = Sol).

**Distance units**

dist is in parsecs. To get light years:

    light_years = dist × 3.26156

Two catches:
- 10,224 rows have dist = 100000. That's a placeholder for "distance unknown or unreliable" (the
  parallax was missing or too small to trust), not a real distance. Filter these out or flag them,
  or they'll land at 326,156 ly and wreck your 3D scale.
- The x, y, z columns are also in parsecs, so convert them by the same factor if your 3D space is
  in light years.

**C# sketch for your StarChart3D app**

```csharp
static readonly Dictionary<string,string> Greek = new() {
  ["Alp"]="α",["Bet"]="β",["Gam"]="γ",["Del"]="δ",["Eps"]="ε",["Zet"]="ζ",["Eta"]="η",["The"]="θ",
  ["Iot"]="ι",["Kap"]="κ",["Lam"]="λ",["Mu"]="μ",["Nu"]="ν",["Xi"]="ξ",["Omi"]="ο",["Pi"]="π",
  ["Rho"]="ρ",["Sig"]="σ",["Tau"]="τ",["Ups"]="υ",["Phi"]="φ",["Chi"]="χ",["Psi"]="ψ",["Ome"]="ω" };
static readonly string Sup = "⁰¹²³⁴⁵⁶⁷⁸⁹";

static string DisplayName(Star s) {
    if (!string.IsNullOrWhiteSpace(s.Proper)) return s.Proper;
    if (!string.IsNullOrWhiteSpace(s.Bayer)) {
        var parts = s.Bayer.Split('-');                       // "Kap-1" -> ["Kap","1"]
        string letter = Greek.GetValueOrDefault(parts[0], parts[0]);
        if (parts.Length > 1) letter += string.Concat(parts[1].Select(c => Sup[c - '0']));
        return $"{letter} {s.Con}";                           // or use the genitive lookup
    }
    if (!string.IsNullOrWhiteSpace(s.Flam)) return $"{s.Flam} {s.Con}";
    if (!string.IsNullOrWhiteSpace(s.Gl))   return s.Gl;
    if (!string.IsNullOrWhiteSpace(s.Hd))   return $"HD {s.Hd}";
    if (!string.IsNullOrWhiteSpace(s.Hip))  return $"HIP {s.Hip}";
    return $"HYG {s.Id}";
}

const double PcToLy = 3.26156;
static double? DistanceLy(double distPc) => distPc >= 100000 ? null : distPc * PcToLy;
```

I can write out the full table of all 88 constellation genitives as a C# dictionary if you want it.

### How I used it
I didn't just trust the answer, I checked it against my own stars.csv. The Sirius example is right
in my data (Alp, 9, CMa, Gl 244A, HIP 32349, HD 48915) and so is Tau Ceti (Tau, 52, Cet, Gl 71).
The gl column really does already have its Gl or GJ prefix.

The warning about dist = 100000 is true for the full file, but it does not affect me, because I
only kept the 2000 nearest stars and the furthest one is about 20.5 parsecs.

I found some problems with the answer:
- Its C# code would not work with my Star class, because it uses names like s.Proper and s.Con,
  but mine are ProperName and Constellation.
- It lists HR in the order of preference but then leaves HR out of its own code. My Star class
  does not store HR anyway.
- One star in my data has the Bayer value "p". That is a Latin letter, not Greek, so it is not in
  its Greek table. It never mentioned this.

I will use its order of preference (proper, Bayer, Flamsteed, Gliese, HD, HIP, HYG id) to make
names for stars without a proper name, and 3.26156 to turn parsecs into light years in Feature 3.



---
## Entry 2 – 2026-11-06, 02:45 – spectral class colours
### Question
I'm colouring stars by spectral class in my star chart (HYG v4.4, the 2000 stars nearest the
Sun). What colour should each main class O, B, A, F, G, K, M be? My spect column also has values
that don't start with those letters: "DA2", "DC5", "DA" (114 stars start with D), "dM5.5e"
(79 start with lowercase d), "sdM4", lowercase "m", "k" and "g" on their own, "p", "S", and 150
stars with a blank spectral type. What do these mean, which class should each be treated as for
colouring, and what colour should I use for a blank one?

### Response
**Colours for O–M** (two sets, depending on the look you want)

| Class | Temperature | Realistic | Chart-style (more saturated) |
|---|---|---|---|
| O | > 30,000 K | #9BB0FF | #6B8CFF blue |
| B | 10,000–30,000 K | #AABFFF | #9DB4FF blue-white |
| A | 7,500–10,000 K | #CAD7FF | #E0E8FF white |
| F | 6,000–7,500 K | #F8F7FF | #FFF8E7 yellow-white |
| G | 5,200–6,000 K | #FFF4EA | #FFE86B yellow |
| K | 3,700–5,200 K | #FFD2A1 | #FFAA55 orange |
| M | 2,400–3,700 K | #FFCC6F | #FF6A4D red |

The realistic values come from Mitchell Charity's "What colour are the stars?" table. Real stars
look much paler than textbooks suggest, so on a dark background most chart makers use the
saturated set. Only the letter matters for colour. Ignore the subclass number (the 2 in G2) and
the luminosity class (V, III).

**What the odd values mean**

| Value | Meaning | Treat as |
|---|---|---|
| D… (DA2, DC5, DA) | White dwarf. The second letter is the spectrum type: A = hydrogen lines, B = helium, C = no lines, Q = carbon, Z = metals. The number is a temperature index, Teff ≈ 50,400 / n K. | By the number: under 2 → B, 2–5 → A (DA2 ≈ 25,000 K, blue-white; DC5 ≈ 10,000 K, white), 5–8 → F, over 8 → G. No number (DA) → A, white. |
| d… (dM5.5e) | Old Mount Wilson prefix meaning dwarf, i.e. an ordinary main-sequence star. The trailing e means emission lines, usually a flare star. | Strip the d: dM5.5e → M |
| sd… (sdM4) | Subdwarf: an old, metal-poor star slightly below the main sequence. esd and usd are extreme and ultra subdwarfs. | Strip the prefix: sdM4 → M |
| lone m, k, g | In the Gliese nearby-star data these are rough types estimated from photometric colour rather than measured from a spectrum. I believe that's the convention, but I haven't verified it against the Gliese documentation. | Uppercase them: M, K, G |
| p | "Peculiar" spectrum with no class given | Unknown, same as blank |
| S | S-type star: a cool giant with zirconium-oxide bands, about as hot as an M star | M (red) |

You may also find L and T (brown dwarfs, cooler than M). Give them a dark red such as #B03020 and
a dim magenta-brown such as #8A3A6A, or treat them as M.

**Blank spectral types.** Don't paint them a default class colour. Use this fallback chain:
1. Use ci, the B–V colour index, if it's present. Convert it to temperature with Ballesteros'
   formula: T = 4600 × (1/(0.92·ci + 1.7) + 1/(0.92·ci + 0.62)), then pick the class from the
   temperature ranges in the first table.
2. If ci is blank too, use absmag as a hint. Among the 2,000 nearest stars, unclassified stars are
   mostly faint red dwarfs, so absmag > 9 → M is a reasonable guess.
3. Otherwise use a neutral grey, such as #B0B0B0, so they read as "unknown" rather than as a real
   class. Show "Unknown" in the info panel.

I couldn't open your file this time because your computer isn't reachable right now, so I haven't
checked how many of the 150 blank stars have ci values.

**C# sketch** (it also gave a C# sketch with a Dictionary<char, Color> of the chart-style colours,
a SpectralClass(spect, ci, absmag) method using two regular expressions, and a StarColour(Star)
method. It said: "The prefix strip only fires when an uppercase class letter follows. That means a
lone g isn't stripped away and still becomes G, while sdM4 and dM5.5e lose their prefixes correctly.")

### How I used it
I used the chart-style colours, because the expert said they look better on a black background.
The programming AI checked the rules against my stars.csv and they give every star a class:
B 17, A 52, F 133, G 186, K 417, M 1044 and 151 unknown. Sirius comes out A, the Sun G and
Barnard's Star M, which is right.

I found a problem in the answer: for white dwarfs the text says DC5 is about 10,000 K and white,
but its own number ranges make DC5 F (yellow-white), and by its own formula 50400 / 5 = 10080 K,
which is class B. So I used the formula with the temperature table for all white dwarfs, so it is
consistent. The expert also said it was not sure about the lone m, k, g meaning.

I did not use the ci and absmag fallback for blank stars, because my Star class does not store
those columns and only 67 of the 150 blanks have ci. I show them grey as "unknown" instead. The
expert's C# code also used names like st.Spect and st.Ci that are not in my Star class.



---

---
## Entry 3 – 2026-11-07, 23:34 – distance, bearing and path between stars
### Question
For my 3D star chart (HYG v4.4, x, y, z in parsecs, Sun at 0,0,0), I need to show the route from
the current star to another star as: distance in light years, bearing in degrees, and path (the
up/down angle) in degrees. What do the x, y and z axes point to in HYG, and how should I calculate
the bearing and the path angle from the two stars' x, y, z? What range should each angle be in
(for example 0 to 360 for bearing, -90 to +90 for path)?

### Response
Your assignment brief only says "Lists distance (light years), bearing (degrees) and path
(degrees)" and doesn't define either angle. So you choose the convention, and you should state it
in your documentation and AI log.

**What the HYG axes point to.** HYG uses equatorial Cartesian coordinates (epoch J2000), with the
Sun at the origin:

| Axis | Points toward |
|---|---|
| +x | The vernal equinox: RA 0h, Dec 0° (in Pisces) |
| +y | RA 6h, Dec 0° (in Orion/Monoceros), 90° east of +x along the celestial equator |
| +z | The north celestial pole: Dec +90°, near Polaris |

x = dist · cos(dec) · cos(ra), y = dist · cos(dec) · sin(ra), z = dist · sin(dec).
The x–y plane is the celestial equator projected into space. It is not the galactic plane.

**Bearing and path.** Take the vector from the current star A to the target star B:
dx = xB − xA, dy = yB − yA, dz = zB − zA.
- Distance: d_pc = √(dx² + dy² + dz²), d_ly = d_pc × 3.26156
- Bearing: bearing = atan2(dy, dx) in degrees, add 360 if negative. Range 0° to 360°.
  0° = toward RA 0h, 90° = RA 6h, 180° = RA 12h, 270° = RA 18h.
- Path: path = atan2(dz, √(dx² + dy²)) in degrees. Range −90° to +90°.
  +90° is straight "up" toward the north celestial pole, 0° level, −90° straight "down".

Because the axes don't rotate as you travel, the bearing and path are simply the RA (in degrees)
and Dec of star B as seen from star A. Bearing ÷ 15 gives the RA in hours. Use atan2 rather than
atan(dy/dx): it handles all four quadrants and won't divide by zero.

**Edge cases**
- Same star (d = 0): both angles are undefined. Show "—".
- Target directly above or below (dx = dy = 0): path is ±90°, bearing is meaningless; show "—".
- dist = 100000 rows: their x, y, z aren't real positions, so exclude them.

**C# sketch.** It gave a `record Route(double DistanceLy, double? BearingDeg, double? PathDeg)` and
a `ComputeRoute(Star a, Star b)` method using the formulas above, returning null angles for the
same star and a null bearing when straight up/down. Display as
"4.37 ly · bearing 219.9° · path −60.8°", one decimal place for angles.

**Quick check:** from the Sun to Alpha Centauri (RA ≈ 14h 39.6m, Dec ≈ −60.8°) this should give
about 4.37 ly, a bearing of about 219.9° and a path of about −60.8°.

**Watch the axis mapping:** WPF 3D treats Y as up. If you pass HYG coordinates in as (x, z, y) so
the celestial pole points up on screen, do the route maths on the original HYG x, y, z, not on the
swapped rendering coordinates.

### How I used it
I used the expert's bearing and path formulas in Navigation.cs. The test in my app gives Sol to
Rigil Kentaurus as bearing 219.9° and path -60.8°, exactly what the expert said, so the maths is
right. I show "—" for the same star, as it suggested.

But its "quick check" distance of 4.37 ly does not match my data. My HYG file says 1.3248 parsecs,
which is 4.32 ly. 4.37 ly is the number usually quoted for Alpha Centauri, so the expert used a
general fact instead of my data. The dist = 100000 warning does not affect me because my 2000 stars
are all within about 20.5 parsecs. I draw the stars with the original x, y, z, so the axis-swap
warning does not apply, but it is good to know.


