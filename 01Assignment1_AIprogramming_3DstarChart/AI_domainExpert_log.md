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